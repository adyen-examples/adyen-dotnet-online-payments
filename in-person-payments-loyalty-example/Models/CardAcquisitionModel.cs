namespace adyen_dotnet_in_person_payments_loyalty_example.Models
{
    public class CardAcquisitionModel
    {
        public AdditionalData AdditionalData { get; set; }
        public string Message { get; set; }
        public string Store { get; set; }
    }

    /// <summary>
    /// See https://docs.adyen.com/point-of-sale/card-acquisition/identifiers/#example for a list of additionalData properties.
    /// </summary>
    public class AdditionalData
    {
        public string PaymentAccountReference { get; set; }
        public string Alias { get; set; }
        public string ShopperEmail { get; set; }
        public string ShopperReference { get; set; }
        public bool GiftcardIndicator { get; set; }
    }
}
