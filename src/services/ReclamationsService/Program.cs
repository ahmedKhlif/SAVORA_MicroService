using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Savora.ReclamationsService.Application.Services;
using Savora.ReclamationsService.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/reclamationsservice-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SAVORA - Reclamations Service",
        Version = "v1",
        Description = "Client & Reclamation Management Service for SAVORA SAV Application"
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
builder.Services.AddDbContext<ReclamationsDbContext>(options =>
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
builder.Services.AddScoped<IReclamationService, ReclamationServiceImpl>();
builder.Services.AddScoped<IClientService, ClientServiceImpl>();
builder.Services.AddScoped<INotificationService, NotificationServiceImpl>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDashboardService, DashboardServiceImpl>();
builder.Services.AddScoped<IMessageService, MessageServiceImpl>();

// Configure HTTP clients for inter-service communication
var articlesServiceUrl = builder.Configuration["Services:ArticlesServiceUrl"] ?? "http://localhost:5002";
var interventionsServiceUrl = builder.Configuration["Services:InterventionsServiceUrl"] ?? "http://localhost:5004";
var authServiceUrl = builder.Configuration["Services:AuthServiceUrl"] ?? "http://localhost:5001";

builder.Services.AddHttpClient<IArticleApiClient, ArticleApiClient>(client =>
{
    client.BaseAddress = new Uri(articlesServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IInterventionApiClient, InterventionApiClient>(client =>
{
    client.BaseAddress = new Uri(interventionsServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IPartApiClient, PartApiClient>(client =>
{
    client.BaseAddress = new Uri(articlesServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("AuthApiClient", client =>
{
    client.BaseAddress = new Uri(authServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddScoped<IAuthApiClient, AuthApiClient>();

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
    var dbContext = scope.ServiceProvider.GetRequiredService<ReclamationsDbContext>();
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
                    
                    // Check if Reclamations table exists
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Reclamations'";
                    var reclamationsTableExists = await command.ExecuteScalarAsync() != null;
                    
                    // Check if Messages table exists
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Messages'";
                    var messagesTableExists = await command.ExecuteScalarAsync() != null;
                    
                    bool needsRecreation = false;
                    
                    if (!reclamationsTableExists || !messagesTableExists)
                    {
                        logger.LogInformation("Required tables missing. Recreating database...");
                        needsRecreation = true;
                    }
                    else
                    {
                        // Check if Reclamations table has ClientArticleId column
                        command.CommandText = "PRAGMA table_info(Reclamations)";
                        using var reader = await command.ExecuteReaderAsync();
                        bool hasClientArticleId = false;
                        while (await reader.ReadAsync())
                        {
                            var columnName = reader.GetString(1);
                            if (columnName == "ClientArticleId")
                            {
                                hasClientArticleId = true;
                                break;
                            }
                        }
                        
                        if (!hasClientArticleId)
                        {
                            logger.LogInformation("Reclamations table missing ClientArticleId column. Recreating database...");
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
                await ReclamationsDbSeeder.SeedAsync(dbContext);
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SAVORA Reclamations Service v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

Log.Information("SAVORA Reclamations Service starting...");
app.Run();

