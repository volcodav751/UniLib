using UniLibrary.Api.Data;
using UniLibrary.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

string connectionString = builder.Configuration.GetConnectionString("LiteDb")
    ?? "Filename=Unilab.db;Connection=shared";

builder.Services.AddSingleton(new LiteDbContext(connectionString));
builder.Services.AddSingleton<LiteDbService>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();