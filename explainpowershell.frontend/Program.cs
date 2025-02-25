using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MudBlazor.Services;

namespace explainpowershell.frontend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            var baseAddress = builder.Configuration.GetValue<string>("BaseAddress") 
                ?? throw new InvalidOperationException("BaseAddress configuration is required");

            builder.Services.AddScoped(sp => new HttpClient {
                BaseAddress = new Uri(baseAddress)
            });
            
            builder.Services.AddMudServices();
            await builder.Build().RunAsync();
        }
    }
}
