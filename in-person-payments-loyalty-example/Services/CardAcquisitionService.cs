using adyen_dotnet_in_person_payments_loyalty_example.Models;
using adyen_dotnet_in_person_payments_loyalty_example.Options;
using adyen_dotnet_in_person_payments_loyalty_example.Services.CardAcquisition;
using adyen_dotnet_in_person_payments_loyalty_example.Utilities;
using Adyen.HttpClient;
using Adyen.Model.Nexo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Services
{
    public interface ICardAcquisitionService
    {

    }

    public class CardAcquisitionService : ICardAcquisitionService
    {
        private readonly ILogger<CardAcquisitionService> _logger;
        private readonly IPosCardAcquisitionService _posCardAcquisitionService;

        private readonly string _saleId;
        private readonly string _poiId;

        public CardAcquisitionService(
            ILogger<CardAcquisitionService> logger,
            IPosCardAcquisitionService posCardAcquisitionService,
            IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _posCardAcquisitionService = posCardAcquisitionService;
            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;
        }
        
        public async Task<(CardAcquisitionRoot additionalData, String refusalReason)> SendCardAcquisitionResponse(string currency, decimal amount, CancellationToken cancellationToken = default)
        {
            try
            {
                string transactionId = Guid.NewGuid().ToString();
                SaleToPOIResponse response = await _posCardAcquisitionService.SendCardAcquisitionRequestAsync(
                    serviceId: IdUtility.GetRandomAlphanumericId(10),
                    poiId: _poiId,
                    saleId: _saleId,
                    transactionId: transactionId,
                    amount: 0,
                    cancellationToken: cancellationToken);

                CardAcquisitionResponse cardAcquisitionResponse = response?.MessagePayload as CardAcquisitionResponse;

                if (cardAcquisitionResponse == null)
                {
                    return (null, $"Cannot reach device with POI ID {_poiId}");
                }

                // Decode the base64 encoded string.
                string decodedUtf8JsonString = Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse.Response.AdditionalResponse));
                _logger.LogInformation(decodedUtf8JsonString);
                
                if (cardAcquisitionResponse.Response.Result != ResultType.Success)
                {
                    return (null, decodedUtf8JsonString);
                }

                CardAcquisitionRoot json = JsonConvert.DeserializeObject<CardAcquisitionRoot>(decodedUtf8JsonString);

                if (json == null)
                {
                    // Can't deserialize json into an object.
                    return (null, "Unable to deserialize json");
                }

                // This is a gift card, handle gift card logic accordingly. For now, do not support card acquisition for gift cards.
                if (json.AdditionalData.GiftcardIndicator)
                {
                    return (null, "Card provided is a gift card");
                }
                
                return (json, null);
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return (null,  $"ErrorCode: {e.Code}, see logs");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }
    }
}