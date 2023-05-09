using adyen_dotnet_paybylink_example.Models;
using adyen_dotnet_paybylink_example.Options;
using adyen_dotnet_paybylink_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace adyen_dotnet_paybylink_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _clientKey;
        private readonly IPaymentLinkService _paymentLinkService;

        public HomeController(IPaymentLinkService paymentLinkService, IOptions<AdyenOptions> options)
        {
            _clientKey = options.Value.ADYEN_CLIENT_KEY;
            _paymentLinkService = paymentLinkService;
        }
 
        [Route("/")]
        public IActionResult Index()
        {
            ConcurrentDictionary<string, PaymentLinkModel> paymentLinks = _paymentLinkService.GetPaymentLinks();
            List<PaymentLinkModel> models = paymentLinks.Select(kvp => kvp.Value).ToList();
            return View(models);
        }

        [HttpGet("result/{status}")]
        public IActionResult Result(string status, [FromQuery(Name = "reason")] string refusalReason)
        {
            string msg;
            string img;
            switch (status)
            {
                case "pending":
                    msg = "Your order has been received! Payment completion pending.";
                    img = "success";
                    break;
                case "failed":
                    msg = "The payment was refused. Please try a different payment method or card.";
                    img = "failed";
                    break;
                case "error":
                    msg = $"Error! Reason: {refusalReason}";
                    img = "failed";
                    break;
                default:
                    msg = "Your order has been successfully placed.";
                    img = "success";
                    break;
            }
            ViewBag.Status = status;
            ViewBag.Msg = msg;
            ViewBag.Img = img;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}
