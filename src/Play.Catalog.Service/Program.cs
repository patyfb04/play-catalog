using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Catalog.Service;
using Play.Catalog.Service.Entities;
using Play.Catalog.Service.Policies;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.Messaging;
using Play.Common.Repositories;
using Play.Common.Settings;
using Serilog;


var builder = WebApplication.CreateBuilder(args);
const string AllowedOriginSetting = "AllowedOrigin";

BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
BsonSerializer.RegisterSerializer(typeof(Guid), new GuidSerializer(GuidRepresentation.Standard));
BsonSerializer.RegisterSerializer(typeof(Guid?), new NullableSerializer<Guid>(new GuidSerializer(GuidRepresentation.Standard)));

Log.Logger = new LoggerConfiguration()
             .WriteTo.Console()
             //.WriteTo.File("logs/log.txt")
             .MinimumLevel.Information()
             .CreateLogger();

builder.Host.UseSerilog();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

builder.Services.Configure<CosmosDbSettings>(
    builder.Configuration.GetSection(nameof(CosmosDbSettings)));

builder.Services.Configure<ServiceSettings>(
    builder.Configuration.GetSection(nameof(ServiceSettings)));

builder.Services.Configure<ServiceBusSettings>(
    builder.Configuration.GetSection(nameof(ServiceBusSettings)));

builder.Services.Configure<MassTransitSettings>(
    builder.Configuration.GetSection(nameof(MassTransitSettings)));

builder.Services
    .AddCosmosDb()
    .AddCosmosRepository<Item>("items")
    .AddMassTransitWithAzureServiceBus()
    .AddJwtBearerAuthentication();

var serviceSettings = builder.Configuration
    .GetSection(nameof(ServiceSettings))
    .Get<ServiceSettings>();

var clientServicesSettings = builder.Configuration
    .GetSection(nameof(ClientServicesSettings))
    .Get<ClientServicesSettings>();

builder.Services.AddSingleton<IAuthorizationHandler, CatalogReadOrAdminHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Read, policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("scope","catalog.fullaccess", "catalog.readaccess");
    });

    options.AddPolicy(Policies.Write, policy =>
    {
        policy.RequireRole("Admin");
        policy.RequireClaim("scope", "catalog.fullaccess", "catalog.writeaccess");
    });

    // Define a policy for read-only access to the catalog from the other services
    options.AddPolicy("CatalogReadOrAdmin", policy =>
          policy.AddRequirements(new CatalogReadOrAdminRequirement()));
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Catalog.Service", Version = "v1" });
});


var app = builder.Build();

// Map a simple anonymous liveness endpoint first
app.MapGet("/health", () => Results.Ok("Healthy"))
   .AllowAnonymous();

// If you want to skip HTTPS redirect for /health, add this BEFORE UseHttpsRedirection
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next();
            return;
        }

        await next();
    });

    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();