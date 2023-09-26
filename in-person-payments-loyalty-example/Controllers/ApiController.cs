using Adyen.HttpClient;
using Adyen.Model.Nexo;
using Adyen.Service.Checkout;
using adyen_dotnet_in_person_payments_loyalty_example.Models;
using adyen_dotnet_in_person_payments_loyalty_example.Models.Requests;
using adyen_dotnet_in_person_payments_loyalty_example.Models.Responses;
using adyen_dotnet_in_person_payments_loyalty_example.Options;
using adyen_dotnet_in_person_payments_loyalty_example.Repositories;
using adyen_dotnet_in_person_payments_loyalty_example.Services;
using adyen_dotnet_in_person_payments_loyalty_example.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Checkout = Adyen.Model.Checkout;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IPosPaymentService _posPaymentService;
        private readonly IPosReversalService _posPaymentReversalService;
        private readonly IPosAbortService _posAbortService;
        private readonly ITableRepository _tableService;
        private readonly IPaymentsService _paymentsService;
        private readonly IUrlService _urlService;
        private readonly ICardAcquisitionRepository _repository;
        private readonly string _saleId;
        private readonly string _merchantAccount;
        private readonly string _poiId;

        public ApiController(ILogger<ApiController> logger, 
            IPosPaymentService posPaymentService, 
            IPosReversalService posPaymentReversalService, 
            IPosAbortService posAbortService,
            ITableRepository tableService, 
            IOptions<AdyenOptions> options,
            IPaymentsService paymentsService,
            IUrlService urlService,
            ICardAcquisitionRepository repository)
        {
            _logger = logger;
            _posPaymentService = posPaymentService;
            _posPaymentReversalService = posPaymentReversalService;
            _posAbortService = posAbortService;
            _tableService = tableService;
            _paymentsService = paymentsService;
            _urlService = urlService;
            _repository = repository;
            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }


        [HttpPost("api/sessions")]
        public async Task<ActionResult<Checkout.CreateCheckoutSessionResponse>> Sessions(CancellationToken cancellationToken = default)
        {
            long amount = 1999;
            var existingCustomer = _repository.CardAcquisitions.FirstOrDefault(x => x.ShopperEmail == Identifiers.ShopperEmail);


            if (existingCustomer != null)//json.AdditionalData.ShopperReference != null
            {
                if (existingCustomer.LoyaltyPoints >= 3000)
                {
                    amount = (long)(amount * 0.5M);
                }
                else if (existingCustomer.LoyaltyPoints >= 2000)
                {
                    amount = (long)(amount * 0.75M);
                }
            }

            var orderRef = Guid.NewGuid();
            var sessionsRequest = new Checkout.CreateCheckoutSessionRequest()
            {
                MerchantAccount = _merchantAccount,
                Reference = Identifiers.ShopperEmail,
                Channel = Checkout.CreateCheckoutSessionRequest.ChannelEnum.Web,
                Amount = new Checkout.Amount("EUR", amount),

                // Required for 3DS2 redirect flow.
                ReturnUrl = $"{_urlService.GetHostUrl()}/redirect?orderRef={orderRef}",
            };

            try
            {
                var res = await _paymentsService.SessionsAsync(sessionsRequest, cancellationToken: cancellationToken);
                _logger.LogInformation($"Response for Payments API:\n{res}\n");
                return res;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        [HttpPost("api/create-payment")]
        public async Task<ActionResult<CreatePaymentResponse>> CreatePayment([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken = default)
        {
            var table = _tableService.Tables.FirstOrDefault(t => t.TableName == request.TableName);

            if (table == null)
            {
                return NotFound(new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = $"{request.TableName} not found"
                });
            }

            string serviceId = IdUtility.GetRandomAlphanumericId(10);

            table.PaymentStatusDetails.ServiceId = serviceId;
            table.PaymentStatus = PaymentStatus.PaymentInProgress;

            try
            {
                SaleToPOIResponse response = await _posPaymentService.SendPaymentRequestAsync(serviceId, _poiId, _saleId, request.Currency, request.Amount, cancellationToken);

                PaymentResponse paymentResponse = response?.MessagePayload as PaymentResponse;
                if (response == null)
                {
                    table.PaymentStatus = PaymentStatus.NotPaid;
                    return BadRequest(new CreatePaymentResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Empty payment response"
                    });
                }

                switch (paymentResponse?.Response?.Result)
                {
                    case ResultType.Success:
                        table.PaymentStatus = PaymentStatus.Paid;
                        table.PaymentStatusDetails.PoiTransactionId = paymentResponse.POIData.POITransactionID.TransactionID;
                        table.PaymentStatusDetails.PoiTransactionTimeStamp = paymentResponse.POIData.POITransactionID.TimeStamp;
                        table.PaymentStatusDetails.SaleTransactionId = paymentResponse.SaleData.SaleTransactionID.TransactionID;
                        table.PaymentStatusDetails.SaleTransactionTimeStamp = paymentResponse.SaleData.SaleTransactionID.TimeStamp;

                        return Ok(new CreatePaymentResponse()
                        {
                            Result = "success"
                        });
                    case ResultType.Failure:
                        table.PaymentStatus = PaymentStatus.NotPaid;
                        table.PaymentStatusDetails.RefusalReason = "Payment terminal responded with: " + paymentResponse.Response.ErrorCondition;
                        table.PaymentStatusDetails.PoiTransactionId = paymentResponse.POIData.POITransactionID.TransactionID;
                        table.PaymentStatusDetails.PoiTransactionTimeStamp = paymentResponse.POIData.POITransactionID.TimeStamp;
                        table.PaymentStatusDetails.SaleTransactionId = paymentResponse.SaleData.SaleTransactionID.TransactionID;
                        table.PaymentStatusDetails.SaleTransactionTimeStamp = paymentResponse.SaleData.SaleTransactionID.TimeStamp;

                        return Ok(new CreatePaymentResponse()
                        {
                            Result = "failure",
                            RefusalReason = table.PaymentStatusDetails.RefusalReason
                        });
                    default:
                        table.PaymentStatus = PaymentStatus.NotPaid;

                        return BadRequest(new CreatePaymentResponse()
                        {
                            Result = "failure",
                            RefusalReason = _poiId == null
                                ? "Could not reach payment terminal - POI ID is not set"
                                : $"Could not reach payment terminal with POI ID {_poiId}"
                        });
                }

            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                table.PaymentStatus = PaymentStatus.NotPaid;

                return StatusCode(e.Code, new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = $"ErrorCode: {e.Code}, see logs"
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                table.PaymentStatus = PaymentStatus.NotPaid;
                throw;
            }
        }

        [HttpPost("api/create-reversal")]
        public async Task<ActionResult<CreateReversalResponse>> CreateReversal([FromBody] CreateReversalRequest request, CancellationToken cancellationToken = default)
        {
            TableModel table = _tableService.Tables.FirstOrDefault(t => t.TableName == request.TableName);

            if (table == null)
            {
                return NotFound(new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = $"Table {request.TableName} not found"
                });
            }
            
            try
            {
                SaleToPOIResponse response = await _posPaymentReversalService.SendReversalRequestAsync(ReversalReasonType.MerchantCancel, table.PaymentStatusDetails.SaleTransactionId, table.PaymentStatusDetails.PoiTransactionId, _poiId, _saleId, cancellationToken);

                ReversalResponse reversalResponse = response?.MessagePayload as ReversalResponse;
                if (reversalResponse == null)
                {
                    return BadRequest(new CreateReversalResponse()
                    {
                        Result = "failure",
                        RefusalReason = $"Empty reversal response"
                    });
                }

                switch (reversalResponse.Response.Result)
                {
                    case ResultType.Success:
                        table.PaymentStatus = PaymentStatus.RefundInProgress;
                        return Ok(new CreateReversalResponse()
                        {
                            Result = "success"
                        });
                    case ResultType.Failure:
                        table.PaymentStatus = PaymentStatus.RefundFailed;
                        return Ok(new CreateReversalResponse()
                        {
                            Result = "failure",
                            RefusalReason = "Payment terminal responded with: " + HttpUtility.UrlDecode(reversalResponse.Response.AdditionalResponse)
                        });
                    case ResultType.Partial:
                        throw new NotImplementedException(nameof(ResultType.Partial));
                    default:
                        return BadRequest(new CreateReversalResponse()
                        {
                            Result = "failure",
                            RefusalReason = _poiId == null 
                                ? "Could not reach payment terminal - POI ID is not set" 
                                : $"Could not reach payment terminal with POI ID {_poiId}"
                        });
                }
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(e.Code, new CreateReversalResponse()
                {
                    Result = "failure",
                    RefusalReason = $"ErrorCode: {e.Code}, see logs"
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }

        [HttpGet("api/abort/{tableName}")]
        public async Task<ActionResult<SaleToPOIResponse>> Abort(string tableName, CancellationToken cancellationToken = default)
        {
            try
            {
                TableModel table = _tableService.Tables.FirstOrDefault(t => t.TableName == tableName);

                if (table?.PaymentStatusDetails?.ServiceId == null)
                {
                    return NotFound();
                }

                SaleToPOIResponse abortResponse = await _posAbortService.SendAbortRequestAsync(MessageCategoryType.Payment, table.PaymentStatusDetails.ServiceId, _poiId, _saleId, cancellationToken);
                return Ok(abortResponse);
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(e.Code, new CreateReversalResponse()
                {
                    Result = "failure",
                    RefusalReason = $"ErrorCode: {e.Code}, see logs"
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }
    }
}