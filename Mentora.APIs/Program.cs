using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Mentora.Infra.Data;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Services;
using Mentora.APIs.Mappings;
using Mentora.Infra.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Set to true in production

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT Secret key is not configured in appsettings.json");
}

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    // Admin-only policies (using admin claim)
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireClaim("IsAdmin", "true"));

    // Action-based policies - any authenticated user can perform these actions
    options.AddPolicy("CanCreateSessions", policy =>
        policy.RequireAuthenticatedUser()); // Any user can create sessions

    options.AddPolicy("CanBookSessions", policy =>
        policy.RequireAuthenticatedUser()); // Any user can book sessions

    options.AddPolicy("CanManageRoles", policy =>
        policy.RequireClaim("IsAdmin", "true"));

    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireClaim("IsAdmin", "true"));

    options.AddPolicy("CanViewAnalytics", policy =>
        policy.RequireClaim("IsAdmin", "true"));
});

// Configure Cloudinary Settings
builder.Services.Configure<Mentora.Domain.Models.CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));

// Register custom services
builder.Services.AddScoped<IJwtService, JwtService>();

// Register Domain services
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// Register Infrastructure services (services that depend on ASP.NET Core Identity)
builder.Services.AddScoped<IUserService, Mentora.Infra.Services.UserService>();

// Register Infrastructure services
// Switching back to CloudinaryService - it was working before
builder.Services.AddScoped<IFileService, CloudinaryService>();

// Register Infrastructure repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IFileRepository, FileRepository>();

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Mentora API", Version = "v1" });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();




app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAll");

// Use Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
