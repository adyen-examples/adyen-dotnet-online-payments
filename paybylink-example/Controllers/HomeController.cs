using adyen_dotnet_paybylink_example.Models;
using adyen_dotnet_paybylink_example.Options;
using adyen_dotnet_paybylink_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace adyen_dotnet_paybylink_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILinksService _linksService;

        public HomeController(ILinksService linksService)
        {
            _linksService = linksService;
        }

        [Route("/")]
        public async Task<IActionResult> Index()
        {
            ConcurrentDictionary<string, PaymentLinkModel> paymentLinks = await _linksService.GetAndUpdatePaymentLinksAsync();
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
