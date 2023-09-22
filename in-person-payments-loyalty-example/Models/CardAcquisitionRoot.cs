namespace adyen_dotnet_in_person_payments_loyalty_example.Models
{
    public class AdditionalData
    {
        public string PaymentAccountReference { get; set; }
        public string Alias { get; set; }
        public string AliasType { get; set; }
        public string CardBin { get; set; }
        public string IssuerCountry { get; set; }
        public string ShopperEmail { get; set; } // Not set
        public string ShopperReference { get; set; }
        public bool GiftcardIndicator { get; set; } // Indicates true if shopper presented a gift card.
        
        public string ApplicationLabel { get; set; }
        public string ApplicationPreferredName { get; set; }
        public bool BackendGiftcardIndicator { get; set; }
        public string CardHolderName { get; set; }
        public string CardIssuerCountryId { get; set; }
        public string CardScheme { get; set; }
        public string CardSummary { get; set; }
        public string CardType { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public string FundingSource { get; set; }
        public string Iso8601TxDate { get; set; }
        public string MerchantReference { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentMethodVariant { get; set; }
        public string PosAmountCashbackValue { get; set; }
        public string PosAmountGratuityValue { get; set; }
        public string PosAuthAmountCurrency { get; set; }
        public string PosAuthAmountValue { get; set; }
        public string PosEntryMode { get; set; }
        public string PosOriginalAmountValue { get; set; }
        public string PosadditionalamountsOriginalAmountCurrency { get; set; }
        public string ShopperCountry { get; set; }
        public string Tid { get; set; }
        public string TransactionType { get; set; }
        public string Txdate { get; set; }
        public string Txtime { get; set; }
        
    }

    public class CardAcquisitionRoot
    {
        public AdditionalData AdditionalData { get; set; }
        public string Message { get; set; }
        public string Store { get; set; }
    }
}
