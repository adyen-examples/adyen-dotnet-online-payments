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
        /// Initiates a session which creates a token (recurringDetailReference) for the given <paramref name="shopperReference"/>.
        /// </summary>
        /// <param name="shopperReference">The unique shopper reference (usually a GUID to uniquely identify your shopper).</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="CreateCheckoutSessionResponse"/></returns>
        Task<CreateCheckoutSessionResponse> CheckoutSessionsAsync(string shopperReference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Makes a payment request using the <paramref name="recurringDetailReference"/> (token) for the given <paramref name="shopperReference"/>.
        /// </summary>
        /// <param name="shopperReference">The unique shopper reference (usually a GUID to uniquely identify your shopper).</param>
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

        /// <inheritdoc/>
        public async Task<CreateCheckoutSessionResponse> CheckoutSessionsAsync(string shopperReference, CancellationToken cancellationToken)
        {
            var orderRef = Guid.NewGuid();

            var sessionsRequest = new CreateCheckoutSessionRequest();
            sessionsRequest.MerchantAccount = _merchantAccount;

            var amount = new Amount("EUR", 0); 
            sessionsRequest.Amount = amount;
            sessionsRequest.Reference = orderRef.ToString();

            sessionsRequest.Channel = CreateCheckoutSessionRequest.ChannelEnum.Web;
            sessionsRequest.ShopperInteraction = CreateCheckoutSessionRequest.ShopperInteractionEnum.Ecommerce;
            sessionsRequest.RecurringProcessingModel = CreateCheckoutSessionRequest.RecurringProcessingModelEnum.Subscription;
            sessionsRequest.EnableRecurring = true;

            sessionsRequest.ShopperReference = shopperReference;

            // required for 3ds2 redirect flow
            sessionsRequest.ReturnUrl = $"{_urlService.GetHostUrl()}/redirect?orderRef={orderRef}";

            try
            {
                var sessionResponse = await _checkout.SessionsAsync(sessionsRequest);
                _logger.LogInformation($"Response for Payments API::\n{sessionResponse}\n");
                return sessionResponse;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed::\n{e.ResponseBody}\n");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<PaymentResponse> MakePaymentAsync(string shopperReference, string recurringDetailReference, CancellationToken cancellationToken)
        {
            var amount = new Amount("USD", 10000);
            var details = new DefaultPaymentMethodDetails
            {
                StoredPaymentMethodId = recurringDetailReference
            };

            var paymentsRequest = new PaymentRequest
            {
                Reference = Guid.NewGuid().ToString(),
                Amount = amount,
                MerchantAccount = _merchantAccount,
                ShopperInteraction = PaymentRequest.ShopperInteractionEnum.ContAuth,
                RecurringProcessingModel = PaymentRequest.RecurringProcessingModelEnum.Subscription,
                ShopperReference = shopperReference,
                PaymentMethod = details
            };

            try
            {
                var paymentResponse = await _checkout.PaymentsAsync(paymentsRequest);
                _logger.LogInformation($"Response for Payments API::\n{paymentResponse}\n");
                return paymentResponse;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for Payments failed::\n{e.ResponseBody}\n");
                throw;
            }
        }

    }
}
