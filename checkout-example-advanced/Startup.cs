using adyen_dotnet_checkout_example_advanced.Options;
using adyen_dotnet_checkout_example_advanced.Services;
using Adyen.Checkout.Services;
using Adyen.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace adyen_dotnet_checkout_example_advanced
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
            services.AddControllers().AddNewtonsoftJson(); // Currently, webhooks models in WebhookController.cs use Newtonsoft.Json.
            services.AddSingleton<IConfigureOptions<JsonOptions>, CheckoutJsonOptions>(); // Use System.Text.Json for Checkout models.
            
            services.AddHttpContextAccessor()
                .AddTransient<IUrlService, UrlService>();
            
            // Three ways of registering the service: 
            
            // > Option [1]: Registers *all* services:
            // IDonationsService, IModificationsService, IOrdersService, IPaymentLinksService, IPaymentsService, IRecurringService, IUtilityService, 
            //services.AddAllCheckoutServices();

            // > Option [2]: Registers *individual* service: IPaymentsService.
            //services.AddPaymentsService(); // Defaults to- `serviceLifetime: ServiceLifetime.Singleton`
            
            // > Option [3]: Register *individual* service manually: IPaymentsService.
            services.AddScoped<IPaymentsService, PaymentsService>()
                .AddHttpClient<IPaymentsService, PaymentsService>();


            // Register HmacValidator to validate HMAC signature when receiving webhooks in the WebhookController.cs
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
