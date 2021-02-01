using System;
using System.Collections.Generic;
using System.Net;
using Adyen;
using Adyen.Model.Checkout;
using Adyen.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Adyen.Model.Checkout.PaymentResponse;

namespace adyen_dotnet_online_payments.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly Checkout _checkout;
        private readonly string _merchant_account;
        private readonly ILogger<ApiController> _logger;
        private IMemoryCache _cache;
        public ApiController(ILogger<ApiController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            var client = new Client(Environment.GetEnvironmentVariable("ADYEN_API_KEY"), Adyen.Model.Enum.Environment.Test); // Test Environment;
            _checkout = new Checkout(client);
            _merchant_account = Environment.GetEnvironmentVariable("ADYEN_MERCHANT");
            _cache = memoryCache;
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

        [HttpPost("api/initiatePayment")]
        public ActionResult<string> InitiatePayment([FromBody] PaymentRequest pmreq)
        {
            _logger.LogInformation($"Request for Payments API::\n{pmreq}\n");

            pmreq.MerchantAccount = _merchant_account; // required
            pmreq.Channel = PaymentRequest.ChannelEnum.Web; // required

            var pmType = pmreq.PaymentMethod.Type;
            var amount = new Amount(findCurrency(pmType), 1000); // value is 10€ in minor units
            pmreq.Amount = amount;
            var orderRef = System.Guid.NewGuid();
            pmreq.Reference = orderRef.ToString(); // required

            // required for 3ds2 native flow
            pmreq.AdditionalData = new Dictionary<string, string>() { { "allow3DS2", "true" } };
            // required for 3ds2 native flow
            pmreq.Origin = "https://localhost:5001";

            pmreq.ShopperIP = HttpContext.Connection.RemoteIpAddress.ToString(); // required by some issuers for 3ds2

            // we pass the orderRef in return URL to get paymentData during redirects
            // required for 3ds2 redirect flow
            pmreq.ReturnUrl = $"https://localhost:5001/api/handleShopperRedirect?orderRef={orderRef}";
            // Required for Klarna:
            if (pmType.Contains("klarna"))
            {
                pmreq.CountryCode = "DE";
                pmreq.ShopperReference = "12345";
                pmreq.ShopperEmail = "youremail@email.com";
                pmreq.ShopperLocale = "en_US";
                pmreq.LineItems = new List<LineItem>()
                {
                    new LineItem(
                        amountExcludingTax: 331,
                        amountIncludingTax: 400,
                        description: "Sunglasses",
                        id: "Item 1",
                        imageUrl: "",
                        productUrl: "",
                        quantity: 1,
                        taxAmount: 69,
                        taxCategory: LineItem.TaxCategoryEnum.None,
                        taxPercentage: 2100
                    ),
                    new LineItem(
                        amountExcludingTax: 248,
                        amountIncludingTax: 300,
                        description: "Headphones",
                        id: "Item 2",
                        imageUrl: "",
                        productUrl: "",
                        quantity: 1,
                        taxAmount: 52,
                        taxCategory: LineItem.TaxCategoryEnum.None,
                        taxPercentage: 2100
                    )
                };
            }

            try
            {
                var res = _checkout.Payments(pmreq);
                _logger.LogInformation($"Response for Payment API::\n{res}\n");
                if (res.PaymentData != "")
                {
                    _logger.LogInformation($"Setting payment data cache for {orderRef}\n");
                    _cache.Set(orderRef.ToString(), res.PaymentData);
                    return res.ToJson();
                }
                else
                {
                    var dict = new Dictionary<string, string>()
                    {
                        { "pspReference", res.PspReference },
                        { "resultCode", res.ResultCode.ToString() },
                        { "refusalReason", res.RefusalReason }
                    };
                    return JsonConvert.SerializeObject(dict);
                }
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed::\n{e.ResponseBody}\n");
                throw e;
            }
        }

        [HttpPost("api/submitAdditionalDetails")]
        public ActionResult<string> SubmitAdditionalDetails([FromBody] PaymentsDetailsRequest req)
        {
            _logger.LogInformation($"Request for PaymentDetails API::\n{req}\n");

            try
            {
                var res = _checkout.PaymentDetails(req);

                if (res.Action != null)
                {
                    return res.ToJson();
                }
                else
                {
                    var dict = new Dictionary<string, string>()
                    {
                        { "pspReference", res.PspReference },
                        { "resultCode", res.ResultCode.ToString() },
                        { "refusalReason", res.RefusalReason }
                    };
                    return JsonConvert.SerializeObject(dict);
                }
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentDetails failed::\n{e.ResponseBody}\n");
                throw e;
            }
        }

        [HttpGet("api/handleShopperRedirect")]
        public void RedirectGetAction([FromQuery(Name = "orderRef")] string orderRef, [FromQuery(Name = "payload")] string payload, [FromQuery(Name = "redirectResult")] string redirectResult)
        {
            var details = new PaymentCompletionDetails();
            if (payload != null)
            {
                details.Payload = payload;
            }
            if (redirectResult != null)
            {
                details.RedirectResult = redirectResult;
            }

            RedirectAction(orderRef, details);
        }

        [HttpPost("api/handleShopperRedirect")]
        public void RedirectPostAction([FromForm(Name = "MD")] string MD, [FromForm(Name = "PaRes")] string PaRes, [FromForm(Name = "payload")] string Payload, [FromQuery(Name = "orderRef")] string orderRef)
        {
            var details = new PaymentCompletionDetails();
            if (Payload != null)
            {
                details.Payload = Payload;
            }
            else
            {
                details.MD = MD;
                details.PaRes = PaRes;
            }

            RedirectAction(orderRef, details);
        }

        private void RedirectAction(string orderRef, PaymentCompletionDetails details)
        {
            _logger.LogInformation($"Redirect request received\nRef: {orderRef}");
            var paymentData = _cache.Get<string>(orderRef);

            var req = new PaymentsDetailsRequest(details: details, paymentData: paymentData);
            _logger.LogInformation($"Request for PaymentDetails API::\n{req}\n");
            try
            {
                var res = _checkout.PaymentDetails(req);
                _logger.LogInformation($"Response for PaymentDetails API::\n{res}\n");
                if (res.PspReference != "")
                {
                    string redirectURL;
                    // Conditionally handle different result codes for the shopper
                    switch (res.ResultCode)
                    {
                        case ResultCodeEnum.Authorised:
                            redirectURL = "/Home/Result/success";
                            break;
                        case ResultCodeEnum.Pending:
                        case ResultCodeEnum.Received:
                            redirectURL = "/Home/Result/pending";
                            break;
                        case ResultCodeEnum.Refused:
                            redirectURL = "/Home/Result/failed";
                            break;
                        default:
                            {
                                var reason = res.RefusalReason;
                                if (reason == "")
                                {
                                    reason = res.ResultCode.ToString();

                                }
                                redirectURL = $"/Home/Result/error?reason={WebUtility.UrlEncode(reason)}";
                                break;
                            }
                    }
                    // now redirect
                    Response.Redirect(redirectURL);
                }
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for PaymentDetails failed::\n{e.ResponseBody}\n");
                Response.Redirect($"/Home/Result/error?reason={WebUtility.UrlEncode(e.ResponseBody)}");
            }
        }

        private string findCurrency(string typ)
        {
            switch (typ)
            {
                case "ach":
                    return "USD";
                case "wechatpayqr":
                case "alipay":
                    return "CNY";
                case "dotpay":
                    return "PLN";
                case "boletobancario":
                case "boletobancario_santander":
                    return "BRL";
                default:
                    return "EUR";
            }
        }
    }
}