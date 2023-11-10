using System.Collections.Generic;

namespace adyen_dotnet_in_person_payments_loyalty_example.Models
{
    /// <summary>
    /// A representation of a unique shopper that is enrolled into the
    /// </summary>
    public class ShopperModel
    {
        public string Alias { get; set; } 
        public string ShopperEmail { get; set; }
        public string ShopperReference { get; set; }
        public bool IsSignedUpForLoyaltyProgram { get; set; }
        public int LoyaltyPoints { get; set; }
        
        public List<ShopperTransactionModel> TransactionHistory { get; set; }
    }
}