using System;

namespace adyen_dotnet_in_person_payments_loyalty_example.Models.Responses
{
    public class CreateCardAcquisitionResponse
    {
        public string PoiTransactionId { get; set; }
        public DateTime? PoiTransactionTimeStamp { get; set; }
        
        public string CardCountryCode { get; set; } // The three-digit code of the issuer country
        public string PaymentToken { get; set; } // The card alias
        
        public string Alias { get; set; }
        public string PaymentAccountReference { get; set; }
        
        // For tax-free shopping:
        public string CardBin { get; set; }
        // For tax-free shopping:
        public string IssuerCountry { get; set; }
        
        // Loyalty
        public string ShopperReference { get; set; }
        public string ShopperEmail { get; set; }
        
        public bool GiftCardIndicator { get; set; }
    }
}