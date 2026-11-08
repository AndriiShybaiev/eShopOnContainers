using Catalog.API.Data;
using Catalog.API.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddDbContext<CatalogContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<CatalogContextSeed>();

// Configure RabbitMQ Publisher
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<RabbitMqExchange>(builder.Configuration.GetSection("RabbitMqExchange"));
builder.Services.AddSingleton<IEventsPublisher, EventsPublisher>();
builder.Services.AddHostedService<EventsPublisherHostedService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "eShopOnContainers - Catalog HTTP API",
        Version = "v1",
        Description = "The Catalog Microservice HTTP API."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog.API v1");
    });
}

if (!app.Environment.IsDevelopment() && !app.Environment.EnvironmentName.Equals("Docker", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<CatalogContext>();
    var seeder = services.GetRequiredService<CatalogContextSeed>();
    var seederLogger = services.GetRequiredService<ILogger<CatalogContextSeed>>();

    const int maxDbRetries = 10;
    for (var attempt = 1; attempt <= maxDbRetries; attempt++)
    {
        try
        {
            logger.LogInformation("Applying EF migrations to database (attempt {Attempt}/{MaxRetries})...", attempt, maxDbRetries);
            await context.Database.MigrateAsync();
            await seeder.SeedAsync(context, seederLogger);
            logger.LogInformation("Database migration and seeding completed successfully.");
            break;
        }
        catch (Exception ex) when (attempt < maxDbRetries)
        {
            logger.LogWarning(ex, "Failed to connect to database or apply migrations on attempt {Attempt}/{MaxRetries}. Retrying in 3s...", attempt, maxDbRetries);
            await Task.Delay(3000);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding database.");
        }
    }
}

app.Run();
