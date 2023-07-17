using Adyen.HttpClient;
using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
using adyen_dotnet_authorisation_adjustment_example.Models;
using adyen_dotnet_authorisation_adjustment_example.Options;
using adyen_dotnet_authorisation_adjustment_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_authorisation_adjustment_example.Controllers
{
    public class AdminController : Controller
    {
        private readonly IHotelPaymentRepository _repository;
        private readonly IModificationsService _modificationsService;
        private readonly ILogger<AdminController> _logger;
        private readonly string _merchantAccount;

        public AdminController(IHotelPaymentRepository repository, IModificationsService modificationsService, IOptions<AdyenOptions> options, ILogger<AdminController> logger)
        {
            _repository = repository;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
            _modificationsService = modificationsService;
            _logger = logger;
        }

        [Route("admin")]
        public IActionResult Index()
        {
            List<HotelPaymentModel> hotelPayments = new List<HotelPaymentModel>();

            // We fetch all hotel payments (regardless whether its authorised or not) that we have stored in our (local) repository and show it.
            foreach (KeyValuePair<string, HotelPaymentModel> kvp in _repository.HotelPayments)
            {
                hotelPayments.Add(kvp.Value);
            }

            hotelPayments.Add(new HotelPaymentModel()
            {
                Amount = 1234,
                Currency = "EUR",
                DateTime = DateTime.UtcNow,
                PaymentMethodBrand = "scheme",
                PaymentMethodType = "mc",
                PspReference = "psp-1",
                Reference = "ref-1",
                ResultCode = "Authorised"
            });

            hotelPayments.Add(new HotelPaymentModel()
            {
                Amount = 5678,
                Currency = "EUR",
                DateTime = DateTime.UtcNow,
                PaymentMethodBrand = "scheme",
                PaymentMethodType = "visa",
                PspReference = "psp-2",
                Reference = "ref-2",
                ResultCode = "Authorised"
            });
            ViewBag.HotelPayments = hotelPayments;
            return View();
        }

        [HttpGet("admin/result/{status}/{pspReference}")]
        public IActionResult Result(string pspReference, string status, [FromQuery(Name = "reason")] string refusalReason)
        {
            string msg;
            string img;
            switch (status)
            {
                case "received":
                    msg = $"Request received for {pspReference}.";
                    img = "success";
                    break;
                default:
                    msg = $"Error! Refusal reason: {refusalReason}";
                    img = "failed";
                    break;
            }
            ViewBag.Status = status;
            ViewBag.Msg = msg;
            ViewBag.Img = img;
            return View();
        }


        [HttpPost("admin/update-payment-amount")]
        public async Task<ActionResult<PaymentAmountUpdateResource>> UpdatePaymentAmount([FromBody] UpdatePaymentAmountRequest request, CancellationToken cancellationToken = default)
        {
            var hotelPayment = _repository.GetByPspReference(request.PspReference);

            if (hotelPayment == null)
            {
                return NotFound();
            }

            try
            {
                var createPaymentAmountUpdateRequest = new CreatePaymentAmountUpdateRequest()
                {
                    MerchantAccount = _merchantAccount, // Required
                    Amount = new Amount() { Value = request.Amount, Currency = hotelPayment.Currency },
                    Reference = hotelPayment.Reference,
                    IndustryUsage = CreatePaymentAmountUpdateRequest.IndustryUsageEnum.DelayedCharge,
                };
                
                var response = await _modificationsService.UpdateAuthorisedAmountAsync(request.PspReference, createPaymentAmountUpdateRequest, cancellationToken: cancellationToken);
                return Ok(response);
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return BadRequest();
            }
        }

        [HttpPost("admin/capture-payment")]
        public async Task<ActionResult<PaymentCaptureResource>> CapturePayment([FromBody] CreateCapturePaymentRequest request, CancellationToken cancellationToken = default)
        {
            var hotelPayment = _repository.GetByPspReference(request.PspReference);

            if (hotelPayment == null)
            {
                return NotFound();
            }

            try
            {
                var createPaymentCaptureRequest = new CreatePaymentCaptureRequest()
                {
                    MerchantAccount = _merchantAccount, // Required.
                    Amount = new Amount() { Value = hotelPayment.Amount, Currency = hotelPayment.Currency }, // Required.
                    Reference = hotelPayment.Reference
                };
                
                var response = await _modificationsService.CaptureAuthorisedPaymentAsync(request.PspReference, createPaymentCaptureRequest, cancellationToken: cancellationToken);
                return Ok(response); // Note that the response will have a different PSPReference compared to the initial preauthorisation.
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return BadRequest();
            }
        }

        [HttpPost("admin/reversal-payment")]
        public async Task<ActionResult<PaymentReversalResource>> ReversalPayment([FromBody] CreateReversalPaymentRequest request, CancellationToken cancellationToken = default)
        {
            var hotelPayment = _repository.GetByPspReference(request.PspReference);

            if (hotelPayment == null)
            {
                return NotFound();
            }

            try
            {
                var createPaymentReversalRequest = new CreatePaymentReversalRequest()
                {
                    MerchantAccount = _merchantAccount, // Required.
                    Reference = hotelPayment.Reference
                };

                var response = await _modificationsService.RefundOrCancelPaymentAsync(request.PspReference, createPaymentReversalRequest, cancellationToken: cancellationToken);
                return Ok(response);
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return BadRequest();
            }
        }
    }
}
