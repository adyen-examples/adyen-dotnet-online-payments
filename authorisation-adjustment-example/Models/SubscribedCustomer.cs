using System.Collections.Generic;

namespace adyen_dotnet_authorisation_adjustment_example.Models
{
    public class SubscribedCustomer // TODO
    {
        public string ShopperReference { get; set; }
        public List<SubscribedCustomerDetails> SubscribedCustomerDetails { get; set; }
    }
}