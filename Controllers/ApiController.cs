using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Adyen;
using Adyen.Model.Checkout;
using Adyen.Service;
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
        private Dictionary<string, string> _paymentDataStore;

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
        public ActionResult<string> InitiatePayment([FromBody] PaymentRequest req)
        {
            _logger.LogInformation($"Request for Payments API::\n{req}\n");

            var pmType = req.PaymentMethod.Type;
            var amount = new Amount(findCurrency(pmType), 1000);
            req.MerchantAccount = _merchant_account;
            req.Amount = amount;
            var orderRef = System.Guid.NewGuid();
            req.Reference = orderRef.ToString();
            req.Channel = PaymentRequest.ChannelEnum.Web;
            // req.AdditionalData = new Dictionary<string, string>();
            req.AdditionalData.Add("allow3DS2", "true");
            req.ReturnUrl = $"http://localhost:3000/api/handleShopperRedirect?orderRef={orderRef}";
            // Required for Klarna:
            if (pmType.Contains("klarna"))
            {
                req.CountryCode = "DE";
                req.ShopperReference = "12345";
                req.ShopperEmail = "youremail@email.com";
                req.ShopperLocale = "en_US";
                req.LineItems.Add(new LineItem(
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
                ));
                req.LineItems.Add(new LineItem(
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
                ));
            }

            try
            {
                var res = _checkout.Payments(req);

                if (res.Action != null && res.Action.PaymentData != "")
                {
                    _logger.LogInformation($"Setting payment data cache for {orderRef}\n");
                    _paymentDataStore[orderRef.ToString()] = res.Action.PaymentData;
                    return res.ToJson();
                }
                else
                {
                    var dict = new Dictionary<string, string>();
                    dict.Add("pspReference", res.PspReference);
                    dict.Add("resultCode", res.ResultCode.ToString());
                    dict.Add("refusalReason", res.RefusalReason);
                    return JsonConvert.SerializeObject(dict);
                }
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed::\n{e}\n");
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
                    var dict = new Dictionary<string, string>();
                    dict.Add("pspReference", res.PspReference);
                    dict.Add("resultCode", res.ResultCode.ToString());
                    dict.Add("refusalReason", res.RefusalReason);
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
        [HttpPost("api/handleShopperRedirect")]
        public void RedirectAction([FromQuery(Name = "orderRef")] string orderRef, [FromBody] Redirect redirect)
        {
            _logger.LogInformation($"Redirect request received\nRef: {orderRef}");
            var paymentData = _paymentDataStore[orderRef];

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
                        redirectURL = "/result/success";
                        break;
                    case PaymentsResponse.ResultCodeEnum.Pending:
                    case PaymentsResponse.ResultCodeEnum.Received:
                        redirectURL = "/result/pending";
                        break;
                    case PaymentsResponse.ResultCodeEnum.Refused:
                        redirectURL = "/result/failed";
                        break;
                    default:
                        {
                            var reason = res.RefusalReason;
                            if (reason == "")
                            {
                                reason = res.ResultCode.ToString();

                            }
                            redirectURL = $"/result/error?reason={WebUtility.UrlEncode(reason)}";
                            break;
                        }
                }
                // now redirect
                Response.Redirect(redirectURL);
            }
        }

        public class Redirect
        {
            public string MD;
            public string PaRes;
            public string Payload;
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