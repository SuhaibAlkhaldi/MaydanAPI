using System.Text;
using FluentValidation;
using Maydan.API.Filters;
using Maydan.API.Security;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Infrastructure.Email;
using Maydan.Infrastructure.Persistence;
using Maydan.Infrastructure.Repositories;
using Maydan.Infrastructure.Security;
using Maydan.Infrastructure.Storage;

using Maydan.Infrastructure.Web;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers(options => options.Filters.Add<SystemConfigurationGateFilter>());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Maydan API", Version = "v1" });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };
    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services.AddDbContext<MaydanDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("MaydanDb")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IAssociationService, AssociationService>();
builder.Services.AddScoped<IAssociationProjectSupervisorService, AssociationProjectSupervisorService>();
builder.Services.AddScoped<IProductionCompanyService, ProductionCompanyService>();
builder.Services.AddScoped<IProductionCompanyOnboardingService, ProductionCompanyOnboardingService>();
builder.Services.AddScoped<IWorkerService, WorkerService>();
builder.Services.AddScoped<IEntityOnboardingService, EntityOnboardingService>();
builder.Services.AddScoped<IFrontendLinkBuilder, FrontendLinkBuilder>();

builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();


builder.Services.AddDataProtection();
builder.Services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
builder.Services.AddScoped<ISystemConfigurationService, SystemConfigurationService>();
builder.Services.AddScoped<ISystemConfigurationGateService, SystemConfigurationGateService>();
builder.Services.AddScoped<IServiceConfigurationService, ServiceConfigurationService>();

var uploadsRootPath = Path.Combine(
    builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"),
    "uploads");
builder.Services.AddSingleton<IFileStorageService>(new LocalFileStorageService(uploadsRootPath));

builder.Services.AddSingleton<ICivilIdHasher, HmacCivilIdHasher>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

builder.Services.AddValidatorsFromAssembly(typeof(Maydan.Application.AssemblyReference).Assembly);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured. Set it via user-secrets or the Jwt__Key environment variable.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();


app.UseStaticFiles();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
