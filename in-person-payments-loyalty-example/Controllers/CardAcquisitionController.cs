using Adyen.HttpClient;
using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_loyalty_example.Models;
using adyen_dotnet_in_person_payments_loyalty_example.Models.Responses;
using adyen_dotnet_in_person_payments_loyalty_example.Options;
using adyen_dotnet_in_person_payments_loyalty_example.Repositories;
using adyen_dotnet_in_person_payments_loyalty_example.Services;
using adyen_dotnet_in_person_payments_loyalty_example.Services.CardAcquisition;
using adyen_dotnet_in_person_payments_loyalty_example.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    [ApiController]
    public class CardAcquisitionController : ControllerBase
    {
        private readonly ILogger<CardAcquisitionController> _logger;
        private readonly IPosPaymentService _posPaymentService;
        private readonly IPosReversalService _posPaymentReversalService;
        private readonly IPosAbortService _posAbortService;
        private readonly IPosCardAcquisitionService _posCardAcquisitionService;
        private readonly IPosCardAcquisitionPaymentService _posCardAcquisitionPaymentService;
        private readonly IPosCardAcquisitionAbortService _posCardAcquisitionAbortService;
        private readonly ITableRepository _tableService;
        private readonly ICardAcquisitionRepository _cardAcquisitionRepository;

        private readonly string _saleId;
        private readonly string _poiId;

        public CardAcquisitionController(ILogger<CardAcquisitionController> logger,
            IPosPaymentService posPaymentService,
            IPosReversalService posPaymentReversalService,
            IPosAbortService posAbortService,
            IPosCardAcquisitionService posCardAcquisitionService,
            IPosCardAcquisitionPaymentService posCardAcquisitionPaymentService,
            IPosCardAcquisitionAbortService posCardAcquisitionAbortService,
            ITableRepository tableService,
            ICardAcquisitionRepository cardAcquisitionRepository,
            IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _posPaymentService = posPaymentService;
            _posPaymentReversalService = posPaymentReversalService;
            _posAbortService = posAbortService;
            _posCardAcquisitionService = posCardAcquisitionService;
            _posCardAcquisitionPaymentService = posCardAcquisitionPaymentService;
            _posCardAcquisitionAbortService = posCardAcquisitionAbortService;
            _tableService = tableService;
            _cardAcquisitionRepository = cardAcquisitionRepository;
            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;
        }
        
        [Route("card-acquisitions")]
        public ActionResult CardAcquisitions(CancellationToken cancellationToken = default)
        {
            var cardAcquisitions = _cardAcquisitionRepository.CardAcquisitions.Select(x => x.ToJson()).ToList();
            var jsonString = JsonConvert.SerializeObject(cardAcquisitions, Formatting.Indented);

            return Ok(jsonString);
        }

        [Route("card-acquisition/create/{tableName}/{amount?}")]
        public async Task<ActionResult> CreateCardAcquisition(string tableName, decimal? amount, CancellationToken cancellationToken = default)
        {
            var pizza = _tableService.Tables.FirstOrDefault(t => t.TableName == tableName);

            if (pizza == null)
            {
                return NotFound(new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = $"Pizza {pizza.TableName} not found"
                });
            }

            if (amount == null || amount.Value <= 0)
            {
                return BadRequest();
            }
            
            try
            {
                SaleToPOIResponse response = await _posCardAcquisitionService.SendCardAcquisitionRequestAsync(IdUtility.GetRandomAlphanumericId(10), _poiId, _saleId, amount, cancellationToken: cancellationToken);
                CardAcquisitionResponse cardAcquisitionResponse = response.MessagePayload as CardAcquisitionResponse;

                if (cardAcquisitionResponse == null)
                {
                    return NotFound($"Device with POI ID {_poiId} found.");
                }

                if (cardAcquisitionResponse.Response.Result != ResultType.Success)
                {
                    return BadRequest(cardAcquisitionResponse); // Unsuccessful
                }

                // Decode the base64 encoded string.
                string decodedUTF8JsonString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse.Response.AdditionalResponse));

                CardAcquisitionRoot json = JsonConvert.DeserializeObject<CardAcquisitionRoot>(decodedUTF8JsonString);

                if (json == null)
                {
                    return BadRequest(); // Can't deserialize json into an object.
                }
                
                if (json.AdditionalData.GiftcardIndicator)
                {
                    return BadRequest(); // This is a giftcard. Can't attach a gift card to this.
                }

                var existingCustomer = _cardAcquisitionRepository.Get(cardAcquisitionResponse.PaymentInstrumentData?.CardData?.PaymentToken?.TokenValue);

                SaleToPOIResponse req;
                if (existingCustomer != null)//json.AdditionalData.ShopperReference != null
                {
                    // Existing Customer.
                    req = await _posCardAcquisitionPaymentService.SendPaymentRequestExistingCustomerAsync(
                        serviceId: IdUtility.GetRandomAlphanumericId(10),
                        poiId: _poiId,
                        saleId: _saleId,
                        currency: "EUR",
                        amount: amount,
                        cardAcquisitionTimeStamp: cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                        cardAcquisitionTransactionId: cardAcquisitionResponse.POIData.POITransactionID.TransactionID,
                        transactionId: existingCustomer.ShopperEmail,
                        cancellationToken: cancellationToken
                    );
                    
                    
                    var pr = req.MessagePayload as PaymentResponse;
                    if (pr == null || pr.Response.Result != ResultType.Success)
                    {
                        return BadRequest();
                    }

                    existingCustomer.LoyaltyPoints += 1000;

                    pizza.PaymentStatusDetails.PoiTransactionId = pr.POIData.POITransactionID.TransactionID;
                    pizza.PaymentStatusDetails.PoiTransactionTimeStamp = pr.POIData.POITransactionID.TimeStamp;
                    pizza.PaymentStatusDetails.SaleTransactionId = pr.SaleData.SaleTransactionID.TransactionID;
                    pizza.PaymentStatusDetails.SaleTransactionTimeStamp = pr.SaleData.SaleTransactionID.TimeStamp;

                    var model = new CardAcquisitionModel()
                    {
                        // If you are going to continue with a payment, keep the TimeStamp and TransactionID, because you need these card acquisition details in your payment request.
                        POIReconciliationID = cardAcquisitionResponse.POIData.POIReconciliationID,
                        PoiTransactionTimeStamp = cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                        PoiTransactionId = cardAcquisitionResponse.POIData.POITransactionID.TransactionID,

                        CardCountryCode = cardAcquisitionResponse.PaymentInstrumentData.CardData.CardCountryCode,
                        MaskedPAN = cardAcquisitionResponse.PaymentInstrumentData.CardData.MaskedPAN,
                        PaymentBrand = cardAcquisitionResponse.PaymentInstrumentData.CardData.PaymentBrand,
                        PaymentToken = cardAcquisitionResponse.PaymentInstrumentData.CardData.PaymentToken.TokenValue, // Provided if the TokenRequestedType == Customer
                        ExpiryDate = cardAcquisitionResponse.PaymentInstrumentData.CardData.SensitiveCardData.ExpiryDate,
                        PaymentInstrumentType = cardAcquisitionResponse.PaymentInstrumentData.PaymentInstrumentType,

                        Alias = json.AdditionalData.Alias,
                        PaymentAccountReference = json.AdditionalData.PaymentAccountReference,
                        CardBin = json.AdditionalData.CardBin,
                        IssuerCountry = json.AdditionalData.IssuerCountry,
                        ShopperEmail = existingCustomer.ShopperEmail,
                        ShopperReference = existingCustomer.ShopperReference,

                        GiftCardIndicator = json.AdditionalData.GiftcardIndicator,

                        LoyaltyPoints = existingCustomer.LoyaltyPoints, 
                        SaleTransactionId = cardAcquisitionResponse.SaleData.SaleTransactionID.TransactionID,
                        SaleTransactionTimeStamp = cardAcquisitionResponse.SaleData.SaleTransactionID.TimeStamp
                    };

                    _cardAcquisitionRepository.CardAcquisitions.Add(model);

                    return Ok(model);
                }
                else
                {
                    var registerCustomerRequest = await _posCardAcquisitionPaymentService.RegisterCustomerAsync(
                        serviceId: IdUtility.GetRandomAlphanumericId(10),
                        poiId: _poiId,
                        saleId: _saleId,
                        cancellationToken: cancellationToken);

                    var inputResponse = registerCustomerRequest.MessagePayload as InputResponse;

                    if (inputResponse == null || inputResponse.InputResult.Response.Result != ResultType.Success)
                    {
                        return BadRequest();
                    }

                    if (inputResponse.InputResult.Input.ConfirmedFlag.HasValue && inputResponse.InputResult.Input.ConfirmedFlag.Value)
                    {
                        SaleToPOIResponse enterEmailAddressResponse = await _posCardAcquisitionPaymentService.EnterEmailAddressAsync(
                            serviceId: IdUtility.GetRandomAlphanumericId(10),
                            poiId: _poiId,
                            saleId: _saleId,
                            cancellationToken: cancellationToken);

                        var inputEmailResponse = enterEmailAddressResponse.MessagePayload as InputResponse;
                        if (inputEmailResponse == null || inputEmailResponse.InputResult.Response.Result != ResultType.Success)
                        {
                            return BadRequest();
                        }
                        
                        _logger.LogInformation(inputEmailResponse.InputResult.Input.TextInput);

                        string newEmail = inputEmailResponse.InputResult.Input.TextInput;

                        if (string.IsNullOrWhiteSpace(newEmail))
                        {
                            return BadRequest("Invalid email");
                        }

                        // New Customer.
                        req = await _posCardAcquisitionPaymentService.SendPaymentRequestNewCustomerAsync(
                            serviceId: IdUtility.GetRandomAlphanumericId(10),
                            poiId: _poiId,
                            saleId: _saleId,
                            currency: "EUR",
                            amount: amount,
                            shopperEmail: newEmail,
                            shopperReference: Identifiers.ShopperReference,
                            cardAcquisitionTimeStamp: cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                            cardAcquisitionTransactionId: cardAcquisitionResponse.POIData.POITransactionID.TransactionID,
                            transactionId: newEmail,
                            cancellationToken: cancellationToken
                        );
                        
                        var pr = req.MessagePayload as PaymentResponse;
                        if (pr == null || pr.Response.Result != ResultType.Success)
                        {
                            return BadRequest();
                        }

                        pizza.PaymentStatusDetails.PoiTransactionId = pr.POIData.POITransactionID.TransactionID;
                        pizza.PaymentStatusDetails.PoiTransactionTimeStamp = pr.POIData.POITransactionID.TimeStamp;
                        pizza.PaymentStatusDetails.SaleTransactionId = pr.SaleData.SaleTransactionID.TransactionID;
                        pizza.PaymentStatusDetails.SaleTransactionTimeStamp = pr.SaleData.SaleTransactionID.TimeStamp;

                        var model = new CardAcquisitionModel()
                        {
                            // If you are going to continue with a payment, keep the TimeStamp and TransactionID, because you need these card acquisition details in your payment request.
                            POIReconciliationID = cardAcquisitionResponse.POIData.POIReconciliationID,
                            PoiTransactionTimeStamp = cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                            PoiTransactionId = cardAcquisitionResponse.POIData.POITransactionID.TransactionID,

                            CardCountryCode = cardAcquisitionResponse.PaymentInstrumentData.CardData.CardCountryCode,
                            MaskedPAN = cardAcquisitionResponse.PaymentInstrumentData.CardData.MaskedPAN,
                            PaymentBrand = cardAcquisitionResponse.PaymentInstrumentData.CardData.PaymentBrand,
                            PaymentToken = cardAcquisitionResponse.PaymentInstrumentData.CardData.PaymentToken.TokenValue, // Provided if the TokenRequestedType == Customer
                            ExpiryDate = cardAcquisitionResponse.PaymentInstrumentData.CardData.SensitiveCardData.ExpiryDate,
                            PaymentInstrumentType = cardAcquisitionResponse.PaymentInstrumentData.PaymentInstrumentType,

                            Alias = json.AdditionalData.Alias,
                            PaymentAccountReference = json.AdditionalData.PaymentAccountReference,
                            CardBin = json.AdditionalData.CardBin,
                            IssuerCountry = json.AdditionalData.IssuerCountry,
                            ShopperEmail = newEmail,
                            ShopperReference = Identifiers.ShopperReference,

                            GiftCardIndicator = json.AdditionalData.GiftcardIndicator,

                            LoyaltyPoints = 1000, 
                            SaleTransactionId = cardAcquisitionResponse.SaleData.SaleTransactionID.TransactionID,
                            SaleTransactionTimeStamp = cardAcquisitionResponse.SaleData.SaleTransactionID.TimeStamp
                        };

                        _cardAcquisitionRepository.CardAcquisitions.Add(model);

                        return Ok(model);
                    }

                    req = await _posCardAcquisitionPaymentService.SendPaymentRequestNewCustomerAsync(
                        serviceId: IdUtility.GetRandomAlphanumericId(10),
                        poiId: _poiId,
                        saleId: _saleId,
                        currency: "EUR",
                        amount: amount,
                        shopperEmail: null,
                        shopperReference: Identifiers.ShopperReference,
                        cardAcquisitionTimeStamp: cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                        cardAcquisitionTransactionId: cardAcquisitionResponse.POIData.POITransactionID.TransactionID,
                        transactionId: Guid.NewGuid().ToString(),
                        cancellationToken: cancellationToken
                    );

                    var noLoyaltyPaymentResponse = req.MessagePayload as PaymentResponse;
                    if (noLoyaltyPaymentResponse == null || noLoyaltyPaymentResponse.Response.Result != ResultType.Success)
                    {
                        return BadRequest();
                    }

                    pizza.PaymentStatusDetails.PoiTransactionId = noLoyaltyPaymentResponse.POIData.POITransactionID.TransactionID;
                    pizza.PaymentStatusDetails.PoiTransactionTimeStamp = noLoyaltyPaymentResponse.POIData.POITransactionID.TimeStamp;
                    pizza.PaymentStatusDetails.SaleTransactionId = noLoyaltyPaymentResponse.SaleData.SaleTransactionID.TransactionID;
                    pizza.PaymentStatusDetails.SaleTransactionTimeStamp = noLoyaltyPaymentResponse.SaleData.SaleTransactionID.TimeStamp;

                    // Customer doesn't want to be part of the loyalty program.
                    if (noLoyaltyPaymentResponse.Response.Result == ResultType.Success)
                        return Ok(new { loyaltyPoints = 0 });
                    return BadRequest();
                    // Abort case.
                    // SaleToPOIResponse abortRequest = await _posCardAcquisitionAbortService.SendAbortRequestAsync(IdUtility.GetRandomAlphanumericId(10), _poiId, _saleId, cancellationToken: cancellationToken);
                    //return Ok(abortRequest);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }

        [Route("card-acquisition/check")]
        public async Task<ActionResult> CheckPoints(CancellationToken cancellationToken = default)
        {
            try
            {
                SaleToPOIResponse response = await _posCardAcquisitionService.SendCardAcquisitionRequestAsync(IdUtility.GetRandomAlphanumericId(10), _poiId, _saleId, 0, cancellationToken: cancellationToken);
                CardAcquisitionResponse cardAcquisitionResponse = response.MessagePayload as CardAcquisitionResponse;

                if (cardAcquisitionResponse == null)
                {
                    return NotFound($"Device with POI ID {_poiId} found.");
                }

                if (cardAcquisitionResponse.Response.Result != ResultType.Success)
                {
                    return BadRequest(cardAcquisitionResponse); // Unsuccessful
                }

                // Decode the base64 encoded string.
                string decodedUTF8JsonString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse.Response.AdditionalResponse));

                CardAcquisitionRoot json = JsonConvert.DeserializeObject<CardAcquisitionRoot>(decodedUTF8JsonString);

                if (json == null)
                {
                    return BadRequest(); // Can't deserialize json into an object.
                }

                if (json.AdditionalData.GiftcardIndicator)
                {
                    return BadRequest(); // This is a giftcard. Can't attach a gift card to this.
                }

                CardAcquisitionModel existingCustomer = _cardAcquisitionRepository.Get(cardAcquisitionResponse.PaymentInstrumentData?.CardData?.PaymentToken?.TokenValue);

                if (existingCustomer != null)
                {

                    if (existingCustomer.LoyaltyPoints >= 3000)
                    {
                        _tableService.ApplyDiscount(0.5M); // 50%
                    }
                    else if (existingCustomer.LoyaltyPoints >= 2000)
                    {
                        _tableService.ApplyDiscount(0.75M); // 25%
                    }
                    else if (existingCustomer.LoyaltyPoints >= 1000)
                    {
                        _tableService.ApplyDiscount(0.9M); // 10%
                    }

                    SaleToPOIResponse abortRequest = await _posCardAcquisitionAbortService.SendAbortRequestAsync(IdUtility.GetRandomAlphanumericId(10), _poiId, _saleId, success: true, loyaltyPoints: existingCustomer.LoyaltyPoints, cancellationToken: cancellationToken);
                    
                    return Ok(existingCustomer);
                }

                SaleToPOIResponse ar = await _posCardAcquisitionAbortService.SendAbortRequestAsync(IdUtility.GetRandomAlphanumericId(10), _poiId, _saleId, success: false, cancellationToken: cancellationToken);
                return Ok() ;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                await _posCardAcquisitionAbortService.SendAbortRequestAsync(IdUtility.GetRandomAlphanumericId(10), _poiId, _saleId, success: false, cancellationToken: cancellationToken);
                throw;
            }
        }

        [HttpGet("card-acquisition/abort/{tableName}")]
        public async Task<ActionResult<SaleToPOIResponse>> Abort(string tableName, CancellationToken cancellationToken = default)
        {
            try
            {
                TableModel table = _tableService.Tables.FirstOrDefault(t => t.TableName == tableName);

                if (table?.PaymentStatusDetails?.ServiceId == null)
                {
                    return NotFound();
                }

                SaleToPOIResponse abortResponse = await _posAbortService.SendAbortRequestAsync(MessageCategoryType.CardAcquisition, table.PaymentStatusDetails.ServiceId, _poiId, _saleId, cancellationToken);
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