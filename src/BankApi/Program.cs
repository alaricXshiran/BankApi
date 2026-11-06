using BankApi.Models;
using Microsoft.AspNetCore.Identity;
using BankApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using BankApi.ErrorHandling;
using System.Threading.RateLimiting;
using BankApi.Services;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    // Use OpenAPI 3.0, which Swagger UI reads correctly (see the Step 8 fix).
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;

    // Tell Swagger that this API uses JWT Bearer tokens,
    // so it shows an "Authorize" button where we can paste a token.
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // 1. Define the "Bearer" security scheme.
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Log in with POST /api/Auth/login, then paste the token here."
        };

        // 2. Apply the scheme to the API, so Swagger sends the token with requests.
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("BankDb")));


builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (context.ProblemDetails.Status == StatusCodes.Status429TooManyRequests)
        {
            context.ProblemDetails.Title = "Too many requests.";
            context.ProblemDetails.Detail = "You have made too many requests. Please wait a moment and try again.";
        }
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // General limit: 100 requests per minute, per IP address
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Stricter limit for moving money: 5 per minute, per IP address
    options.AddPolicy("money", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Strict limit for login attempts: 5 per minute, per IP address.
    // This makes guessing passwords (brute-force attacks) far too slow to work.
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
// Creates JWTs at login.
builder.Services.AddScoped<TokenService>();

// Authentication: check the JWT sent with each request.
// A token is only accepted if it was signed with our key, by our API,
// for our API, and hasn't expired.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is not configured.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Was it issued by us?
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            // Is it meant for us?
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            // Is the signature (the wax seal) genuine?
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            // Has it expired? Allow only 1 minute of clock difference between servers.
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Authorization: decide what an authenticated user is allowed to do.
builder.Services.AddAuthorization();


var app = builder.Build();
// Create or upgrade the database, and the first admin user, before handling any requests.
await DatabaseInitializer.InitializeAsync(app);
app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BankApi v1");
        // Remember the pasted token when the Swagger page is refreshed.
        options.EnablePersistAuthorization();
    });
}

app.UseHttpsRedirection();

// Read and validate the token first, so we know WHO is calling...
app.UseAuthentication();

app.UseRateLimiter();

// ...then check WHAT they're allowed to do.
app.UseAuthorization();

app.MapControllers();

app.Run();
