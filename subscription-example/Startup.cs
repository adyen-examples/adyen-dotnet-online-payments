using Adyen;
using Adyen.Model.Enum;
using Adyen.Service;
using adyen_dotnet_subscription_example.Clients;
using adyen_dotnet_subscription_example.Options;
using adyen_dotnet_subscription_example.Repositories;
using adyen_dotnet_subscription_example.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace adyen_dotnet_subscription_example
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
            services.Configure<AdyenOptions>(
                options =>
                {
                    options.ADYEN_API_KEY = Configuration[nameof(AdyenOptions.ADYEN_API_KEY)];
                    options.ADYEN_MERCHANT_ACCOUNT = Configuration[nameof(AdyenOptions.ADYEN_MERCHANT_ACCOUNT)];
                    options.ADYEN_CLIENT_KEY = Configuration[nameof(AdyenOptions.ADYEN_CLIENT_KEY)];
                    options.ADYEN_HMAC_KEY = Configuration[nameof(AdyenOptions.ADYEN_HMAC_KEY)];
                }
            );
            
            services.AddControllersWithViews();
            services.AddControllers().AddNewtonsoftJson();
            services.AddHttpContextAccessor();
            services.AddTransient<IUrlService, UrlService>();

            services.AddSingleton<Client>(new Client(Configuration[nameof(AdyenOptions.ADYEN_API_KEY)], Environment.Test));
            services.AddSingleton<Checkout>();
            services.AddSingleton<Recurring>();
            services.AddSingleton<IRecurringClient, RecurringClient>();
            services.AddSingleton<ICheckoutClient, CheckoutClient>();
            services.AddSingleton<ISubscriptionRepository, SubscriptionRepository>();
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
