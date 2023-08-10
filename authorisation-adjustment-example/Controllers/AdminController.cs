﻿using Adyen.HttpClient;
using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
using adyen_dotnet_authorisation_adjustment_example.Models;
using adyen_dotnet_authorisation_adjustment_example.Options;
using adyen_dotnet_authorisation_adjustment_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_authorisation_adjustment_example.Controllers
{
    public class AdminController : Controller
    {
        private readonly IPaymentRepository _repository;
        private readonly IModificationsService _modificationsService;
        private readonly ILogger<AdminController> _logger;
        private readonly string _merchantAccount;

        public AdminController(IPaymentRepository repository, IModificationsService modificationsService, IOptions<AdyenOptions> options, ILogger<AdminController> logger)
        {
            _repository = repository;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
            _modificationsService = modificationsService;
            _logger = logger;
        }

        [Route("admin")]
        public IActionResult Index()
        {
            List<PaymentModel> payments = new List<PaymentModel>();
            foreach (var kvp in _repository.Payments)
            {
                payments.Add(_repository.FindLatestPaymentByReference(kvp.Key));
            }
            ViewBag.Payments = payments;
            return View();
        }

        [Route("admin/details/{reference}")]
        public IActionResult Details(string reference)
        {
            // We fetch all payments (regardless whether its authorised/refused) that we have stored in our local repository and show it.
            ViewBag.Payments = _repository.FindByReference(reference)
                .OrderBy(x=> x.DateTime)
                .ToList();
            return View();
        }

        [HttpGet("admin/result/{status}/{reference}")]
        public IActionResult Result(string reference, string status, [FromQuery(Name = "reason")] string refusalReason)
        {
            string msg;
            string img;
            switch (status)
            {
                case "received":
                    msg = $"Request received for Merchant Reference: {reference}. Wait a bit to receive the asynchronous webhook response.";
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
        public async Task<ActionResult<PaymentAmountUpdateResponse>> UpdatePaymentAmount([FromBody] UpdatePaymentAmountRequest request, CancellationToken cancellationToken = default)
        {
            PaymentModel payment = _repository.FindLatestPaymentByReference(request.Reference);

            if (payment == null)
            {
                return NotFound();
            }

            try
            {
                var paymentAmountUpdateRequest = new PaymentAmountUpdateRequest()
                {
                    MerchantAccount = _merchantAccount, // Required
                    Amount = new Amount() { Value = request.Amount, Currency = payment.Currency },
                    Reference = payment.Reference,
                    IndustryUsage = PaymentAmountUpdateRequest.IndustryUsageEnum.DelayedCharge,
                };
                
                var response = await _modificationsService.UpdateAuthorisedAmountAsync(payment.GetOriginalPspReference(), paymentAmountUpdateRequest, cancellationToken: cancellationToken);
                return Ok(response);
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return BadRequest();
            }
        }

        [HttpPost("admin/capture-payment")]
        public async Task<ActionResult<PaymentCaptureResponse>> CapturePayment([FromBody] CreateCapturePaymentRequest request, CancellationToken cancellationToken = default)
        {
            PaymentModel payment = _repository.FindLatestPaymentByReference(request.Reference);

            if (payment == null)
            {
                return NotFound();
            }

            try
            {
                var paymentCaptureRequest = new PaymentCaptureRequest()
                {
                    MerchantAccount = _merchantAccount, // Required.
                    Amount = new Amount() { Value = payment.Amount, Currency = payment.Currency }, // Required.
                    Reference = payment.Reference
                };
                
                var response = await _modificationsService.CaptureAuthorisedPaymentAsync(payment.GetOriginalPspReference(), paymentCaptureRequest, cancellationToken: cancellationToken);
                return Ok(response); // Note that the response will have a different PSPReference compared to the initial pre-authorisation.
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return BadRequest();
            }
        }

        [HttpPost("admin/reversal-payment")]
        public async Task<ActionResult<PaymentReversalResponse>> ReversalPayment([FromBody] CreateReversalPaymentRequest request, CancellationToken cancellationToken = default)
        {
            PaymentModel payment = _repository.FindLatestPaymentByReference(request.Reference);

            if (payment == null)
            {
                return NotFound();
            }

            try
            {
                var paymentReversalRequest = new PaymentReversalRequest()
                {
                    MerchantAccount = _merchantAccount, // Required.
                    Reference = payment.Reference
                };

                var response = await _modificationsService.RefundOrCancelPaymentAsync(payment.GetOriginalPspReference(), paymentReversalRequest, cancellationToken: cancellationToken);
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
