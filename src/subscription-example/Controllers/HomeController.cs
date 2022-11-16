﻿using adyen_dotnet_subscription_example.Options;
using adyen_dotnet_subscription_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace adyen_dotnet_subscription_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _clientKey;
        private readonly IRecurringClient _subscriptionService;

        public HomeController(IOptions<AdyenOptions> options, IRecurringClient subscriptionService)
        {
            _clientKey = options.Value.ADYEN_CLIENT_KEY;
            _subscriptionService = subscriptionService;
        }

        [Route("/")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("preview/{id}")]
        public IActionResult Preview(string id)
        {
            ViewBag.PaymentMethod = id;
            return View();
        }
        
        [Route("subscription/{id}")]
        public IActionResult Subscription(string id)
        {
            ViewBag.PaymentMethod = id;
            ViewBag.ClientKey = _clientKey;
            return View();
        }


        [Route("managetokens")]
        public IActionResult ManageTokens()
        {
            // TODO;
            return View();
        }

        [Route("redirect")]
        public IActionResult Redirect()
        {
            ViewBag.ClientKey = _clientKey;
            return View();
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
