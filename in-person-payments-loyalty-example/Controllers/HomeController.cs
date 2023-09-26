using Adyen.Model.Nexo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using adyen_dotnet_in_person_payments_loyalty_example.Models;
using adyen_dotnet_in_person_payments_loyalty_example.Options;
using adyen_dotnet_in_person_payments_loyalty_example.Repositories;
using adyen_dotnet_in_person_payments_loyalty_example.Services;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _poiId;
        private readonly string _saleId;
        private readonly string _clientKey;
        private readonly ILogger<HomeController> _logger;
        private readonly ITableRepository _tableService;
        private readonly IPosTransactionStatusService _posTransactionStatusService;
        private readonly ICardAcquisitionRepository _repository;

        public HomeController(ILogger<HomeController> logger, IOptions<AdyenOptions> optionsAccessor, ITableRepository tableService, IPosTransactionStatusService posTransactionStatusService, ICardAcquisitionRepository repository)
        {
            _poiId = optionsAccessor.Value.ADYEN_POS_POI_ID;
            _saleId = optionsAccessor.Value.ADYEN_POS_SALE_ID;
            _clientKey = optionsAccessor.Value.ADYEN_CLIENT_KEY;
            _logger = logger;
            _tableService = tableService;
            _posTransactionStatusService = posTransactionStatusService;
            _repository = repository;
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
            ViewBag.Tables = _tableService.Tables;
            return View();
        }

        [HttpGet("result/{status}/{refusalReason?}")]
        public IActionResult Result(string status, string refusalReason = null)
        {
            _tableService.Reset();
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

        [Route("redirect")]
        public IActionResult Redirect()
        {
            ViewBag.ClientKey = _clientKey;
            return View();
        }

        [HttpGet("resultview/{status}")]
        public IActionResult ResultView(string status, [FromQuery(Name = "reason")] string refusalReason)
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
                    var existingCustomer = _repository.CardAcquisitions.FirstOrDefault(x => x.ShopperEmail == Identifiers.ShopperEmail);

                    if (existingCustomer != null)
                    {
                        existingCustomer.LoyaltyPoints += 1000;
                    }

                    msg = "Your pizza order has been successfully placed.";
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

        [HttpGet("transaction-status/{tableName}")]
        public async Task<IActionResult> TransactionStatus(string tableName, CancellationToken cancellationToken = default)
        {
            TableModel table = _tableService.Tables.FirstOrDefault(t => t.TableName == tableName);

            ViewBag.TableName = tableName;
            
            if (table?.PaymentStatusDetails?.ServiceId == null)
            {
                ViewBag.ErrorMessage = $"Could not find any transactions for {tableName}.";
                return View();
            }

            try
            {
                SaleToPOIResponse response = await _posTransactionStatusService.SendTransactionStatusRequestAsync(table.PaymentStatusDetails.ServiceId, _poiId, _saleId, cancellationToken);
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

                ViewBag.ServiceId = table.PaymentStatusDetails.ServiceId;

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
