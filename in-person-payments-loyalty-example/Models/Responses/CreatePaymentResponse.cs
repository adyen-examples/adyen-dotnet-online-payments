﻿using System;

namespace adyen_dotnet_in_person_payments_loyalty_example.Models.Responses
{
    public class CreatePaymentResponse
    {
        public string Result { get; set; }
        public string RefusalReason { get; set; }

        public string SaleTransactionId { get; set; } // == SaleReferenceId
        public DateTime? SaleTransactionDateTime { get; set; }

        public string PoiTransactionId { get; set; }
        public DateTime? PoiTransactionDateTime { get; set; }
    }
}
