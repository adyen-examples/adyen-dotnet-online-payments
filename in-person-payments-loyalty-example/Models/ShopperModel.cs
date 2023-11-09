using Newtonsoft.Json.Linq;
using System.Reflection;

namespace adyen_dotnet_in_person_payments_loyalty_example.Models
{
    public class ShopperModel
    {
        public string Alias { get; set; } 
        public string ShopperEmail { get; set; }
        public string ShopperReference { get; set; }
        public bool IsSignedUpForLoyaltyProgram { get; set; }
        public int LoyaltyPoints { get; set; }

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