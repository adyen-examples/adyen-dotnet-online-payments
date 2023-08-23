using adyen_dotnet_in_person_payments_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;

namespace adyen_dotnet_in_person_payments_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _poiId;
        private readonly string _saleId;

        public HomeController(IOptions<AdyenOptions> optionsAccessor)
        {
            _poiId = optionsAccessor.Value.ADYEN_POS_POI_ID;
            _saleId = optionsAccessor.Value.ADYEN_POS_SALE_ID;
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
            return View();
        }

        [HttpGet("result/{status}/{refusalReason?}")]
        public IActionResult Result(string status, string refusalReason = null)
        {
            string msg;
            string img;
            switch (status)
            {
                case "failure":
                    msg = $"Failed to initialize transaction. {refusalReason}.";
                    img = "failed";
                    break;
                case "success":
                    msg = $"Your transaction has been successfully processed.";
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
    }
}
