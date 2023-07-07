using Adyen.HttpClient;
using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
using adyen_dotnet_authorisation_adjustment_example.Models;
using adyen_dotnet_authorisation_adjustment_example.Options;
using adyen_dotnet_authorisation_adjustment_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace adyen_dotnet_authorisation_adjustment_example.Services
{
    public class AuthorisationAdjustmentService
    {
        private readonly IBookingPaymentRepository _repository;
        private readonly IModificationsService _modificationsService;
        private readonly string _merchantAccount;

        public AuthorisationAdjustmentService(IBookingPaymentRepository repository, IModificationsService modificationsService, IOptions<AdyenOptions> options)
        {
            _repository = repository;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
            _modificationsService = modificationsService;
        }

        public async Task<PaymentAmountUpdateResource> IncrementalAdjustment(string reference, string pspReference, int amount, CancellationToken cancellationToken = default)
        {
            // TODO: handle incremental, decremental, extension.
            try
            {
                var request = new CreatePaymentAmountUpdateRequest()
                {
                    MerchantAccount = _merchantAccount, // Required
                    Amount = new Amount() { Value = amount, Currency = "EUR" },
                    Reference = reference,
                    IndustryUsage = CreatePaymentAmountUpdateRequest.IndustryUsageEnum.DelayedCharge,
                };

                var response = await _modificationsService.UpdateAuthorisedAmountAsync(pspReference, request, cancellationToken: cancellationToken);
                return response;
            }
            catch (HttpClientException e)
            {
                //ViewBag.Message = $"Incremental adjustment failed for PSPReference {pspReference}. See error logs for the exception.";
                //ViewBag.Img = "failed";
                throw;
            }
            //try
            //{
            //    PaymentResponse result = await _checkoutClient.MakePaymentAsync(ShopperReference.Value, recurringDetailReference, cancellationToken);

            //    switch (result.ResultCode)
            //    {
            //        // Handle other payment response cases here.
            //        case PaymentResponse.ResultCodeEnum.Authorised:
            //            ViewBag.Message = $"Successfully authorised a payment with RecurringDetailReference: {recurringDetailReference}.";
            //            ViewBag.Img = "success";
            //            break;
            //        default:
            //            ViewBag.Message = $"Payment failed for RecurringDetailReference {recurringDetailReference}. See error logs for the message.";
            //            ViewBag.Img = "failed";
            //            break;
            //    }
            //}   
            //catch (HttpClientException)
            //{
            //    ViewBag.Message = $"Payment failed for RecurringDetailReference {recurringDetailReference}. See error logs for the exception.";
            //    ViewBag.Img = "failed";
            //}
        }

        [Route("admin/capture/{reference}/{pspReference}/{amount}")] // TODO: Move amount/reference into its own storage, it should not be passed on as parameter.
        public async Task<PaymentCaptureResource> Capture(string reference, string pspReference, int amount, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new CreatePaymentCaptureRequest()
                {
                    MerchantAccount = _merchantAccount, // Required.
                    Amount = new Amount() { Value = amount, Currency = "EUR" }, // Required.
                    Reference = reference,
                };

                var response = await _modificationsService.CaptureAuthorisedPaymentAsync(pspReference, request, cancellationToken: cancellationToken);
                return response; // Note that the response will have a DIFFERENT PSPReference compared to the initial preauth
            }
            catch (HttpClientException e)
            {
                //ViewBag.Message = $"Incremental adjustment failed for PSPReference {pspReference}. See error logs for the exception.";
                //ViewBag.Img = "failed";
                throw;
            }
        }

        [Route("admin/cancel-pre-authorisation/{pspReference}/{reference}")] // Move reference
        public async Task<PaymentCancelResource> CancelPreAuthorisation(string pspReference, string reference, CancellationToken cancellationToken = default)
        {

            try
            {
                var request = new CreatePaymentCancelRequest()
                {
                    MerchantAccount = _merchantAccount, // Required.
                    Reference = reference,
                };

                var response = await _modificationsService.CancelAuthorisedPaymentByPspReferenceAsync(pspReference, request, cancellationToken: cancellationToken);
                return response;
            }
            catch (HttpClientException e)
            {
                //ViewBag.Message = $"Incremental adjustment failed for PSPReference {pspReference}. See error logs for the exception.";
                //ViewBag.Img = "failed";
                throw;
            }
        }
    }
}
