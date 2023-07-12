using Adyen.HttpClient;
using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
using adyen_dotnet_authorisation_adjustment_example.Models;
using adyen_dotnet_authorisation_adjustment_example.Options;
using adyen_dotnet_authorisation_adjustment_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_authorisation_adjustment_example.Controllers
{
    public class AdminController : Controller
    {
        private readonly IBookingPaymentRepository _repository;
        private readonly IModificationsService _modificationsService;
        private readonly string _merchantAccount;
        
        public AdminController(IBookingPaymentRepository repository,  IModificationsService modificationsService, IOptions<AdyenOptions> options)
        {
            _repository = repository;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
            _modificationsService = modificationsService;
        }

        [Route("admin")]
        public IActionResult Index()
        {
            List<BookingPayment> bookingPayments = new List<BookingPayment>();

            // We fetch all booking payments that we have stored in our (local) repository and show it.
            foreach (KeyValuePair<string, BookingPayment> kvp in _repository.BookingPayments)
            {
                bookingPayments.Add(kvp.Value);
            }
            ViewBag.BookingPayments = bookingPayments;
            return View();
        }

        [HttpPost("admin/update-payment-amount")]
        public async Task<ActionResult<PaymentAmountUpdateResource>> UpdatePaymentAmount(UpdatePaymentAmountRequest request, CancellationToken cancellationToken = default)
        {
            if (!_repository.BookingPayments.TryGetValue(request.PspReference, out var bookingPayment))
            {
                return NotFound();
            }

            try
            {
                var createPaymentAmountUpdateRequest = new CreatePaymentAmountUpdateRequest()
                {
                    MerchantAccount = _merchantAccount, // Required
                    Amount = new Amount() { Value = request.Amount, Currency = bookingPayment.Currency },
                    Reference = bookingPayment.Reference,
                    IndustryUsage = CreatePaymentAmountUpdateRequest.IndustryUsageEnum.DelayedCharge,
                };
                
                var response = await _modificationsService.UpdateAuthorisedAmountAsync(request.PspReference, createPaymentAmountUpdateRequest, cancellationToken: cancellationToken);
                return Ok(response);
            }
            catch (HttpClientException e)
            {
                return BadRequest(e);
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

        [HttpPost("admin/create-capture")]
        public async Task<ActionResult<PaymentCaptureResource>> CreateCapture(CreateCaptureRequest request, CancellationToken cancellationToken = default)
        {
            if (!_repository.BookingPayments.TryGetValue(request.PspReference, out var bookingPayment))
            {
                return NotFound();
            }

            try
            {
                var createPaymentCaptureRequest = new CreatePaymentCaptureRequest()
                {
                    MerchantAccount = _merchantAccount, // Required.
                    Amount = new Amount() { Value = request.Amount, Currency = bookingPayment.Currency }, // Required.
                    Reference = bookingPayment.Reference
                };
                
                var response = await _modificationsService.CaptureAuthorisedPaymentAsync(request.PspReference, createPaymentCaptureRequest, cancellationToken: cancellationToken);
                return Ok(response); // Note that the response will have a DIFFERENT PSPReference compared to the initial preauth
            }
            catch (HttpClientException e)
            {
                return BadRequest(e);
            }
        }
        
        [HttpPost("admin/cancel-authorised-payment")]
        public async Task<ActionResult<PaymentCancelResource>> CancelAuthorisedPaymentRequest(CancelAuthorisedPaymentRequest request, CancellationToken cancellationToken = default)
        {
            if (!_repository.BookingPayments.TryGetValue(request.PspReference, out var bookingPayment))
            {
                return NotFound();
            }

            try
            {
                var createPaymentCancelRequest = new CreatePaymentCancelRequest()
                {
                    MerchantAccount = _merchantAccount, // Required.
                    Reference = bookingPayment.Reference
                };
                
                var response = await _modificationsService.CancelAuthorisedPaymentByPspReferenceAsync(request.PspReference, createPaymentCancelRequest, cancellationToken: cancellationToken);
                return Ok(response);
            }
            catch (HttpClientException e)
            {
                return BadRequest(e);
            }
        }
    }
}
