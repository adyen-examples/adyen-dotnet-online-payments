using System.Collections.Generic;

namespace adyen_dotnet_subscription_example.Models
{
    public class SubscribedCustomer
    {
        public string ShopperReference { get; set; }
        public List<SubscribedCustomerDetails> SubscribedCustomerDetails { get; set; }
    }

    public class SubscribedCustomerDetails
    {
        public string PspReference { get; set; }
        public string RecurringDetailReference { get; set; }
    }
}