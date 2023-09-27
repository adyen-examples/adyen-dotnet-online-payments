using System;

namespace adyen_dotnet_in_person_payments_example.Models.Responses
{
    public class CreatePaymentResponse
    {
        public string Result { get; set; }
        public string RefusalReason { get; set; }
    }
}
