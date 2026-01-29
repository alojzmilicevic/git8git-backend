using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Core;
using api.Core.Middlewares;
using core;
using core.Auth;
using core.GitHub;
using core.Licensing;
using core.Storage.Dynamo;
using core.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AuthSettings>(provider =>
{
    var settings = new AuthSettings();
    builder.Configuration.GetSection("Auth").Bind(settings);
    return settings;
});

builder.Services.AddSingleton<DynamoDbSettings>(provider =>
{
    var settings = new DynamoDbSettings();
    builder.Configuration.GetSection("DynamoDbSettings").Bind(settings);
    return settings;
});

builder.Services.AddSingleton<GitHubSettings>(provider =>
{
    var settings = new GitHubSettings();
    builder.Configuration.GetSection("GitHub").Bind(settings);
    return settings;
});

builder.Services.AddSingleton<IDynamoDb, DynamoDb>();

builder.Services.AddFeature<AuthFeature>();
builder.Services.AddFeature<GitHubFeature>();
builder.Services.AddFeature<LicensingFeature>();
builder.Services.AddFeature<UsersFeature>();

var authSettings = new AuthSettings();
builder.Configuration.GetSection("Auth").Bind(authSettings);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSettings.JwtSecret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var chromeExtensionId = authSettings.ChromeExtensionId;
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = new List<string>();
        
        if (!string.IsNullOrEmpty(chromeExtensionId))
        {
            origins.Add($"chrome-extension://{chromeExtensionId}");
        }

        if (builder.Environment.IsDevelopment())
        {
            origins.Add("http://localhost:3000");
            origins.Add("http://localhost:5173");
            origins.Add("http://localhost:8080");
        }

        if (origins.Count > 0)
        {
            policy.WithOrigins(origins.ToArray());
        }
        else
        {
            policy.AllowAnyOrigin();
        }

        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Git8Git API", Version = "v1" });
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

await app.Services.WarmUpFeatures();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Git8Git API v1");
        c.RoutePrefix = "docs";
    });
}

app.UseMiddleware<RequestIdMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
