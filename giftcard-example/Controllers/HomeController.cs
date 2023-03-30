using adyen_dotnet_giftcard_example.Options;
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

        [Route("previewdropin/{id}")]
        public IActionResult PreviewDropin(string id)
        {
            ViewBag.PaymentMethod = id;
            return View();
        }

        [Route("checkoutdropin/{id}")]
        public IActionResult CheckoutDropin(string id)
        {
            ViewBag.PaymentMethod = id;
            ViewBag.ClientKey = _clientKey;
            return View();
        }

        [Route("previewgiftcardcomponent/{id}")]
        public IActionResult PreviewGiftcardComponent(string id)
        {
            ViewBag.PaymentMethod = id;
            return View();
        }

        [Route("checkoutgiftcardcomponent/{id}")]
        public IActionResult CheckoutGiftcardComponent(string id)
        {
            ViewBag.PaymentMethod = id;
            ViewBag.ClientKey = _clientKey;
            return View();
        }

        [Route("redirectfromdropin")]
        public IActionResult RedirectFromDropin()
        {
            ViewBag.ClientKey = _clientKey;
            return View();
        }

        [Route("redirectfromgiftcardcomponent")]
        public IActionResult RedirectFromGiftcardComponent()
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
