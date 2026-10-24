using System.Text;
using Billing.Api.Http;
using Billing.Api.Live;
using Billing.Api.Security;
using Billing.Application.Abstractions;
using Billing.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestContext, HttpRequestContext>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));
builder.Services.AddSignalR();
builder.Services.AddSingleton<IHouseLive, SignalRHouseLive>();
builder.Services.AddOpenApi();

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireClaim("super_admin", "true"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
        policy.WithOrigins(
                "http://localhost:4200",
                "http://127.0.0.1:4200",
                "http://127.0.0.1:4210",
                "http://localhost",
                "https://localhost",
                "capacitor://localhost")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Aangan.Api");
        logger.LogError(exception, "Unhandled API error");
        if (context.Response.HasStarted)
            throw;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var message = app.Environment.IsDevelopment()
            ? exception.GetBaseException().Message
            : "Something went wrong. Please try again.";
        await context.Response.WriteAsJsonAsync(new { message });
    }
});

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<FloorHub>("/hubs/floor");
app.Run();
