using adyen_dotnet_giving_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace adyen_dotnet_giving_example.Controllers
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

        [Route("preview/{id}")]
        public IActionResult Preview(string id)
        {
            ViewBag.PaymentMethod = id;
            return View();
        }

        [Route("checkout/{id}")]
        public IActionResult Checkout(string id)
        {
            ViewBag.PaymentMethod = id;
            ViewBag.ClientKey = _clientKey;
            return View();
        }

        [HttpGet("result/{status}")]
        public IActionResult Result(string status, [FromQuery(Name = "reason")] string refusalReason)
        {
            ViewBag.ClientKey = _clientKey;
            string msg;
            string img;
            switch (status)
            {
                case "donated":
                    msg = "Thank you for your donation!";
                    img = "success";
                    break;
                case "success":
                    msg = "Your order has been successfully placed.";
                    img = "success";
                    break;
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
                    msg = $"Unhandled {status} status-response";
                    img = "error";
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
