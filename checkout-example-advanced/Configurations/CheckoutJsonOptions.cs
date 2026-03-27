using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace adyen_dotnet_checkout_example_advanced.Options
{
    public class CheckoutJsonOptions : IConfigureOptions<JsonOptions>
    {
        private readonly Adyen.Checkout.Client.JsonSerializerOptionsProvider _jsonSerializerOptionsProvider;

        public CheckoutJsonOptions(Adyen.Checkout.Client.JsonSerializerOptionsProvider jsonSerializerOptionsProvider)
        {
            _jsonSerializerOptionsProvider = jsonSerializerOptionsProvider;
        }

        // Register all JsonConverters to AspNetCore.Mvc. Used in the controllers. See Startup.cs -> ConfigureServices(...).
        public void Configure(JsonOptions options)
        {
            foreach (JsonConverter converter in _jsonSerializerOptionsProvider.Options.Converters)
            {
                options.JsonSerializerOptions.Converters.Add(converter);
            }

            options.JsonSerializerOptions.PropertyNamingPolicy = _jsonSerializerOptionsProvider.Options.PropertyNamingPolicy;
            options.JsonSerializerOptions.DefaultIgnoreCondition = _jsonSerializerOptionsProvider.Options.DefaultIgnoreCondition;
        }
    }
}