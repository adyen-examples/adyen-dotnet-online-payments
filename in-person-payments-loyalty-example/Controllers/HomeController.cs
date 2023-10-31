using adyen_dotnet_in_person_payments_loyalty_example.Models;
using adyen_dotnet_in_person_payments_loyalty_example.Options;
using adyen_dotnet_in_person_payments_loyalty_example.Repositories;
using adyen_dotnet_in_person_payments_loyalty_example.Services;
using Adyen.Model.Nexo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _poiId;
        private readonly string _saleId;
        private readonly ILogger<HomeController> _logger;
        private readonly IPizzaRepository _pizzaRepository;
        private readonly IPosTransactionStatusService _posTransactionStatusService;

        public HomeController(ILogger<HomeController> logger, IOptions<AdyenOptions> optionsAccessor, IPizzaRepository pizzaRepository, IPosTransactionStatusService posTransactionStatusService, ICardAcquisitionRepository repository)
        {
            _poiId = optionsAccessor.Value.ADYEN_POS_POI_ID;
            _saleId = optionsAccessor.Value.ADYEN_POS_SALE_ID;
            _logger = logger;
            _pizzaRepository = pizzaRepository;
            _posTransactionStatusService = posTransactionStatusService;
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

        [HttpGet("transaction-status/{pizzaName}")]
        public async Task<IActionResult> TransactionStatus(string pizzaName, CancellationToken cancellationToken = default)
        {
            PizzaModel pizza = _pizzaRepository.Pizzas.FirstOrDefault(t => t.PizzaName == pizzaName);

            ViewBag.PizzaName = pizzaName;
            
            if (pizza?.PaymentStatusDetails?.ServiceId == null)
            {
                ViewBag.ErrorMessage = $"Could not find any transactions for {pizzaName}.";
                return View();
            }

            try
            {
                SaleToPOIResponse response = await _posTransactionStatusService.SendTransactionStatusRequestAsync(pizza.PaymentStatusDetails.ServiceId, _poiId, _saleId, cancellationToken);
                TransactionStatusResponse transactionStatusResponse = response.MessagePayload as TransactionStatusResponse;

                if (transactionStatusResponse == null)
                {
                    ViewBag.ErrorMessage = "Could not parse the transaction status response.";
                    return View();
                }
                
                
                PaymentResponse paymentResponse = transactionStatusResponse.RepeatedMessageResponse.RepeatedResponseMessageBody.MessagePayload as PaymentResponse;
                if (paymentResponse != null)
                {
                    ViewBag.PaymentResponse = paymentResponse;
                }

                ViewBag.ServiceId = pizza.PaymentStatusDetails.ServiceId;

                return View();
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                ViewBag.ErrorMessage = e.ToString();
                return View();
            }
        }
    }
}
