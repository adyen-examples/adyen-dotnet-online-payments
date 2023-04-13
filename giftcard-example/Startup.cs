using Adyen;
using Adyen.Model.Enum;
using Adyen.Service;
using Adyen.Util;
using adyen_dotnet_giftcard_example.Options;
using adyen_dotnet_giftcard_example.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace adyen_dotnet_giftcard_example
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configure your keys using the Options pattern.
            // This will auto-retrieve/configure your keys from your environmental variables (ADYEN_CLIENT_KEY, ADYEN_API_KEY, ADYEN_MERCHANT_ACCOUNT, ADYEN_HMAC_KEY).
            services.Configure<AdyenOptions>(
                options =>
                {
                    // Public key used for client-side authentication: https://docs.adyen.com/development-resources/client-side-authentication.
                    options.ADYEN_CLIENT_KEY = Configuration[nameof(AdyenOptions.ADYEN_CLIENT_KEY)];

                    // Your secret API Key: https://docs.adyen.com/development-resources/api-credentials#generate-your-api-key.
                    options.ADYEN_API_KEY = Configuration[nameof(AdyenOptions.ADYEN_API_KEY)];

                    // Your Merchant Account name: https://docs.adyen.com/account/account-structure.
                    options.ADYEN_MERCHANT_ACCOUNT = Configuration[nameof(AdyenOptions.ADYEN_MERCHANT_ACCOUNT)];

                    // HMAC Key used to validate your webhook signatures: https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures.
                    options.ADYEN_HMAC_KEY = Configuration[nameof(AdyenOptions.ADYEN_HMAC_KEY)];
                }
            );

            // Register your controllers.
            services.AddControllersWithViews();
            services.AddControllers().AddNewtonsoftJson();
            services.AddHttpContextAccessor()
                .AddTransient<IUrlService, UrlService>();

            // Register your dependencies.
            services.AddSingleton<Client>(provider => new Client(
                provider.GetRequiredService<IOptions<AdyenOptions>>().Value.ADYEN_API_KEY,  // Get your API Key from the AdyenOptions using the Options pattern.
                Environment.Test) // Test environment.
            );
            services.AddSingleton<Checkout>();
            services.AddSingleton<HmacValidator>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
