using Adyen;
using Adyen.Model;
using Adyen.Service.Checkout;
using Adyen.Util;
using adyen_dotnet_authorisation_adjustment_example.Options;
using adyen_dotnet_authorisation_adjustment_example.Repositories;
using adyen_dotnet_authorisation_adjustment_example.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace adyen_dotnet_authorisation_adjustment_example
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
            // This will auto-retrieve/configure your keys from your environmental variables (ADYEN_API_KEY, ADYEN_CLIENT_KEY, ADYEN_MERCHANT_ACCOUNT, ADYEN_HMAC_KEY).
            services.Configure<AdyenOptions>(
                options =>
                {
                    options.ADYEN_API_KEY = Configuration[nameof(AdyenOptions.ADYEN_API_KEY)];
                    options.ADYEN_CLIENT_KEY = Configuration[nameof(AdyenOptions.ADYEN_CLIENT_KEY)];
                    options.ADYEN_MERCHANT_ACCOUNT = Configuration[nameof(AdyenOptions.ADYEN_MERCHANT_ACCOUNT)];
                    options.ADYEN_HMAC_KEY = Configuration[nameof(AdyenOptions.ADYEN_HMAC_KEY)];
                }
            );

            // Register controllers.
            services.AddControllersWithViews();
            services.AddControllers().AddNewtonsoftJson();
            services.AddHttpContextAccessor()
                .AddTransient<IUrlService, UrlService>();

            // Register Adyen dependencies.
            services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AdyenOptions>>();
                return new Client(
                    new Config()
                    {
                        // Get your `API Key`, `HMAC Key` and `MerchantAccount` from AdyenOptions using the Options pattern.
                        XApiKey = options.Value.ADYEN_API_KEY,
                        HmacKey = options.Value.ADYEN_HMAC_KEY,
                        MerchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT,
                        
                        // Test environment.
                        Environment = Environment.Test,
                    });
            }).AddHttpClient(); // Add HttpClient.

            services.AddSingleton<IPaymentsService, PaymentsService>(); // Used to be called "Checkout.cs" in Adyen .NET 9.x.x and below, see https://github.com/Adyen/adyen-dotnet-api-library/blob/9.2.1/Adyen/Service/Checkout.cs.
            services.AddSingleton<IModificationsService, ModificationsService>();
            services.AddSingleton<HmacValidator>();

            // Register your dependencies.
            services.AddSingleton<IPaymentRepository, PaymentRepository>();
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
