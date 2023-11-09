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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    [ApiController]
    public class CardAcquisitionController : ControllerBase
    {
        private readonly ILogger<CardAcquisitionController> _logger;
        private readonly IPosAbortService _posAbortService;
        private readonly IPosCardAcquisitionService _posCardAcquisitionService;
        private readonly IPosCardAcquisitionPaymentService _posCardAcquisitionPaymentService;
        private readonly IPosCardAcquisitionAbortService _posCardAcquisitionAbortService;
        private readonly IPosInputService _posInputService;
        private readonly IPizzaRepository _pizzaRepository;
        private readonly IShopperRepository _shopperRepository;

        private readonly string _saleId;
        private readonly string _poiId;

        public CardAcquisitionController(ILogger<CardAcquisitionController> logger,
            IPosAbortService posAbortService,
            IPosCardAcquisitionService posCardAcquisitionService,
            IPosCardAcquisitionPaymentService posCardAcquisitionPaymentService,
            IPosCardAcquisitionAbortService posCardAcquisitionAbortService,
            IPosInputService posInputService,
            IPizzaRepository pizzaRepository,
            IShopperRepository shopperRepository,
            IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _posAbortService = posAbortService;
            _posCardAcquisitionService = posCardAcquisitionService;
            _posCardAcquisitionPaymentService = posCardAcquisitionPaymentService;
            _posCardAcquisitionAbortService = posCardAcquisitionAbortService;
            _posInputService = posInputService;
            _pizzaRepository = pizzaRepository;
            _shopperRepository = shopperRepository;
            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;
        }

        [Route("shoppers")]
        public ActionResult CardAcquisitions(CancellationToken cancellationToken = default)
        {
            var shoppers = _shopperRepository.Shoppers.Select(kvp => kvp.Value.ToJson()).ToList();
            var jsonString = JsonConvert.SerializeObject(shoppers, Formatting.Indented);
            return Ok(jsonString);
        }

        [Route("card-acquisition/create/{pizzaName}")]
        public async Task<ActionResult> CreateCardAcquisition(string pizzaName, CancellationToken cancellationToken = default)
        {
            var pizza = _pizzaRepository.Pizzas.FirstOrDefault(t => t.PizzaName == pizzaName);

            if (pizza == null)
            {
                return NotFound(new CreatePaymentResponse()
                {
                    Result = "failure",
                    RefusalReason = $"Pizza {pizza.PizzaName} not found"
                });
            }

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
                    return NotFound($"Device with POI ID {_poiId} not found, is the device turned on and up-to-date?");
                }

                if (cardAcquisitionResponse.Response.Result != ResultType.Success)
                {
                    return BadRequest(new CreatePaymentResponse()
                    {
                        Result = "failure",
                        RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse?.Response?.AdditionalResponse))
                    });
                }

                //string alias = cardAcquisitionResponse.PaymentInstrumentData?.CardData?.PaymentToken?.TokenValue;

                // Decode the base64 encoded string.
                string decodedUtf8JsonString = Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse.Response.AdditionalResponse));

                _logger.LogInformation(decodedUtf8JsonString);

                CardAcquisitionRoot json = JsonConvert.DeserializeObject<CardAcquisitionRoot>(decodedUtf8JsonString);

                if (json == null)
                {
                    return BadRequest(); // Can't deserialize json into an object.
                }

                if (json.AdditionalData.GiftcardIndicator)
                {
                    return BadRequest(new CreatePaymentResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Card provided is a gift card"
                    }); // This is a gift card, handle gift card logic accordingly. For now, do not support card acquisition for gift cards.
                }
                
                // Note that if you specified a TokenRequestedType of Customer in the request, the card alias is also provided in cardAcquisitionResponse.PaymentInstrumentData.CardData.PaymentToken.TokenValue.
                string alias = json.AdditionalData.Alias;
                
                ShopperModel shopper = _shopperRepository.Get(alias);

                if (!_shopperRepository.IsSignedUpForLoyaltyProgram(alias))
                {
                    // User is not signed up for the loyalty program, let's collect their details if they consent
                    var enterLoyaltyProgramResponse = await _posInputService.GetConfirmationAsync(
                        serviceId: IdUtility.GetRandomAlphanumericId(10),
                        poiId: _poiId,
                        saleId: _saleId,
                        text: "Would you like to join our membership program?",
                        maxInputTime: 30,
                        cancellationToken: cancellationToken);

                    if (enterLoyaltyProgramResponse == null || enterLoyaltyProgramResponse.Response.Result != ResultType.Success)
                    {
                        _logger.LogError("Error sending loyalty program confirmation request");
                        return BadRequest(new CreatePaymentResponse()
                        {
                            Result = "failure",
                            RefusalReason =  Encoding.UTF8.GetString(Convert.FromBase64String(enterLoyaltyProgramResponse?.Response?.AdditionalResponse))
                        });
                    }

                    if (enterLoyaltyProgramResponse.Input.ConfirmedFlag.HasValue && enterLoyaltyProgramResponse.Input.ConfirmedFlag.Value)
                    {
                        InputResult enterEmailAddressResponse = await _posInputService.GetTextAsync(
                            serviceId: IdUtility.GetRandomAlphanumericId(10),
                            poiId: _poiId,
                            saleId: _saleId,
                            text: "Enter your email address",
                            defaultInputString: "yourname@domain.com",
                            maxInputTime: 90,
                            cancellationToken: cancellationToken);

                        if (enterEmailAddressResponse == null || enterEmailAddressResponse.Response.Result != ResultType.Success)
                        {
                            _logger.LogError("Error sending email request");
                            return BadRequest(new CreatePaymentResponse()
                            {
                                Result = "failure",
                                RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(enterEmailAddressResponse?.Response?.AdditionalResponse))
                            });
                        }

                        string email = enterEmailAddressResponse.Input.TextInput;
                        _logger.LogInformation("Your email: {email}", email);

                        if (!StringUtility.IsValidEmail(email))
                        {
                            return BadRequest(new CreatePaymentResponse()
                            {
                                Result = "failure",
                                RefusalReason = "Invalid email"
                            });
                        }

                        shopper = _shopperRepository.AddIfNotExists(
                            alias: alias,
                            shopperReference: Guid.NewGuid().ToString(),
                            shopperEmail: email,
                            isLoyaltyMember: true,
                            loyaltyPoints: 0);

                        var newCustomerResponse =
                            await _posCardAcquisitionPaymentService.SendPaymentRequestNewCustomerAsync(
                                serviceId: IdUtility.GetRandomAlphanumericId(10),
                                poiId: _poiId,
                                saleId: _saleId,
                                currency: "EUR",
                                amount: pizza.Amount,
                                shopperEmail: shopper.ShopperEmail,
                                shopperReference: shopper.ShopperReference,
                                cardAcquisitionTimeStamp: cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                                cardAcquisitionTransactionId: cardAcquisitionResponse.POIData.POITransactionID.TransactionID,
                                transactionId: transactionId,
                                cancellationToken: cancellationToken
                            );


                        var newCustomerPaymentResponse = newCustomerResponse.MessagePayload as PaymentResponse;
                        if (newCustomerPaymentResponse == null || newCustomerPaymentResponse.Response.Result != ResultType.Success)
                        {
                            return BadRequest(new CreatePaymentResponse()
                            {
                                Result = "failure",
                                RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(newCustomerPaymentResponse?.Response?.AdditionalResponse))
                            });
                        }

                        _logger.LogInformation(Encoding.UTF8.GetString(Convert.FromBase64String(newCustomerPaymentResponse.Response.AdditionalResponse)));
                        
                        // Set some values to show on the frontend once a payment is completed.
                        pizza.PaymentStatusDetails.PoiTransactionId = newCustomerPaymentResponse.POIData.POITransactionID.TransactionID;
                        pizza.PaymentStatusDetails.PoiTransactionTimeStamp = newCustomerPaymentResponse.POIData.POITransactionID.TimeStamp;
                        pizza.PaymentStatusDetails.SaleTransactionId = newCustomerPaymentResponse.SaleData.SaleTransactionID.TransactionID;
                        pizza.PaymentStatusDetails.SaleTransactionTimeStamp = newCustomerPaymentResponse.SaleData.SaleTransactionID.TimeStamp;

                        _shopperRepository.AddLoyaltyPoints(shopper.ShopperReference, 1000);

                        return Ok(new CreatePaymentResponse()
                        {
                            Result = "success"
                        });
                    }
                }

                // User is already signed-up for loyalty program
                SaleToPOIResponse existingCustomerResponse =
                    await _posCardAcquisitionPaymentService.SendPaymentRequestExistingCustomerAsync(
                        serviceId: IdUtility.GetRandomAlphanumericId(10),
                        poiId: _poiId,
                        saleId: _saleId,
                        currency: "EUR",
                        amount: pizza.Amount,
                        cardAcquisitionTimeStamp: cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                        cardAcquisitionTransactionId: cardAcquisitionResponse.POIData.POITransactionID
                            .TransactionID,
                        transactionId: transactionId,
                        cancellationToken: cancellationToken
                    );

                var existingCustomerPaymentResponse = existingCustomerResponse.MessagePayload as PaymentResponse;
                if (existingCustomerPaymentResponse == null ||
                    existingCustomerPaymentResponse.Response.Result != ResultType.Success)
                {
                    return BadRequest(new CreatePaymentResponse()
                    {
                        Result = "failure",
                        RefusalReason = existingCustomerPaymentResponse?.Response?.AdditionalResponse,
                    });
                }

                _logger.LogInformation(Encoding.UTF8.GetString(Convert.FromBase64String(existingCustomerPaymentResponse.Response.AdditionalResponse)));

                pizza.PaymentStatusDetails.PoiTransactionId = existingCustomerPaymentResponse.POIData.POITransactionID.TransactionID;
                pizza.PaymentStatusDetails.PoiTransactionTimeStamp = existingCustomerPaymentResponse.POIData.POITransactionID.TimeStamp;
                pizza.PaymentStatusDetails.SaleTransactionId = existingCustomerPaymentResponse.SaleData.SaleTransactionID.TransactionID;
                pizza.PaymentStatusDetails.SaleTransactionTimeStamp = existingCustomerPaymentResponse.SaleData.SaleTransactionID.TimeStamp;

                _shopperRepository.AddLoyaltyPoints(alias, 1000);

                return Ok(new CreatePaymentResponse()
                {
                    Result = "success"
                });
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
                string transactionId = Guid.NewGuid().ToString();
                SaleToPOIResponse response = await _posCardAcquisitionService.SendCardAcquisitionRequestAsync(
                    serviceId: IdUtility.GetRandomAlphanumericId(10),
                    poiId: _poiId,
                    saleId: _saleId,
                    transactionId: transactionId,
                    cancellationToken: cancellationToken);

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
                string decodedUtf8JsonString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse.Response.AdditionalResponse));

                CardAcquisitionRoot json = JsonConvert.DeserializeObject<CardAcquisitionRoot>(decodedUtf8JsonString);

                if (json == null)
                {
                    return BadRequest(); // Can't deserialize json into an object.
                }

                if (json.AdditionalData.GiftcardIndicator)
                {
                    return BadRequest(); // This is a giftcard. Can't attach a gift card to this.
                }

                var existingCustomer = _shopperRepository.Get(json.AdditionalData.Alias);

                if (existingCustomer != null)
                {
                    SaleToPOIResponse abortRequest = await _posCardAcquisitionAbortService.SendAbortRequestAsync(
                        serviceId: IdUtility.GetRandomAlphanumericId(10),
                        poiId: _poiId,
                        saleId: _saleId,
                        cancellationToken: cancellationToken);

                    return Ok(existingCustomer);
                }

                SaleToPOIResponse ar = await _posCardAcquisitionAbortService.SendAbortRequestAsync(
                    serviceId: IdUtility.GetRandomAlphanumericId(10),
                    poiId: _poiId,
                    saleId: _saleId,
                    cancellationToken: cancellationToken);
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                await _posCardAcquisitionAbortService.SendAbortRequestAsync(
                    serviceId: IdUtility.GetRandomAlphanumericId(10), 
                    poiId: _poiId, 
                    saleId: _saleId, 
                    cancellationToken: cancellationToken); // TODO: serviceId, top-level
                throw;
            }
        }

        [HttpGet("card-acquisition/abort/{pizzaName}")]
        public async Task<ActionResult<SaleToPOIResponse>> Abort(string pizzaName, CancellationToken cancellationToken = default)
        {
            try
            {
                PizzaModel pizza = _pizzaRepository.Pizzas.FirstOrDefault(t => t.PizzaName == pizzaName);

                if (pizza?.PaymentStatusDetails?.ServiceId == null)
                {
                    return NotFound();
                }

                SaleToPOIResponse abortResponse = await _posAbortService.SendAbortRequestAsync(
                    messageCategoryType: MessageCategoryType.CardAcquisition,
                    serviceId: pizza.PaymentStatusDetails.ServiceId,
                    poiId: _poiId,
                    saleId: _saleId,
                    cancellationToken: cancellationToken);
                return Ok(abortResponse);
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(e.Code, new CreatePaymentResponse() // TODO
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