using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace adyen_dotnet_checkout_example_advanced.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _clientKey;
        public HomeController(IConfiguration configuration)
        {
            _clientKey = configuration["ADYEN_CLIENT_KEY"];
        }

        [Route("/")]
        public IActionResult Index()
        {
            return View("1_Index");
        }

        [Route("preview/{id}")]
        public IActionResult Preview(string id)
        {
            ViewBag.PaymentMethod = id;
            return View("2_Preview");
        }

        [Route("checkout/{id}")]
        public IActionResult Checkout(string id)
        {
            ViewBag.PaymentMethod = id;
            ViewBag.ClientKey = _clientKey;
            return View("3_Checkout");
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

            return View("4_Result");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}
