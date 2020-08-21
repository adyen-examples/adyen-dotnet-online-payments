using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace adyen_dotnet_online_payments.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string _client_key;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _client_key = Environment.GetEnvironmentVariable("ADYEN_CLIENT_KEY");
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Preview(string id)
        {
            ViewBag.PaymentMethod = id;
            return View();
        }

        public IActionResult Checkout(string id)
        {
            ViewBag.PaymentMethod = id;
            ViewBag.ClientKey = _client_key;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
