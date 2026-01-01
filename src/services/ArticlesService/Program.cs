using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Savora.ArticlesService.Application.Services;
using Savora.ArticlesService.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/articlesservice-.log", rollingInterval: RollingInterval.Day)
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
        Title = "SAVORA - Articles Service",
        Version = "v1",
        Description = "Articles and Parts Management Service for SAVORA SAV Application"
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
builder.Services.AddDbContext<ArticlesDbContext>(options =>
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
// Note: IArticleService removed - using direct DbContext access where needed
builder.Services.AddScoped<IPartService, PartService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Configure HTTP clients for inter-service communication
var reclamationsServiceUrl = builder.Configuration["Services:ReclamationsServiceUrl"] ?? "http://localhost:5003";
builder.Services.AddHttpClient<IClientApiClient, ClientApiClient>(client =>
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
    var dbContext = scope.ServiceProvider.GetRequiredService<ArticlesDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    var retries = 0;
    var maxRetries = 10;
    while (retries < maxRetries)
    {
        try
        {
            logger.LogInformation("Attempting to ensure database is created (attempt {Retry}/{MaxRetries})...", retries + 1, maxRetries);
            
            // Check if database exists and has the correct schema
            if (await dbContext.Database.CanConnectAsync())
            {
                var connection = dbContext.Database.GetDbConnection();
                await connection.OpenAsync();
                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Articles'";
                    var articlesTableExists = await command.ExecuteScalarAsync() != null;
                    
                    if (!articlesTableExists)
                    {
                        logger.LogInformation("Articles table not found. Recreating database with new schema...");
                        await connection.CloseAsync();
                        await dbContext.Database.EnsureDeletedAsync();
                        await dbContext.Database.EnsureCreatedAsync();
                    }
                    else
                    {
                        logger.LogInformation("Database schema is up to date.");
                    }
                }
                catch
                {
                    // If check fails, recreate database
                    logger.LogInformation("Schema check failed. Recreating database...");
                    if (connection.State == ConnectionState.Open)
                        await connection.CloseAsync();
                    await dbContext.Database.EnsureDeletedAsync();
                    await dbContext.Database.EnsureCreatedAsync();
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        await connection.CloseAsync();
                }
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
            }
            
            // Verify tables were created
            var verifyConnection = dbContext.Database.GetDbConnection();
            await verifyConnection.OpenAsync();
            try
            {
                var verifyCommand = verifyConnection.CreateCommand();
                verifyCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Articles'";
                var tableExists = await verifyCommand.ExecuteScalarAsync() != null;
                if (!tableExists)
                {
                    logger.LogWarning("Articles table still does not exist after EnsureCreated. Forcing recreation...");
                    await verifyConnection.CloseAsync();
                    await dbContext.Database.EnsureDeletedAsync();
                    await dbContext.Database.EnsureCreatedAsync();
                }
            }
            finally
            {
                if (verifyConnection.State == ConnectionState.Open)
                    await verifyConnection.CloseAsync();
            }
            
            logger.LogInformation("Database created successfully");
            
            logger.LogInformation("Seeding database...");
            try
            {
                await ArticlesDbSeeder.SeedAsync(dbContext);
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SAVORA Articles Service v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseStaticFiles(); // Enable static file serving for images
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

Log.Information("SAVORA Articles Service starting...");
app.Run();

