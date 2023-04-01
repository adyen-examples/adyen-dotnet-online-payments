﻿using adyen_dotnet_giftcard_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace adyen_dotnet_giftcard_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _clientKey;

        public HomeController(IOptions<AdyenOptions> options)
        {
            _clientKey = options.Value.ADYEN_CLIENT_KEY;
        }
 
        [Route("/")]
        public IActionResult Index()
        {
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

        #region ### Drop-in

        [Route("dropin/checkout/{id}")]
        public IActionResult DropinCheckout(string id)
        {
            ViewBag.PaymentMethod = id;
            ViewBag.ClientKey = _clientKey;
            return View("~/Views/Home/Dropin/Checkout.cshtml");
        }

        [Route("dropin/preview/{id}")]
        public IActionResult DropinPreview(string id)
        {
            ViewBag.PaymentMethod = id;
            return View("~/Views/Home/Dropin/Preview.cshtml");
        }

        [Route("dropin/redirect")]
        public IActionResult DropinRedirect()
        {
            ViewBag.ClientKey = _clientKey;
            return View("~/Views/Home/Dropin/Redirect.cshtml");
        }

        #endregion


        #region ### GiftcardComponent

        [Route("giftcardcomponent/checkout")]
        public IActionResult GiftcardComponentCheckout()
        {
            ViewBag.ClientKey = _clientKey;
            return View("~/Views/Home/GiftcardComponent/Checkout.cshtml");
        }

        [Route("giftcardcomponent/preview")]
        public IActionResult GiftcardComponentPreview()
        {
            return View("~/Views/Home/GiftcardComponent/Preview.cshtml");
        }

        [Route("giftcardcomponent/redirect")]
        public IActionResult GiftcardComponentRedirect()
        {
            ViewBag.ClientKey = _clientKey;
            return View("~/Views/Home/GiftcardComponent/Redirect.cshtml");
        }

        #endregion
    }
}
