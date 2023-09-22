using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_example.Models;
using adyen_dotnet_in_person_payments_example.Options;
using adyen_dotnet_in_person_payments_example.Repositories;
using adyen_dotnet_in_person_payments_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_example.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _poiId;
        private readonly string _saleId;
        private readonly ILogger<HomeController> _logger;
        private readonly ITableRepository _tableService;
        private readonly IPosTransactionStatusService _posTransactionStatusService;

        public HomeController(ILogger<HomeController> logger, IOptions<AdyenOptions> optionsAccessor, ITableRepository tableService, IPosTransactionStatusService posTransactionStatusService)
        {
            _poiId = optionsAccessor.Value.ADYEN_POS_POI_ID;
            _saleId = optionsAccessor.Value.ADYEN_POS_SALE_ID;
            _logger = logger;
            _tableService = tableService;
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
            ViewBag.Tables = _tableService.Tables;
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
                    msg = $"Your request has been successfully processed.";
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
