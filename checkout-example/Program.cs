using Adyen.Checkout.Extensions;
using Adyen.Core.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace adyen_dotnet_checkout_example
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureCheckout(((context, services, config) =>
                {
                    config.ConfigureAdyenOptions(x =>
                    {
                        x.AdyenApiKey = context.Configuration["ADYEN_API_KEY"];
                        x.Environment = AdyenEnvironment.Test;
                    });
                }))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}
