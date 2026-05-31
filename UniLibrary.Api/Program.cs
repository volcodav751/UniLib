using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UniLibrary.Api.Data;
using UniLibrary.Api.Repos;
using UniLibrary.Api.Services;
using UniLibrary.Api.Services.Auth;
using UniLibrary.Api.Services.Books;
using UniLibrary.Api.Services.Common;
using UniLibrary.Api.Services.Files;
using UniLibrary.Api.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

string connectionString = builder.Configuration.GetConnectionString("LiteDb")
    ?? "Filename=Unilab.db;Connection=shared";

builder.Services.AddSingleton(new LiteDbContext(connectionString));

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<PdfPreviewService>();

builder.Services.AddSingleton<IBookRepository, BookRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IBookFileRepository, LiteDbBookFileRepository>();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IBookFileService, BookFileService>();

string jwtKey = builder.Configuration["Jwt:Key"]
    ?? "UniLibrary development JWT secret key 2026. Change this long key before real deployment.";

byte[] jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                string? accessToken = context.Request.Query["access_token"];
                PathString path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/api/books"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorCors", policy =>
    {
        policy
            .WithOrigins("https://localhost:7170")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("BlazorCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
