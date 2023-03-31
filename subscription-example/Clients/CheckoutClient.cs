using Adyen.Model.Checkout;
using Adyen.Service;
using adyen_dotnet_subscription_example.Options;
using adyen_dotnet_subscription_example.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Clients
{
    public interface ICheckoutClient
    {
        /// <summary>
        /// Initiates a session which creates the subscription for the given <paramref name="shopperReference"/>.
        /// Once completed, your endpoint <see cref="Controllers.WebhookController.Webhooks"/> will receive a notification with the recurringDetailReference.
        /// </summary>
        /// <param name="shopperReference">The unique shopper reference (usually a GUID to identify your shopper).</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="CreateCheckoutSessionResponse"/></returns>
        Task<CreateCheckoutSessionResponse> CheckoutSessionsAsync(string shopperReference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Makes a payment request using the <paramref name="recurringDetailReference"/> (token) for the given <paramref name="shopperReference"/>.
        /// </summary>
        /// <param name="shopperReference">The unique shopper reference (usually a GUID to identify your shopper).</param>
        /// <param name="recurringDetailReference">The <paramref name="recurringDetailReference"/> token that is retrieved from <see cref="Controllers.WebhookController.Webhooks"/>.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="PaymentResponse"/></returns>
        Task<PaymentResponse> MakePaymentAsync(string shopperReference, string recurringDetailReference, CancellationToken cancellationToken = default);
    }

    public class CheckoutClient : ICheckoutClient
    {
        private readonly ILogger<RecurringClient> _logger;
        private readonly string _merchantAccount;
        private readonly Checkout _checkout;
        private readonly IUrlService _urlService;

        public CheckoutClient(ILogger<RecurringClient> logger, Checkout checkout, IUrlService urlService, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _checkout = checkout;
            _urlService = urlService;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        public async Task<CreateCheckoutSessionResponse> CheckoutSessionsAsync(string shopperReference, CancellationToken cancellationToken)
        {
            var orderRef = Guid.NewGuid();

            var sessionsRequest = new CreateCheckoutSessionRequest();
            sessionsRequest.MerchantAccount = _merchantAccount; // Required.
            sessionsRequest.Amount = new Amount("EUR", 0);
            sessionsRequest.Reference = orderRef.ToString(); // Required.

            sessionsRequest.Channel = CreateCheckoutSessionRequest.ChannelEnum.Web;
            sessionsRequest.ShopperInteraction = CreateCheckoutSessionRequest.ShopperInteractionEnum.Ecommerce;
            sessionsRequest.RecurringProcessingModel = CreateCheckoutSessionRequest.RecurringProcessingModelEnum.Subscription;
            sessionsRequest.EnableRecurring = true;

            sessionsRequest.ShopperReference = shopperReference;

            // Required for 3DS2 redirect flow.
            sessionsRequest.ReturnUrl = $"{_urlService.GetHostUrl()}/redirect?orderRef={orderRef}";

            try
            {
                var sessionResponse = await _checkout.SessionsAsync(sessionsRequest);
                _logger.LogInformation($"Response for Payments API:\n{sessionResponse}\n");
                return sessionResponse;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

        public async Task<PaymentResponse> MakePaymentAsync(string shopperReference, string recurringDetailReference, CancellationToken cancellationToken)
        {
            var details = new DefaultPaymentMethodDetails
            {
                StoredPaymentMethodId = recurringDetailReference // Set the RecurringDetailReference.
            };

            var paymentsRequest = new PaymentRequest
            {
                Reference = Guid.NewGuid().ToString(), // Required.
                Amount = new Amount("EUR", 1199),
                MerchantAccount = _merchantAccount, // Required.
                ShopperInteraction = PaymentRequest.ShopperInteractionEnum.ContAuth, // Set the shopper InteractionEnum to Cont.Auth.
                RecurringProcessingModel = PaymentRequest.RecurringProcessingModelEnum.Subscription,
                ShopperReference = shopperReference, // Set the ShopperReference.
                PaymentMethod = details
            };

            try
            {
                var paymentResponse = await _checkout.PaymentsAsync(paymentsRequest);
                _logger.LogInformation($"Response for Payments API:\n{paymentResponse}\n");
                return paymentResponse;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed:\n{e.ResponseBody}\n");
                throw;
            }
        }

    }
}
