using System.Collections.Generic;

namespace adyen_dotnet_subscription_example.Models
{
    public class SubscribedCustomer
    {
        public string ShopperReference { get; set; }
        public List<SubscribedCustomerDetails> SubscribedCustomerDetails { get; set; }
    }
}