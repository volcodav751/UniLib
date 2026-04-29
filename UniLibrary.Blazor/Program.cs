using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UniLibrary.Blazor;
using UniLibrary.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:5152/")
});

builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<BookApiService>();

await builder.Build().RunAsync();