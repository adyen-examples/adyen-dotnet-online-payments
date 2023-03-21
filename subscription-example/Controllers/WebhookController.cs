using Adyen.Model.Notification;
using Adyen.Util;
using adyen_dotnet_subscription_example.Options;
using adyen_dotnet_subscription_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly ISubscriptionRepository _repository;
        private readonly HmacValidator _hmacValidator;
        private readonly string _hmacKey;

        public WebhookController(ILogger<WebhookController> logger, IOptions<AdyenOptions> options, ISubscriptionRepository repository, HmacValidator hmacValidator)
        {
            _logger = logger;
            _repository = repository;
            _hmacValidator = hmacValidator;
            _hmacKey = options.Value.ADYEN_HMAC_KEY;
        }

        [HttpPost("api/webhooks/notifications")]
        public async Task<ActionResult<string>> ReceiveWebhooksAsync(NotificationRequest notificationRequest)
        {
            _logger.LogInformation($"Webhook received: \n{notificationRequest}\n");

            try
            {
                // JSON and HTTP POST notifications always contain a single `NotificationRequestItem` object.
                // Read more: https://docs.adyen.com/development-resources/webhooks/understand-notifications#notification-structure.
                NotificationRequestItemContainer container = notificationRequest.NotificationItemContainers.FirstOrDefault();

                if (container == null)
                {
                    return BadRequest("Container has no notification items");
                }

                // We always recommend to activate HMAC validation in the webhooks for security reasons.
                // Read more here: https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures & https://docs.adyen.com/development-resources/webhooks#accept-notifications
                if (!_hmacValidator.IsValidHmac(container.NotificationItem, _hmacKey))
                {
                    _logger.LogError($"Error while validating HMAC Key");
                    return BadRequest("[not accepted invalid hmac key]");
                }

                // Process notification asynchronously.
                await ProcessNotificationAsync(container.NotificationItem);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while calculating HMAC signature: \n{e}\n");
                throw;
            }

            return Ok("[accepted]");
        }

        private Task ProcessNotificationAsync(NotificationRequestItem notificationRequestItem)
        {
            // Return if not success.
            if (!notificationRequestItem.Success)
            {
                _logger.LogError($"Webhook unsuccessful: {notificationRequestItem.Reason}");
                return Task.CompletedTask;
            }

            // Get the `recurringDetailReference` from the `AdditionalData` property in the webhook.
            if (!notificationRequestItem.AdditionalData.TryGetValue("recurring.recurringDetailReference", out string recurringDetailReference))
            {
                return Task.CompletedTask;
            }

            // Get the `shopperReference` from the `AdditionalData` property in the webhook.
            if (!notificationRequestItem.AdditionalData.TryGetValue("recurring.shopperReference", out string shopperReference))
            {
                return Task.CompletedTask;
            }

            // Get and log the recurringProcessingModel below
            notificationRequestItem.AdditionalData.TryGetValue("recurringProcessingModel", out string recurringProcessingModel);

            _logger.LogInformation($"Received recurringDetailReference: {recurringDetailReference} for {shopperReference}" +
                $"RecurringProcessingModel: {recurringProcessingModel}");

            // Save the paymentMethod, shopperReference and recurringDetailReference in our in-memory cache.
            _repository.Upsert(notificationRequestItem.PaymentMethod, shopperReference, recurringDetailReference);
            
            _logger.LogInformation($"Received webhook with event: \n" +
                                   $"Merchant Reference: {notificationRequestItem.MerchantReference} \n" +
                                   $"PSP Reference: {notificationRequestItem.PspReference} \n");
            return Task.CompletedTask;
        }
    }
}