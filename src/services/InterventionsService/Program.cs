using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Savora.InterventionsService.Application.Services;
using Savora.InterventionsService.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/interventionsservice-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SAVORA - Interventions Service",
        Version = "v1",
        Description = "Technical Intervention Management Service for SAVORA SAV Application"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Database
builder.Services.AddDbContext<InterventionsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Configure SMTP Settings
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// Register Services
builder.Services.AddScoped<IInterventionService, InterventionServiceImpl>();
builder.Services.AddScoped<ITechnicianService, TechnicianServiceImpl>();
builder.Services.AddScoped<IInvoiceService, InvoiceServiceImpl>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Configure HTTP clients for inter-service communication
var articlesServiceUrl = builder.Configuration["Services:ArticlesServiceUrl"] ?? "http://localhost:5002";
builder.Services.AddHttpClient<IArticleApiClient, ArticleApiClient>(client =>
{
    client.BaseAddress = new Uri(articlesServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var reclamationsServiceUrl = builder.Configuration["Services:ReclamationsServiceUrl"] ?? "http://localhost:5003";
builder.Services.AddHttpClient<IReclamationApiClient, ReclamationApiClient>(client =>
{
    client.BaseAddress = new Uri(reclamationsServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IClientApiClient, ClientApiClient>(client =>
{
    client.BaseAddress = new Uri(reclamationsServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<INotificationApiClient, NotificationApiClient>(client =>
{
    client.BaseAddress = new Uri(reclamationsServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpContextAccessor();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health Checks
builder.Services.AddHealthChecks();

// Global Exception Handling
builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InterventionsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    var retries = 0;
    var maxRetries = 10;
    while (retries < maxRetries)
    {
        try
        {
            logger.LogInformation("Attempting to ensure database is created (attempt {Retry}/{MaxRetries})...", retries + 1, maxRetries);
            
            // Check if database exists and has correct schema
            if (await dbContext.Database.CanConnectAsync())
            {
                var connection = dbContext.Database.GetDbConnection();
                await connection.OpenAsync();
                try
                {
                    var command = connection.CreateCommand();
                    
                    // Check if Invoices table exists
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Invoices'";
                    var invoicesTableExists = await command.ExecuteScalarAsync() != null;
                    
                    bool needsRecreation = false;
                    
                    if (!invoicesTableExists)
                    {
                        logger.LogInformation("Invoices table missing. Recreating database...");
                        needsRecreation = true;
                    }
                    else
                    {
                        // Check if Invoices table has OrderId column
                        command.CommandText = "PRAGMA table_info(Invoices)";
                        using var reader = await command.ExecuteReaderAsync();
                        bool hasOrderId = false;
                        while (await reader.ReadAsync())
                        {
                            var columnName = reader.GetString(1);
                            if (columnName == "OrderId")
                            {
                                hasOrderId = true;
                                break;
                            }
                        }
                        
                        if (!hasOrderId)
                        {
                            logger.LogInformation("Invoices table missing OrderId column. Recreating database...");
                            needsRecreation = true;
                        }
                    }
                    
                    if (needsRecreation)
                    {
                        await connection.CloseAsync();
                        await dbContext.Database.EnsureDeletedAsync();
                        await dbContext.Database.EnsureCreatedAsync();
                    }
                    else
                    {
                        logger.LogInformation("Database schema is up to date.");
                    }
                }
                catch (Exception schemaEx)
                {
                    // If check fails, recreate database
                    logger.LogWarning(schemaEx, "Schema check failed. Recreating database...");
                    if (connection.State == System.Data.ConnectionState.Open)
                        await connection.CloseAsync();
                    await dbContext.Database.EnsureDeletedAsync();
                    await dbContext.Database.EnsureCreatedAsync();
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        await connection.CloseAsync();
                }
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
            }
            
            logger.LogInformation("Database created successfully");
            
            logger.LogInformation("Seeding database...");
            try
            {
                await InterventionsDbSeeder.SeedAsync(dbContext);
                logger.LogInformation("Database seeded successfully");
            }
            catch (Exception seedEx)
            {
                logger.LogWarning(seedEx, "Database seeding failed, but continuing startup. Tables may not exist yet.");
                // Continue anyway - the service can still start
            }
            break;
        }
        catch (Exception ex)
        {
            retries++;
            logger.LogWarning(ex, "Migration attempt {Retry} failed. Retrying in 5 seconds...", retries);
            if (retries >= maxRetries)
            {
                logger.LogError(ex, "Failed to apply migrations after {MaxRetries} attempts", maxRetries);
                throw;
            }
            await Task.Delay(5000);
        }
    }
}

// Configure the HTTP request pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SAVORA Interventions Service v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapHealthChecks("/health");
app.MapControllers();

Log.Information("SAVORA Interventions Service starting...");
app.Run();

