using Adyen;
using Adyen.Checkout.Services;
using Adyen.Util;
using adyen_dotnet_checkout_example.Services;
using Adyen.Webhooks.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace adyen_dotnet_checkout_example
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
            // Register controllers.
            services.AddControllersWithViews();
            services.AddControllers();

            services.AddHttpContextAccessor()
                .AddTransient<IUrlService, UrlService>();
            
            
            // > Option [1]: Registers *all* services:
            // IDonationsService, IModificationsService, IOrdersService, IPaymentLinksService, IPaymentsService, IRecurringService, IUtilityService, 
            //services.AddAllCheckoutServices();
            
            
            // > Option [2]: Registers *individual* service: IPaymentsService.
            //services.AddPaymentsService(); // Defaults to- `serviceLifetime: ServiceLifetime.Singleton`
            
            
            // > Option [3]: Register *individual* service manually: IPaymentsService.
            services.AddScoped<IPaymentsService, PaymentsService>()
                .AddHttpClient<IPaymentsService, PaymentsService>();
                //.AddDefaultLogger();

            // Register WebhookHandlers to validate HMAC signature when receiving webhooks in the WebhookController.cs
            // The ADYEN_HMAC_KEY is passed in Startup.cs when the ConfigureWebhooks(..)-function is called.
            services.AddWebhooksHandler();
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
