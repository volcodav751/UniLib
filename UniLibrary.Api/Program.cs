using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorCors", policy =>
    {
        policy.WithOrigins("https://localhost:7170")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Pipeline
app.UseHttpsRedirection();

app.UseCors("BlazorCors");

app.UseAuthorization();

app.MapControllers();

app.Run();