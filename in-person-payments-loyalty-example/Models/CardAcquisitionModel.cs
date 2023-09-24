using Adyen.Model.Nexo;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace adyen_dotnet_in_person_payments_loyalty_example.Models
{
    public class CardAcquisitionModel
    {
        public string POIReconciliationID { get; set; }
        public string PoiTransactionId { get; set; }
        public DateTime? PoiTransactionTimeStamp { get; set; }


        public string CardCountryCode { get; set; } // The three-digit code of the issuer country
        public string PaymentBrand { get; set; }
        public string MaskedPAN { get; set; }
        public string PaymentToken { get; set; } // The card alias
        public string ExpiryDate { get; set; } // "0228"
        public PaymentInstrumentType PaymentInstrumentType { get; set; } // Card

        public string SaleTransactionId { get; set; }
        public DateTime? SaleTransactionTimeStamp { get; set; }

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

        public JObject ToJson()
        {
            PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            JObject jsonResult = new JObject();

            foreach (PropertyInfo property in properties)
            {
                string propertyName = property.Name;
                object propertyValue = property.GetValue(this, null);
                jsonResult.Add(propertyName, propertyValue != null ? new JValue(propertyValue) : null);
            }

            return jsonResult;
        }
    }
}