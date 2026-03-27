
using System.Text.Json.Serialization;
using Adyen.Checkout.Models;

namespace adyen_dotnet_checkout_example_advanced.Dtos
{
    /// <summary>
    /// Used in ApiController.cs /api/payments call.
    /// </summary>
    public class PaymentsDto
    {
        [JsonPropertyName("riskData")]
        public RiskDataDto RiskData { get; set; }

        [JsonPropertyName("paymentMethod")]
        public CheckoutPaymentMethod PaymentMethod { get; set; }

        [JsonPropertyName("browserInfo")]
        public BrowserInfoDto BrowserInfo { get; set; }

        [JsonPropertyName("origin")]
        public string Origin { get; set; }

        [JsonPropertyName("clientStateDataIndicator")]
        public bool ClientStateDataIndicator { get; set; }
    }

    public class RiskDataDto
    {
        [JsonPropertyName("clientData")]
        public string ClientData { get; set; }
        
        public static RiskData MapToRiskData(RiskDataDto dto)
        {
            if (dto == null)
                return null;
            
            return new RiskData
            {
                ClientData = dto.ClientData
            };
        }
    }
    
    public class BrowserInfoDto
    {
        [JsonPropertyName("acceptHeader")]
        public string AcceptHeader { get; set; }

        [JsonPropertyName("colorDepth")]
        public int ColorDepth { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("javaEnabled")]
        public bool JavaEnabled { get; set; }

        [JsonPropertyName("screenHeight")]
        public int ScreenHeight { get; set; }

        [JsonPropertyName("screenWidth")]
        public int ScreenWidth { get; set; }

        [JsonPropertyName("userAgent")]
        public string UserAgent { get; set; }

        [JsonPropertyName("timeZoneOffset")]
        public int TimeZoneOffset { get; set; }
        
        public static BrowserInfo MapToBrowserInfo(BrowserInfoDto dto)
        {
            if (dto == null)
                return null;
            
            return new BrowserInfo
            {
                AcceptHeader = dto.AcceptHeader,
                ColorDepth = dto.ColorDepth,
                JavaEnabled = dto.JavaEnabled,
                Language = dto.Language,
                ScreenHeight = dto.ScreenHeight,
                ScreenWidth = dto.ScreenWidth,
                TimeZoneOffset = dto.TimeZoneOffset,
                UserAgent = dto.UserAgent
            };
        }
    }
}