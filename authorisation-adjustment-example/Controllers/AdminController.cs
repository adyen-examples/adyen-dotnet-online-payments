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
using Microsoft.Extensions.Logging;

namespace adyen_dotnet_authorisation_adjustment_example.Controllers
{
    public class AdminController : Controller
    {
        private readonly IHotelPaymentRepository _repository;
        private readonly IModificationsService _modificationsService;
        private readonly ILogger<AdminController> _logger;
        private readonly string _merchantAccount;
        
        public AdminController(IHotelPaymentRepository repository,  IModificationsService modificationsService, IOptions<AdyenOptions> options, ILogger<AdminController> logger)
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
            ViewBag.HotelPayments = hotelPayments;
            return View();
        }

        [HttpPost("admin/update-payment-amount")]
        public async Task<ActionResult<PaymentAmountUpdateResource>> UpdatePaymentAmount(UpdatePaymentAmountRequest request, CancellationToken cancellationToken = default)
        {
            if (!_repository.HotelPayments.TryGetValue(request.PspReference, out var hotelPayment))
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

        [HttpPost("admin/create-capture")]
        public async Task<ActionResult<PaymentCaptureResource>> CreateCapture(CreateCaptureRequest request, CancellationToken cancellationToken = default)
        {
            if (!_repository.HotelPayments.TryGetValue(request.PspReference, out var hotelPayment))
            {
                return NotFound();
            }

            try
            {
                var createPaymentCaptureRequest = new CreatePaymentCaptureRequest()
                {
                    MerchantAccount = _merchantAccount, // Required.
                    Amount = new Amount() { Value = request.Amount, Currency = hotelPayment.Currency }, // Required.
                    Reference = hotelPayment.Reference
                };
                
                var response = await _modificationsService.CaptureAuthorisedPaymentAsync(request.PspReference, createPaymentCaptureRequest, cancellationToken: cancellationToken);
                return Ok(response); // Note that the response will have a DIFFERENT PSPReference compared to the initial preauth
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return BadRequest();
            }
        }
        
        [HttpPost("admin/cancel-authorised-payment")]
        public async Task<ActionResult<PaymentCancelResource>> CancelAuthorisedPaymentRequest(CancelAuthorisedPaymentRequest request, CancellationToken cancellationToken = default)
        {
            if (!_repository.HotelPayments.TryGetValue(request.PspReference, out var hotelPayment))
            {
                return NotFound();
            }

            try
            {
                var createPaymentCancelRequest = new CreatePaymentCancelRequest()
                {
                    MerchantAccount = _merchantAccount, // Required.
                    Reference = hotelPayment.Reference
                };
                
                var response = await _modificationsService.CancelAuthorisedPaymentByPspReferenceAsync(request.PspReference, createPaymentCancelRequest, cancellationToken: cancellationToken);
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
