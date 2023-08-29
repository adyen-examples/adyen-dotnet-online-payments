﻿using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using adyen_dotnet_in_person_payments_example.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_example.Services
{
    public interface IPosPaymentTransactionStatusService
    {
        /// <summary>
        /// Sends a terminal-api transaction status request
        /// </summary>
        /// <param name="serviceId">The unique ID of a message pair, which processes a transaction. The length of the string must be greater than or equal to 1 and less than or equal to 10.</param>
        /// <param name="poiId">The unique ID of the terminal to send this request to. Format: [device model]-[serial number].</param>
        /// <param name="saleId">Your unique ID for the POS system (cash register) to send this request from.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns><see cref="SaleToPOIResponse"/>.</returns>
        Task<SaleToPOIResponse> SendTransactionStatusRequestAsync(string serviceId, string poiId, string saleId, CancellationToken cancellationToken = default);
    }

    public class PosTransactionStatusService : IPosPaymentTransactionStatusService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosTransactionStatusService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendTransactionStatusRequestAsync(string serviceId, string poiId, string saleId, CancellationToken cancellationToken)
        {
            SaleToPOIRequest request = GetTransactionStatusRequest(serviceId, poiId, saleId);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        private SaleToPOIRequest GetTransactionStatusRequest(string serviceId, string poiId, string saleId)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.TransactionStatus,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = IdUtility.GetRandomAlphanumericId(10), 
                },
                MessagePayload = new TransactionStatusRequest()
                {
                    MessageReference = new MessageReference()
                    {
                        MessageCategory = MessageCategoryType.Abort,
                        ServiceID = serviceId,
                        POIID = poiId,
                        SaleID = saleId
                    }
                }
            };

            return request;
        }
    }
}