using adyen_dotnet_in_person_payments_loyalty_example.Options;
using adyen_dotnet_in_person_payments_loyalty_example.Repositories;
using adyen_dotnet_in_person_payments_loyalty_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using adyen_dotnet_in_person_payments_loyalty_example.Services.CardAcquisition;
using Adyen.Model.TerminalApi;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _poiId;
        private readonly string _saleId;
        private readonly IPizzaRepository _pizzaRepository;
        private readonly IShopperRepository _shopperRepository;
        private readonly IPosTransactionStatusService _posTransactionStatusService;

        public HomeController(IOptions<AdyenOptions> optionsAccessor, IPizzaRepository pizzaRepository, IShopperRepository shopperRepository, IPosTransactionStatusService posTransactionStatusService)
        {
            _poiId = optionsAccessor.Value.ADYEN_POS_POI_ID;
            _saleId = optionsAccessor.Value.ADYEN_POS_SALE_ID;
            _pizzaRepository = pizzaRepository;
            _shopperRepository = shopperRepository;
            _posTransactionStatusService = posTransactionStatusService;
        }

        [Route("/")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("cash-register")]
        public IActionResult CashRegister()
        {
            ViewBag.PoiId = _poiId;
            ViewBag.SaleId = _saleId;
            ViewBag.Pizzas = _pizzaRepository.Pizzas;
            return View();
        }
        
        [Route("signuponly")]
        public IActionResult SignupOnly()
        {
            ViewBag.PoiId = _poiId;
            ViewBag.SaleId = _saleId;
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
                    msg = refusalReason;
                    img = "failed";
                    break;
                case "success":
                    msg = $"Success!";
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

        [Route("adminpanel")]
        public IActionResult AdminPanel()
        {
            ViewBag.Shoppers = _shopperRepository.Shoppers.Select(kvp => kvp.Value).ToList();
            return View();
        }
        
        [Route("transactionstatus/{serviceId}")]
        public async Task<IActionResult> TransactionStatus(string serviceId, CancellationToken cancellationToken = default)
        {
            var saleResponse = await _posTransactionStatusService.SendTransactionStatusRequestAsync(
                serviceId: serviceId, 
                poiId: _poiId, 
                saleId: _saleId,
                cancellationToken: cancellationToken);
            
            TransactionStatusResponse transactionStatusResponse = saleResponse?.MessagePayload as TransactionStatusResponse;

            if (transactionStatusResponse == null)
            {
                ViewBag.ErrorMessage = $"Transaction status response is null for {@serviceId}";
                return View();
            }
            
            if (transactionStatusResponse.Response.Result != ResultType.Success)
            {
                ViewBag.ErrorMessage = HttpUtility.UrlDecode(transactionStatusResponse.Response.AdditionalResponse);
                return View();
            }
            
            PaymentResponse paymentResponse = transactionStatusResponse?.RepeatedMessageResponse?.RepeatedResponseMessageBody?.MessagePayload as PaymentResponse;

            if (paymentResponse == null)
            {
                ViewBag.ErrorMessage = "PaymentResponse response is null.";
                return View();
            }
            
            if (paymentResponse.Response.Result != ResultType.Success)
            {
                ViewBag.ErrorMessage = HttpUtility.UrlDecode(paymentResponse.Response.AdditionalResponse);
                return View();
            }
            
            ViewBag.PaymentResponse = paymentResponse;
            ViewBag.ServiceId = serviceId;

            return View();
        }

    }
}
