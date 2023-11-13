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
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    [ApiController]
    [Route("cash-register")]
    public class CardAcquisitionController : ControllerBase
    {
        private readonly ILogger<CardAcquisitionController> _logger;
        private readonly IPosCardAcquisitionService _posCardAcquisitionService;
        private readonly IPosCardAcquisitionPaymentService _posCardAcquisitionPaymentService;
        private readonly IPosCardAcquisitionAbortService _posCardAcquisitionAbortService;
        private readonly IPosAbortService _posAbortService;
        private readonly IPosInputService _posInputService;
        private readonly IPizzaRepository _pizzaRepository;
        private readonly IShopperRepository _shopperRepository;

        private readonly string _saleId;
        private readonly string _poiId;

        public CardAcquisitionController(ILogger<CardAcquisitionController> logger,
            IPosCardAcquisitionService posCardAcquisitionService,
            IPosCardAcquisitionPaymentService posCardAcquisitionPaymentService,
            IPosCardAcquisitionAbortService posCardAcquisitionAbortService,
            IPosAbortService posAbortService,
            IPosInputService posInputService,
            IPizzaRepository pizzaRepository,
            IShopperRepository shopperRepository,
            IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _posCardAcquisitionService = posCardAcquisitionService;
            _posCardAcquisitionPaymentService = posCardAcquisitionPaymentService;
            _posCardAcquisitionAbortService = posCardAcquisitionAbortService;
            _posAbortService = posAbortService;
            _posInputService = posInputService;
            _pizzaRepository = pizzaRepository;
            _shopperRepository = shopperRepository;
            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;
        }
        
        [Route("create/{pizzaName}")]
        public async Task<ActionResult<ApiResponse>> CreateCardAcquisitionPayment(string pizzaName, CancellationToken cancellationToken = default)
        {
            var pizza = _pizzaRepository.Pizzas.FirstOrDefault(t => t.PizzaName == pizzaName);

            if (pizza == null)
            {
                return NotFound(new ApiResponse()
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
                    return NotFound(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = $"Cannot reach device with POI ID {_poiId}"
                    });
                }

                if (cardAcquisitionResponse.Response.Result != ResultType.Success)
                {
                    return BadRequest(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse?.Response?.AdditionalResponse))
                    });
                }

                // Decode the base64 encoded string.
                string decodedUtf8JsonString = Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse.Response.AdditionalResponse));

                _logger.LogInformation(decodedUtf8JsonString);

                CardAcquisitionModel json = JsonConvert.DeserializeObject<CardAcquisitionModel>(decodedUtf8JsonString);

                if (json == null)
                {
                    // Can't deserialize json into an object.
                    return BadRequest(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Unable to deserialize json"
                    }); 
                }

                // This is a gift card, handle gift card logic accordingly. For now, do not support card acquisition for gift cards.
                if (json.AdditionalData.GiftcardIndicator)
                {
                    return BadRequest(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Card provided is a gift card"
                    }); 
                }
                
                // Note that if you specified a TokenRequestedType of Customer in the request, the card alias is also provided in cardAcquisitionResponse.PaymentInstrumentData.CardData.PaymentToken.TokenValue.
                string alias = json.AdditionalData.Alias;
                
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
                        return BadRequest(new ApiResponse()
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
                            return BadRequest(new ApiResponse()
                            {
                                Result = "failure",
                                RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(enterEmailAddressResponse?.Response?.AdditionalResponse))
                            });
                        }

                        string email = enterEmailAddressResponse.Input.TextInput;
                        _logger.LogInformation("Your email: {email}", email);

                        // Perform email validation
                        /*if (!StringUtility.IsValidEmail(email))
                        {
                            return BadRequest(new ApiResponse()
                            {
                                Result = "failure",
                                RefusalReason = "Invalid email"
                            });
                        }*/
                        
                        ShopperModel shopper = _shopperRepository.AddIfNotExists(
                            alias: alias,
                            shopperReference: Guid.NewGuid().ToString(),
                            shopperEmail: email,
                            isLoyaltyMember: true,
                            loyaltyPoints: 0);

                        var newCustomerResponse = await _posCardAcquisitionPaymentService.SendPaymentRequestNewCustomerAsync(
                                serviceId: IdUtility.GetRandomAlphanumericId(10),
                                poiId: _poiId,
                                saleId: _saleId,
                                currency: pizza.Currency,
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
                            return BadRequest(new ApiResponse()
                            {
                                Result = "failure",
                                RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(newCustomerPaymentResponse?.Response?.AdditionalResponse))
                            });
                        }

                        _logger.LogInformation(Encoding.UTF8.GetString(Convert.FromBase64String(newCustomerPaymentResponse.Response.AdditionalResponse)));

                        _shopperRepository.AddLoyaltyPoints(alias, 100);
                        _shopperRepository.AddShopperTransaction(alias, pizza.Amount, pizza.Currency, pizza.PizzaName, newCustomerResponse.MessageHeader.ServiceID);

                        return Ok(new ApiResponse()
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
                        currency: pizza.Currency,
                        amount: pizza.Amount,
                        cardAcquisitionTimeStamp: cardAcquisitionResponse.POIData.POITransactionID.TimeStamp,
                        cardAcquisitionTransactionId: cardAcquisitionResponse.POIData.POITransactionID.TransactionID,
                        transactionId: transactionId,
                        cancellationToken: cancellationToken
                    );

                var existingCustomerPaymentResponse = existingCustomerResponse.MessagePayload as PaymentResponse;
                if (existingCustomerPaymentResponse == null || existingCustomerPaymentResponse.Response.Result != ResultType.Success)
                {
                    return BadRequest(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(existingCustomerPaymentResponse?.Response?.AdditionalResponse)),
                    });
                }

                _logger.LogInformation(Encoding.UTF8.GetString(Convert.FromBase64String(existingCustomerPaymentResponse.Response.AdditionalResponse)));

                _shopperRepository.AddLoyaltyPoints(alias, 100);
                _shopperRepository.AddShopperTransaction(alias, pizza.Amount, pizza.Currency, pizza.PizzaName, existingCustomerResponse.MessageHeader.ServiceID);

                return Ok(new ApiResponse()
                {
                    Result = "success"
                });
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(e.Code, new ApiResponse()
                {
                    Result = "failure",
                    RefusalReason = $"ErrorCode: {e.Code}, see logs"
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                await _posAbortService.SendAbortRequestAsync(
                    poiId: _poiId, 
                    saleId: _saleId, 
                    cancellationToken: cancellationToken);
                throw;
            }
        }

        [Route("apply-discount")]
        public async Task<ActionResult<ApiResponse>> ApplyDiscount(CancellationToken cancellationToken = default)
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
                    return NotFound(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = $"Cannot reach device with POI ID {_poiId}"
                    });
                }

                if (cardAcquisitionResponse.Response.Result != ResultType.Success)
                {
                    return BadRequest(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse?.Response?.AdditionalResponse))
                    });
                }

                // Decode the base64 encoded string.
                string decodedUtf8JsonString = Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse.Response.AdditionalResponse));

                _logger.LogInformation(decodedUtf8JsonString);

                CardAcquisitionModel json = JsonConvert.DeserializeObject<CardAcquisitionModel>(decodedUtf8JsonString);

                if (json == null)
                {
                    // Can't deserialize json into an object.
                    return BadRequest(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Unable to deserialize json"
                    }); 
                }

                // This is a gift card, handle gift card logic accordingly. For now, do not support card acquisition for gift cards.
                if (json.AdditionalData.GiftcardIndicator)
                {
                    return BadRequest(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Card provided is a gift card"
                    }); 
                }

                var existingCustomer = _shopperRepository.Get(json.AdditionalData.Alias);

                // A signed-up loyal shopper
                if (existingCustomer != null)
                {
                    if (existingCustomer.IsSignedUpForLoyaltyProgram)
                    {
                        if (existingCustomer.LoyaltyPoints >= 200)
                        {
                            _pizzaRepository.ApplyDiscount(20);

                            await _posCardAcquisitionAbortService.SendAbortRequestAfterSignUpAsync(
                                serviceId: IdUtility.GetRandomAlphanumericId(10),
                                poiId: _poiId,
                                saleId: _saleId,
                                textTitle: "Welcome back!",
                                textDescription: "We've discounted your pizza!",
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await _posCardAcquisitionAbortService.SendAbortRequestAfterSignUpAsync(
                                serviceId: IdUtility.GetRandomAlphanumericId(10),
                                poiId: _poiId,
                                saleId: _saleId,
                                textTitle: "Welcome back!",
                                textDescription: $"You need {200 - existingCustomer.LoyaltyPoints} loyalty points to get your next discount!",
                                cancellationToken: cancellationToken);
                        }
                    }
                }
                
                // Not a recognized loyal shopper 
                await _posCardAcquisitionAbortService.SendAbortRequestAfterSignUpAsync(
                    serviceId: IdUtility.GetRandomAlphanumericId(10),
                    poiId: _poiId,
                    saleId: _saleId,
                    textTitle: "Hello!",
                    textDescription: "You've not signed-up for our loyalty program.",
                    cancellationToken: cancellationToken);

                return Ok(new ApiResponse()
                {
                    Result = "success"
                });
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(e.Code, new ApiResponse()
                {
                    Result = "failure",
                    RefusalReason = $"ErrorCode: {e.Code}, see logs"
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                await _posAbortService.SendAbortRequestAsync(
                    poiId: _poiId, 
                    saleId: _saleId, 
                    cancellationToken: cancellationToken);
                throw;
            }
        }
        
        [Route("card-acquisition-only")]
        public async Task<ActionResult<ApiResponse>> CreateCardAcquisitionOnly(CancellationToken cancellationToken = default)
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
                    return NotFound(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = $"Cannot reach device with POI ID {_poiId}"
                    });
                }

                if (cardAcquisitionResponse.Response.Result != ResultType.Success)
                {
                    return BadRequest(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse?.Response?.AdditionalResponse))
                    });
                }

                // Decode the base64 encoded string.
                string decodedUtf8JsonString = Encoding.UTF8.GetString(Convert.FromBase64String(cardAcquisitionResponse.Response.AdditionalResponse));

                _logger.LogInformation(decodedUtf8JsonString);

                CardAcquisitionModel json = JsonConvert.DeserializeObject<CardAcquisitionModel>(decodedUtf8JsonString);

                if (json == null)
                {
                    // Can't deserialize json into an object.
                    return BadRequest(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Unable to deserialize json"
                    }); 
                }

                // This is a gift card, handle gift card logic accordingly. For now, do not support card acquisition for gift cards.
                if (json.AdditionalData.GiftcardIndicator)
                {
                    return BadRequest(new ApiResponse()
                    {
                        Result = "failure",
                        RefusalReason = "Card provided is a gift card"
                    }); 
                }
                
                // Note that if you specified a TokenRequestedType of Customer in the request, the card alias is also provided in cardAcquisitionResponse.PaymentInstrumentData.CardData.PaymentToken.TokenValue.
                string alias = json.AdditionalData.Alias;
                
                if (!_shopperRepository.IsSignedUpForLoyaltyProgram(alias))
                {
                    // User is not signed up for the loyalty program, let's collect their details if they consent.
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
                        return BadRequest(new ApiResponse()
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
                            return BadRequest(new ApiResponse()
                            {
                                Result = "failure",
                                RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(enterEmailAddressResponse?.Response?.AdditionalResponse))
                            });
                        }

                        string email = enterEmailAddressResponse.Input.TextInput;
                        _logger.LogInformation("Your email: {email}", email);

                        // Perform email validation
                        /*if (!StringUtility.IsValidEmail(email))
                        {
                            return BadRequest(new ApiResponse()
                            {
                                Result = "failure",
                                RefusalReason = "Invalid email"
                            });
                        }*/
                        
                        ShopperModel shopper = _shopperRepository.AddIfNotExists(
                            alias: alias,
                            shopperReference: Guid.NewGuid().ToString(),
                            shopperEmail: email,
                            isLoyaltyMember: true,
                            loyaltyPoints: 0);

                        
                        // Cancel the card acquisition after signing them up, indicating that the sign-up was successful
                        SaleToPOIResponse abortResponse =
                            await _posCardAcquisitionAbortService.SendAbortRequestAfterSignUpAsync(
                                serviceId: IdUtility.GetRandomAlphanumericId(10),
                                poiId: _poiId,
                                saleId: _saleId, 
                                textTitle: "Welcome!",
                                textDescription: "Thank you for joining our loyalty program!",
                                cancellationToken: cancellationToken
                            );

                        var enableServiceResponse = abortResponse.MessagePayload as EnableServiceResponse;

                        if (enableServiceResponse == null || enableServiceResponse.Response.Result != ResultType.Success)
                        {
                            return BadRequest(new ApiResponse()
                            {
                              Result  = "failure", 
                              RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(enableServiceResponse?.Response?.AdditionalResponse))
                            });
                        }
                        
                        return Ok(new ApiResponse()
                        {
                            Result = "success"
                        });
                    }
                }
                else
                {
                    // The shopper is already a member
                    SaleToPOIResponse abortResponse =
                        await _posCardAcquisitionAbortService.SendAbortRequestAfterSignUpAsync(
                            serviceId: IdUtility.GetRandomAlphanumericId(10),
                            poiId: _poiId,
                            saleId: _saleId,
                            textTitle: "Welcome back!",
                            textDescription: "You're already a part of our loyalty program!",
                            cancellationToken: cancellationToken
                        );


                    var enableServiceResponse = abortResponse.MessagePayload as EnableServiceResponse;

                    if (enableServiceResponse == null || enableServiceResponse.Response.Result != ResultType.Success)
                    {
                        return BadRequest(new ApiResponse()
                        {
                            Result = "failure",
                            RefusalReason = Encoding.UTF8.GetString(Convert.FromBase64String(enableServiceResponse?.Response?.AdditionalResponse))
                        });
                    }

                    return Ok(new ApiResponse()
                    {
                        Result = "success"
                    });
                }
                
                return Ok(new ApiResponse()
                {
                    Result = "success"
                });
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(e.Code, new ApiResponse()
                {
                    Result = "failure",
                    RefusalReason = $"ErrorCode: {e.Code}, see logs"
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                await _posAbortService.SendAbortRequestAsync(
                    poiId: _poiId, 
                    saleId: _saleId, 
                    cancellationToken: cancellationToken);
                throw;
            }
        }
        
        [Route("abort")]
        public async Task<ActionResult<ApiResponse>> Abort(CancellationToken cancellationToken = default)
        {
            try
            {
                SaleToPOIResponse abortResponse = await _posAbortService.SendAbortRequestAsync(
                    poiId: _poiId,
                    saleId: _saleId,
                    cancellationToken: cancellationToken);
                return Ok(abortResponse);
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                return StatusCode(e.Code, new ApiResponse()
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