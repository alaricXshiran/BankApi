// Program.cs: the starting point of BankApi.
// Part 1 registers the services (tools) the app uses.
// Part 2 builds the app and defines the request pipeline, the steps every request passes through.

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

// =====================================================================
// PART 1: SERVICES
// =====================================================================

// ---------- Controllers and JSON ----------

// Use controllers, and write enums as words in JSON ("Savings" instead of 0).
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---------- OpenAPI description (used by Swagger UI) ----------

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

// ---------- Database ----------

// Register BankDbContext so controllers can ask for it (dependency injection).
// The connection string comes from appsettings.json, or from an environment variable in Docker.
builder.Services.AddDbContext<BankDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("BankDb")));

// ---------- Error handling ----------

// Return every error in the standard Problem Details format.
builder.Services.AddProblemDetails(options =>
{
    // Adjust error responses in one central place before they're sent.
    options.CustomizeProblemDetails = context =>
    {
        // .NET gives 429 no title by default, so add a clear message.
        if (context.ProblemDetails.Status == StatusCodes.Status429TooManyRequests)
        {
            context.ProblemDetails.Title = "Too many requests.";
            context.ProblemDetails.Detail = "You have made too many requests. Please wait a moment and try again.";
        }
    };
});

// Catch any unexpected error: log the full details, return a safe 500 to the caller.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---------- Rate limiting ----------

// Limit how many requests each caller (IP address) can make, to block abuse and password guessing.
builder.Services.AddRateLimiter(options =>
{
    // Rejected requests get 429 Too Many Requests.
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

// ---------- Authentication and authorization ----------

// Hashes passwords at registration and checks them at login (never stores plain passwords).
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
        // The signing key comes from User Secrets locally, or an environment variable in Docker.
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

// =====================================================================
// PART 2: BUILD THE APP AND THE REQUEST PIPELINE
// The order below matters: every request passes through these steps from top to bottom.
// =====================================================================

var app = builder.Build();

// Create or upgrade the database, and the first admin user, before handling any requests.
await DatabaseInitializer.InitializeAsync(app);

// First in the pipeline, so it can catch errors from every step after it.
app.UseExceptionHandler();

// Give empty error responses (like 401, 404 and 429) a Problem Details body.
app.UseStatusCodePages();

// Swagger is only available in Development, so the public never sees the full list of endpoints.
if (app.Environment.IsDevelopment())
{
    // Publish the API description at /openapi/v1.json.
    app.MapOpenApi();

    // Serve the Swagger page at /swagger, built from that description.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "BankApi v1");
        // Remember the pasted token when the Swagger page is refreshed.
        options.EnablePersistAuthorization();
    });
}

// Send plain HTTP requests to HTTPS when an HTTPS address is available.
app.UseHttpsRedirection();

// Read and validate the token first, so we know WHO is calling...
app.UseAuthentication();

// Turn away callers who are over their limit, before they reach the controllers or the database.
app.UseRateLimiter();

// ...then check WHAT they're allowed to do.
app.UseAuthorization();

// Send each request to the matching controller method.
app.MapControllers();

// Start the app and wait for requests.
app.Run();