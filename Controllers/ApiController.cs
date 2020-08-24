using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Adyen;
using Adyen.Model.Checkout;
using Adyen.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
                _logger.LogError($"Request for PaymentMethods failed::\n{e}\n");
                throw e;
            }
        }

        [HttpPost("api/initiatePayment")]
        public ActionResult<string> InitiatePayment([FromBody] Dictionary<string, object> raw)
        {
            _logger.LogInformation($"Request for Payments API::\n{raw}\n");

            var paymentMethodToken = (Newtonsoft.Json.Linq.JObject)raw["paymentMethod"];
            var pmType = paymentMethodToken.GetValue("type").ToString();
            var pm = parsePaymentMethodDetails(JsonConvert.SerializeObject(paymentMethodToken), pmType);
            var amount = new Amount(findCurrency(pmType), 1000);
            var pmreq = new PaymentRequest();
            pmreq.PaymentMethod = pm;
            if (raw.ContainsKey("browserInfo"))
            {
                pmreq.BrowserInfo = JsonConvert.DeserializeObject<BrowserInfo>(JsonConvert.SerializeObject(raw["browserInfo"]));
            }
            if (raw.ContainsKey("origin"))
            {
                pmreq.Origin = JsonConvert.DeserializeObject<string>(JsonConvert.SerializeObject(raw["origin"]));
            }
            pmreq.MerchantAccount = _merchant_account;
            pmreq.Amount = amount;
            var orderRef = System.Guid.NewGuid();
            pmreq.Reference = orderRef.ToString();
            pmreq.Channel = PaymentRequest.ChannelEnum.Web;
            pmreq.AdditionalData = new Dictionary<string, string>() { { "allow3DS2", "true" } };
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
                        AmountExcludingTax: 331,
                        AmountIncludingTax: 400,
                        Description: "Sunglasses",
                        Id: "Item 1",
                        Quantity: 1,
                        TaxAmount: 69,
                        TaxCategory: LineItem.TaxCategoryEnum.None,
                        TaxPercentage: 2100,
                        ProductUrl: "",
                        ImageUrl: ""
                    ),
                    new LineItem(
                        AmountExcludingTax: 248,
                        AmountIncludingTax: 300,
                        Description: "Headphones",
                        Id: "Item 2",
                        Quantity: 1,
                        TaxAmount: 52,
                        TaxCategory: LineItem.TaxCategoryEnum.None,
                        TaxPercentage: 2100,
                        ProductUrl: "",
                        ImageUrl: ""
                    )
                };
            }

            try
            {
                var res = _checkout.Payments(pmreq);

                if (res.Action != null && res.Action.PaymentData != "")
                {
                    _logger.LogInformation($"Setting payment data cache for {orderRef}\n");
                    HttpContext.Session.SetString(orderRef.ToString(), res.Action.PaymentData);
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
                _logger.LogError($"Request for Payments failed::\n{e.Code} {e.JsonResponse} {e.ResponseBody}\n");
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
                _logger.LogError($"Request for PaymentDetails failed::\n{e}\n");
                throw e;
            }
        }

        [HttpGet("api/handleShopperRedirect")]
        public void RedirectGetAction([FromQuery(Name = "orderRef")] string orderRef, [FromQuery(Name = "payload")] string payload)
        {
            var details = new Dictionary<string, string>() {
                { "payload", payload }
            };

            RedirectAction(orderRef, details);
        }

        [HttpPost("api/handleShopperRedirect")]
        public void RedirectPostAction([FromQuery(Name = "orderRef")] string orderRef, [FromBody] RedirectReq redirect)
        {
            var details = new Dictionary<string, string>();
            if (redirect.Payload != "")
            {
                details["payload"] = redirect.Payload;
            }
            else
            {
                details["MD"] = redirect.MD;
                details["PaRes"] = redirect.PaRes;
            }

            RedirectAction(orderRef, details);
        }

        private void RedirectAction(string orderRef, Dictionary<string, string> details)
        {
            _logger.LogInformation($"Redirect request received\nRef: {orderRef}");
            var paymentData = HttpContext.Session.GetString(orderRef);

            var req = new PaymentsDetailsRequest(Details: details, PaymentData: paymentData);
            _logger.LogInformation($"Request for PaymentDetails API::\n{req}\n");
            var res = _checkout.PaymentDetails(req);
            _logger.LogInformation($"Response for PaymentDetails API::\n{res}\n");

            if (res.PspReference != "")
            {
                string redirectURL;
                // Conditionally handle different result codes for the shopper
                switch (res.ResultCode)
                {
                    case PaymentsResponse.ResultCodeEnum.Authorised:
                        redirectURL = "/Home/Result/success";
                        break;
                    case PaymentsResponse.ResultCodeEnum.Pending:
                    case PaymentsResponse.ResultCodeEnum.Received:
                        redirectURL = "/Home/Result/pending";
                        break;
                    case PaymentsResponse.ResultCodeEnum.Refused:
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


        private IPaymentMethodDetails parsePaymentMethodDetails(string pm, string type)
        {
            switch (type)
            {
                case "ideal":
                    return JsonConvert.DeserializeObject<IdealDetails>(pm);
                case "dotpay":
                    return JsonConvert.DeserializeObject<DotpayDetails>(pm);
                case "giropay":
                    return JsonConvert.DeserializeObject<GiropayDetails>(pm);
                case "ach":
                    return JsonConvert.DeserializeObject<AchDetails>(pm);
                default:
                    return JsonConvert.DeserializeObject<DefaultPaymentMethodDetails>(pm);
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

        public class RedirectReq
        {
            public string MD;
            public string PaRes;
            public string Payload;
        }

    }
}