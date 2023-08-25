using Adyen.Model.Checkout;
using adyen_dotnet_authorisation_adjustment_example.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace adyen_dotnet_authorisation_adjustment_example.Repositories
{
    /// <summary>
    /// Acts as a local (in-memory) repository to store <see cref="PaymentModel"/>s by <see cref="PaymentModel.MerchantReference"/>.
    /// </summary>
    public interface IPaymentRepository
    {
        /// <summary>
        /// Dictionary of all payments for the hotel bookings by <see cref="PaymentModel.MerchantReference"/>.
        /// Key: <see cref="PaymentModel.MerchantReference"/>  |  
        /// Value: <see cref="PaymentModel"/> which contains the initial pre-authorisation and a history of the payment details.
        /// </summary>
        public Dictionary<string, PaymentModel> Payments { get; }

        /// <summary>
        /// Insert a new <see cref="PaymentModel"/> into the <see cref="Payments"/> dictionary with the <see cref="PaymentModel.MerchantReference"/> as its key.
        /// </summary>
        /// <param name="response">The <see cref="PaymentResponse"/> from the <see cref="Controllers.ApiController.PreAuthorisation(Adyen.Model.Checkout.PaymentRequest, System.Threading.CancellationToken)"/> call.</param>
        /// <returns>True if inserted.</returns>
        bool InsertPayment(PaymentResponse response);
        
        /// <summary>
        /// Upserts <see cref="PaymentDetailsModel"/> into the <see cref="PaymentModel.PaymentsHistory"/> list.
        /// This is used to keep track of the payment details history.
        /// </summary>
        /// <param name="paymentDetails"><see cref="PaymentDetailsModel"/>.</param>
        /// <returns>True if inserted.</returns>
        bool UpsertPaymentDetails(PaymentDetailsModel paymentDetails);
        
        /// <summary>
        /// Retrieves <see cref="PaymentModel"/> by <see cref="PaymentModel.MerchantReference"/>.
        /// </summary>
        /// <param name="merchantReference"><see cref="PaymentModel.MerchantReference"/>.</param>
        /// <returns><see cref="PaymentModel"/>.</returns>
        PaymentModel GetPayment(string merchantReference);
    }

    public class PaymentRepository : IPaymentRepository
    {
        public Dictionary<string, PaymentModel> Payments { get; }

        public PaymentRepository()
        {
            Payments = new Dictionary<string, PaymentModel>();
        }

        public bool InsertPayment(PaymentResponse response)
        {
            var payment = new PaymentModel()
            {
                MerchantReference = response.MerchantReference,
                PspReference = response.PspReference,
                Amount = response.Amount?.Value,
                Currency = response.Amount?.Currency,
                BookingDate = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                ExpiryDate = DateTimeOffset.UtcNow.AddDays(28), // The value of '28' varies per scheme, see https://docs.adyen.com/online-payments/adjust-authorisation/#validity.
                PaymentMethodBrand = response.PaymentMethod?.Brand,
                PaymentsHistory = new List<PaymentDetailsModel>()
                {
                    new PaymentDetailsModel()
                    {
                        PspReference = response.PspReference,
                        OriginalReference = null,
                        MerchantReference = response.MerchantReference,
                        Amount = response.Amount?.Value,
                        Currency = response.Amount?.Currency,
                        DateTime = DateTimeOffset.UtcNow,
                        ResultCode = response.ResultCode.ToString(),
                        RefusalReason = response.RefusalReason,
                        PaymentMethodBrand = response.PaymentMethod?.Brand,
                    }
                }
            };

            return Payments.TryAdd(payment.MerchantReference, payment);
        }

        public bool UpsertPaymentDetails(PaymentDetailsModel paymentDetails)
        {
            // If the `Merchant Reference` is not specified, throw an ArgumentNullException.
            if (string.IsNullOrWhiteSpace(paymentDetails.MerchantReference))
            {
                throw new ArgumentNullException();
            }

            // Check if the `Merchant Reference` is in the list, do nothing if we've never saved the reference.
            if (!Payments.TryGetValue(paymentDetails.MerchantReference, out var payment))
            {
                return false;
            }

            // Check if the `PspReference` already exists.
            PaymentDetailsModel existingPayment = payment.PaymentsHistory.FirstOrDefault(details => details.PspReference == payment.PspReference);

            // If the values are exactly the same (in the case of receiving a webhook multiple times).
            // We consider it a duplicate, and don't add anything.
            if (paymentDetails.IsEqual(existingPayment))
            {
                return false;
            }

            // If it doesn't exists, we add it.
            payment.PaymentsHistory.Add(paymentDetails);
            payment.LastUpdated = DateTimeOffset.UtcNow;
            return true;
        }

        public PaymentModel GetPayment(string merchantReference)
        {
            if (!Payments.TryGetValue(merchantReference, out PaymentModel result))
            {
                return null;
            }

            return result;
        }
    }
}