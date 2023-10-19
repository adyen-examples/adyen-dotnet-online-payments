using Adyen;
using Adyen.Service;
using Adyen.Util;
using adyen_dotnet_in_person_payments_loyalty_example.Options;
using adyen_dotnet_in_person_payments_loyalty_example.Repositories;
using adyen_dotnet_in_person_payments_loyalty_example.Services;
using adyen_dotnet_in_person_payments_loyalty_example.Services.CardAcquisition;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;

namespace adyen_dotnet_in_person_payments_loyalty_example
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
            // This will auto-retrieve/configure your keys from your environmental variables (ADYEN_API_KEY, ADYEN_HMAC_KEY, ADYEN_POS_POI_ID).
            services.Configure<AdyenOptions>(
                options =>
                {
                    options.ADYEN_API_KEY = Configuration[nameof(AdyenOptions.ADYEN_API_KEY)];
                    options.ADYEN_HMAC_KEY = Configuration[nameof(AdyenOptions.ADYEN_HMAC_KEY)];
                    options.ADYEN_POS_POI_ID = Configuration[nameof(AdyenOptions.ADYEN_POS_POI_ID)];
                    options.ADYEN_POS_SALE_ID = "SALE_ID_POS_42";
                }
            );

            // Register controllers.
            services.AddControllersWithViews();
            services.AddControllers().AddNewtonsoftJson();

            services.AddHttpContextAccessor();

            // Register Adyen Client.
            string httpClientName = "YourCustomHttpClientName";
            services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<AdyenOptions>>();
                return new Client(
                    new Config()
                    {
                        // Get your `API Key` from AdyenOptions using the Options pattern.
                        XApiKey = options.Value.ADYEN_API_KEY,
                        // Test environment.
                        Environment = Adyen.Model.Environment.Test,
                        Timeout = 180,
                    },
                    provider.GetRequiredService<IHttpClientFactory>(),
                    httpClientName
                );
            });

            // Register a named HttpClient for Adyen.Client.
            services.AddHttpClient(httpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(180); // https://docs.adyen.com/point-of-sale/design-your-integration/choose-your-architecture/cloud/#sync
            });

            // Register Adyen services and utilities.
            services.AddSingleton<IPosPaymentCloudApi, PosPaymentCloudApi>();
            services.AddSingleton<HmacValidator>();

            // Register repositories that saves data in-memory.
            services.AddSingleton<ITableRepository, TableRepository>();
            services.AddSingleton<ICardAcquisitionRepository, CardAcquisitionRepository>(); // TODO

            // Register application services.
            services.AddSingleton<IPosPaymentService, PosPaymentService>();
            services.AddSingleton<IPosTransactionStatusService, PosTransactionStatusService>();
            services.AddSingleton<IPosAbortService, PosAbortService>();
            services.AddSingleton<IPosCardAcquisitionService, PosCardAcquisitionService>();
            services.AddSingleton<IPosCardAcquisitionAbortService, PosCardAcquisitionAbortService>();
            services.AddSingleton<IPosCardAcquisitionPaymentService, PosCardAcquisitionPaymentService>();
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
