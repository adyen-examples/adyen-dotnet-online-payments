using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_example.Models;
using adyen_dotnet_in_person_payments_example.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_example.Services
{
    public interface ITableService
    {
        /// <summary>
        /// List of all <see cref="TableModel"/>s.
        /// </summary>
        List<TableModel> Tables { get; }

        /// <summary>
        /// Updates payment status and values of the given <paramref name="table"/> with the terminal-api response <see cref="PaymentResponse"/>.
        /// </summary>
        /// <param name="table"><see cref="TableModel"/>.</param>
        /// <param name="paymentResponse"><see cref="PaymentResponse"/>.</param>
        /// <returns><see cref="TableModel"/>.</returns>
        TableModel UpdatePaymentStatus(TableModel table, PaymentResponse paymentResponse);

        /// <summary>
        /// Uses the transaction-status endpoint to check for payments that are still in progress and updates them if they are aborted/cancelled or approved.
        /// See: https://docs.adyen.com/point-of-sale/basic-tapi-integration/verify-transaction-status/#success.
        /// </summary>
        Task UpdateInProgressTransactionsAsync(CancellationToken cancellationToken = default); // TODO: Bind this to a button
    }

    public class TableService : ITableService
    {
        public List<TableModel> Tables { get; }

        private readonly ILogger<TableService> _logger;
        private readonly IPosTransactionStatusService _posTransactionStatusService;
        private readonly string _poiId;
        private readonly string _saleId;

        public TableService(ILogger<TableService> logger, IOptions<AdyenOptions> options, IPosTransactionStatusService posTransactionStatusService)
        {
            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;

            Tables = new List<TableModel>();

            // Add tables.
            for (int i = 0; i < 12; i++)
            {
                int tableNumber = i + 1;
                Tables.Add(new TableModel()
                {
                    TableName = "Table " + tableNumber,
                    Amount = 11.11M * tableNumber,
                    Currency = "EUR",
                    PaymentStatus = PaymentStatus.NotPaid
                });
            }
            _logger = logger;
            _posTransactionStatusService = posTransactionStatusService;
        }

        public async Task UpdateInProgressTransactionsAsync(CancellationToken cancellationToken)
        {
            List<TableModel> tables = Tables.Where(t => t.PaymentStatus == PaymentStatus.PaymentInProgress && t.PaymentStatusDetails.ServiceId != null).ToList();

            foreach (TableModel table in tables)
            {
                try
                {
                    SaleToPOIResponse response = await _posTransactionStatusService.SendTransactionStatusRequestAsync(table.PaymentStatusDetails.ServiceId, _poiId, _saleId, cancellationToken);

                    TransactionStatusResponse transactionStatusResponse = response?.MessagePayload as TransactionStatusResponse;

                    /// In this case we update the payment status. Handle your business logic accordingly here.
                    /// You probably won't need most of them, but we've listed all cases below.
                    /// See: https://docs.adyen.com/point-of-sale/error-scenarios/#error-conditions.
                    switch (transactionStatusResponse?.Response?.ErrorCondition)
                    {
                        case ErrorConditionType.Aborted:
                            table.PaymentStatus = PaymentStatus.NotPaid;
                            break;
                        case ErrorConditionType.Busy:
                            break;
                        case ErrorConditionType.Cancel:
                            table.PaymentStatus = PaymentStatus.NotPaid;
                            break;
                        case ErrorConditionType.DeviceOut:
                            break;
                        case ErrorConditionType.InsertedCard:
                            break;
                        case ErrorConditionType.InProgress:
                            break;
                        case ErrorConditionType.LoggedOut:
                            break;
                        case ErrorConditionType.MessageFormat:
                            break;
                        case ErrorConditionType.NotAllowed:
                            break;
                        case ErrorConditionType.NotFound:
                            break;
                        case ErrorConditionType.PaymentRestriction:
                            break;
                        case ErrorConditionType.Refusal:
                            table.PaymentStatus = PaymentStatus.NotPaid;
                            break;
                        case ErrorConditionType.UnavailableDevice:
                            break;
                        case ErrorConditionType.UnavailableService:
                            break;
                        case ErrorConditionType.InvalidCard:
                            break;
                        case ErrorConditionType.UnreachableHost:
                            break;
                        case ErrorConditionType.WrongPIN:
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                }
            }
        }

        public TableModel UpdatePaymentStatus(TableModel table, PaymentResponse paymentResponse)
        {
            switch (paymentResponse?.Response?.Result)
            {
                case ResultType.Success:
                    table.PaymentStatus = PaymentStatus.Paid;
                    table.PaymentStatusDetails.PoiTransactionId = paymentResponse.POIData.POITransactionID.TransactionID;
                    table.PaymentStatusDetails.PoiTransactionTimeStamp = paymentResponse.POIData.POITransactionID.TimeStamp;
                    table.PaymentStatusDetails.SaleTransactionId = paymentResponse.SaleData.SaleTransactionID.TransactionID;
                    table.PaymentStatusDetails.SaleTransactionTimeStamp = paymentResponse.SaleData.SaleTransactionID.TimeStamp;
                    return table;
                case ResultType.Failure:
                    table.PaymentStatus = PaymentStatus.NotPaid;
                    table.PaymentStatusDetails.RefusalReason = "Payment terminal responded with: " + paymentResponse.Response.ErrorCondition;
                    table.PaymentStatusDetails.PoiTransactionId = paymentResponse.POIData.POITransactionID.TransactionID;
                    table.PaymentStatusDetails.PoiTransactionTimeStamp = paymentResponse.POIData.POITransactionID.TimeStamp;
                    table.PaymentStatusDetails.SaleTransactionId = paymentResponse.SaleData.SaleTransactionID.TransactionID;
                    table.PaymentStatusDetails.SaleTransactionTimeStamp = paymentResponse.SaleData.SaleTransactionID.TimeStamp;
                    return table;
                default:
                    return null;
            }
        }
    }
}
