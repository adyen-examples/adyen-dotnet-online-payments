using adyen_dotnet_in_person_payments_loyalty_example.Options;
using adyen_dotnet_in_person_payments_loyalty_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _poiId;
        private readonly string _saleId;
        private readonly IPizzaRepository _pizzaRepository;
        private readonly IShopperRepository _shopperRepository;

        public HomeController(IOptions<AdyenOptions> optionsAccessor, IPizzaRepository pizzaRepository, IShopperRepository shopperRepository)
        {
            _poiId = optionsAccessor.Value.ADYEN_POS_POI_ID;
            _saleId = optionsAccessor.Value.ADYEN_POS_SALE_ID;
            _pizzaRepository = pizzaRepository;
            _shopperRepository = shopperRepository;
        }

        [Route("/")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("cashregister")]
        public IActionResult CashRegister()
        {
            ViewBag.PoiId = _poiId;
            ViewBag.SaleId = _saleId;
            ViewBag.Pizzas = _pizzaRepository.Pizzas;
            return View();
        }

        [HttpGet("result/{status}/{refusalReason?}")]
        public IActionResult Result(string status, string refusalReason = null)
        {
            _pizzaRepository.ClearDiscount();
            string msg;
            string img;
            switch (status)
            {
                case "failure":
                    msg = $"{refusalReason}.";
                    img = "failed";
                    break;
                case "success":
                    msg = $"Payment successful! We're putting your pizza in the oven.";
                    img = "success";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(status);
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

        [Route("shoppers")]
        public IActionResult Shoppers()
        {
            ViewBag.Shoppers = _shopperRepository.Shoppers.Select(kvp => kvp.Value).ToList();
            return View();
        }

    }
}
