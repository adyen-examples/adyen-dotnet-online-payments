using Adyen.Model.Checkout;
using Adyen.Service;
using adyen_dotnet_checkout_example.Dtos.Requests;
using adyen_dotnet_giftcard_example.Options;
using adyen_dotnet_giftcard_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace adyen_dotnet_giftcard_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IUrlService _urlService;
        private readonly Checkout _checkout;
        private readonly string _merchantAccount;
        
        public ApiController(Checkout checkout, ILogger<ApiController> logger, IUrlService urlService, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _urlService = urlService;
            _checkout = checkout;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        [HttpPost("api/createorder")]
        public async Task<ActionResult<string>> CreateOrder()
        {
            var createOrderRequest = new CheckoutCreateOrderRequest(
                amount: new Amount("EUR", 10000), // value is 100€ in minor units
                merchantAccount: _merchantAccount,
                expiresAt: "2024-04-09T14:16:46Z", 
                reference: Guid.NewGuid().ToString() // This needs to match with cancelorder cancelorderRequest.orderData
            );

            try
            {
                var res = await _checkout.OrdersAsync(createOrderRequest);
                _logger.LogInformation($"Response from Orders API: {res}");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for CreateOrder failed: {e.ResponseBody}");
                throw;
            }
        }

        [HttpPost("api/cancelorder")]
        public async Task<ActionResult<string>> CancelOrder(string orderData, string pspReference)
        {
            var createOrderRequest = new CheckoutCancelOrderRequest( 
                order: new CheckoutOrder(orderData, pspReference),
                merchantAccount: _merchantAccount
            );

            try
            {
                var res = await _checkout.OrdersCancelAsync(createOrderRequest);
                _logger.LogInformation($"Response from Orders API: {res}");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for CancelOrder failed: {e.ResponseBody}");
                throw;
            }
        }

        [HttpPost("api/balancecheck")]
        public async Task<ActionResult<string>> BalanceCheck(BalanceCheckRequest request)
        {
            var balanceCheckRequest = new CheckoutBalanceCheckRequest(
                amount: new Amount("EUR", 11000), // value is 110€ in minor units
                merchantAccount: _merchantAccount,
                //string type, string number, string cvc, string holderName for plastic
                paymentMethod: new Dictionary<string, string>()
                {
                    { "type", request.Type },
                    { "number", request.Number },
                    { "cvc", request.Cvc }
                },
                reference: Guid.NewGuid().ToString()
            );

            try
            {
                var res = await _checkout.PaymentMethodsBalanceAsync(balanceCheckRequest);
                _logger.LogInformation($"Response from Orders API: {res}");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for BalanceCheck failed: {e.ResponseBody}");
                throw;
            }
        }

        [HttpPost("api/sessions/dropin")]
        public async Task<ActionResult<string>> SessionsDropin()
        {
            var sessionsRequest = new CreateCheckoutSessionRequest();
            sessionsRequest.MerchantAccount = _merchantAccount; // required
            sessionsRequest.Channel = CreateCheckoutSessionRequest.ChannelEnum.Web;
            sessionsRequest.Amount = new Amount("EUR", 11000); // value is 110€ in minor units

            var orderRef = Guid.NewGuid();
            sessionsRequest.Reference = orderRef.ToString(); // required

            // required for 3ds2 redirect flow
            sessionsRequest.ReturnUrl = $"{_urlService.GetHostUrl()}/dropinredirect?orderRef={orderRef}";

            try
            {
                var res = await _checkout.SessionsAsync(sessionsRequest);
                _logger.LogInformation($"Response: \n{res}\n");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request: {e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("api/sessions/giftcardcomponent")]
        public async Task<ActionResult<string>> SessionsGiftcardComponent()
        {
            var sessionsRequest = new CreateCheckoutSessionRequest();
            sessionsRequest.MerchantAccount = _merchantAccount; // required
            sessionsRequest.Channel = CreateCheckoutSessionRequest.ChannelEnum.Web;
            sessionsRequest.Amount = new Amount("EUR", 11000); // value is 110€ in minor units

            var orderRef = Guid.NewGuid();
            sessionsRequest.Reference = orderRef.ToString(); // required

            // required for 3ds2 redirect flow
            sessionsRequest.ReturnUrl = $"{_urlService.GetHostUrl()}/giftcardcomponentredirect?orderRef={orderRef}";

            try
            {
                var res = await _checkout.SessionsAsync(sessionsRequest);
                _logger.LogInformation($"Response: \n{res}\n");
                return res.ToJson();
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request: {e.ResponseBody}\n");
                throw;
            }
        }
    }
}