﻿using System;
using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using adyen_dotnet_in_person_payments_example.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_example.Services
{
    public interface IPosReversalService
    {
        /// <summary>
        /// Issue a referenced refund by sending a terminal-api payment reversal request.
        /// https://docs.adyen.com/point-of-sale/basic-tapi-integration/refund-payment/referenced/?tab=full_refund_1#referenced-request
        /// </summary>
        /// <param name="reversalReasonType"><see cref="ReversalReasonType"/>.</param>
        /// <param name="saleReferenceId">Unique Id of a sale global transaction. Appears as MerchantReference in your Customer Area.</param>
        /// <param name="poiTransactionId">Unique Id of a POI transaction.</param>
        /// <param name="poiId">The unique ID of the terminal to send this request to. Format: [device model]-[serial number].</param>
        /// <param name="saleId">Your unique ID for the POS system (cash register) to send this request from.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendReversalRequestAsync(ReversalReasonType reversalReasonType, string saleReferenceId, string poiTransactionId, string poiId, string saleId, CancellationToken cancellationToken = default);
    }

    public class PosReversalService : IPosReversalService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosReversalService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendReversalRequestAsync(ReversalReasonType reversalReasonType, string saleReferenceId, string poiTransactionId, string poiId, string saleId, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = GetReversalRequest(reversalReasonType, saleReferenceId, poiTransactionId, poiId, saleId);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        private SaleToPOIRequest GetReversalRequest(ReversalReasonType reversalReasonType, string saleReferenceId, string poiTransactionId, string poiId, string saleId)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.Reversal,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,

                    // The unique ID of a message pair, which processes the transaction.
                    // Your unique ID for this request, consisting of 1-10 alphanumeric characters.
                    // Must be unique within the last 48 hours for the terminal (POIID) being used.
                    ServiceID = IdUtility.GetRandomAlphanumericId(10), /// Used in <see cref="IPosTransactionStatusService"/> and <see cref="IPosAbortService"/> and <see cref="IPosReversalService"/>
                },
                MessagePayload = new ReversalRequest()
                {
                    OriginalPOITransaction = new OriginalPOITransaction()
                    {
                        SaleID = saleId,
                        POIID = poiId,
                        POITransactionID = new TransactionIdentification()
                        {
                            TimeStamp = DateTime.UtcNow, 
                            TransactionID = poiTransactionId
                        } 
                    },
                    ReversalReason = reversalReasonType,
                    SaleReferenceID = saleReferenceId
                }
            };

            return request;
        }
    }
}