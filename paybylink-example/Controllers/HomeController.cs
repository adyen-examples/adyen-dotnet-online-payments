using adyen_dotnet_paybylink_example.Models;
using adyen_dotnet_paybylink_example.Options;
using adyen_dotnet_paybylink_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace adyen_dotnet_paybylink_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPaymentLinkService _paymentLinkService;

        public HomeController(IPaymentLinkService paymentLinkService, IOptions<AdyenOptions> options)
        {
            _paymentLinkService = paymentLinkService;
        }

        [Route("/")]
        public IActionResult Index()
        {
            ConcurrentDictionary<string, PaymentLinkModel> paymentLinks = _paymentLinkService.GetPaymentLinks();
            List<PaymentLinkModel> models = paymentLinks.Select(kvp => kvp.Value).ToList();
            return View(models);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}
