using System;
using System.Collections.Generic;
using System.Net;
using Adyen;
using Adyen.Model.Checkout;
using Adyen.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static Adyen.Model.Checkout.PaymentDetailsResponse;

namespace adyen_dotnet_online_payments.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly Checkout _checkout;
        private readonly string _merchant_account;
        private readonly ILogger<ApiController> _logger;
        public ApiController(ILogger<ApiController> logger)
        {
            _logger = logger;
            var client = new Client(Environment.GetEnvironmentVariable("ADYEN_API_KEY"), Adyen.Model.Enum.Environment.Test); // Test Environment;
            _checkout = new Checkout(client);
            _merchant_account = Environment.GetEnvironmentVariable("ADYEN_MERCHANT");
        }

        [HttpPost("api/getPaymentMethods")]
        public ActionResult<string> GetPaymentMethods([FromBody] PaymentMethodsRequest req)
        {
            _logger.LogInformation($"Request for PaymentMethods API::\n{req}\n");

            req.MerchantAccount = _merchant_account;
            req.Channel = PaymentMethodsRequest.ChannelEnum.Web;

            try
            {
                return _checkout.PaymentMethods(req).ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentMethods failed::\n{e.ResponseBody}\n");
                throw e;
            }
        }

        [HttpPost("api/sessions")]
        public ActionResult<string> Sessions()
        {
            var sessionsRequest = new CreateCheckoutSessionRequest();
            sessionsRequest.merchantAccount = _merchant_account; // required
            sessionsRequest.channel = (CreateCheckoutSessionRequest.ChannelEnum?) PaymentRequest.ChannelEnum.Web; // required

            var amount = new Amount("EUR", 1000); // value is 10€ in minor units
            sessionsRequest.amount = amount;
            var orderRef = System.Guid.NewGuid();
            sessionsRequest.reference = orderRef.ToString(); // required
            
            // required for 3ds2 redirect flow
            sessionsRequest.returnUrl = $"https://localhost:5001/Home/Redirect?orderRef={orderRef}";

            try
            {
                var res = _checkout.Sessions(sessionsRequest);
                _logger.LogInformation($"Response for Payment API::\n{res}\n");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed::\n{e.ResponseBody}\n");
                throw e;
            }
        }
        
    }
}