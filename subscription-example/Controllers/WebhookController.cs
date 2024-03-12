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
        public async Task<ActionResult<string>> Webhooks(NotificationRequest notificationRequest)
        {
            _logger.LogInformation($"Webhook received::\n{notificationRequest.ToJson()}");

            try
            {
                // JSON and HTTP POST notifications always contain a single `NotificationRequestItem` object.
                // Read more: https://docs.adyen.com/development-resources/webhooks/understand-notifications#notification-structure.
                NotificationRequestItemContainer container = notificationRequest.NotificationItemContainers?.FirstOrDefault();

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

                // Process notifications asynchronously.
                await ProcessAuthorisationNotificationAsync(container.NotificationItem);

                await ProcessRecurringContractNotificationAsync(container.NotificationItem);

                // Return a 202 status with an empty response body
                return Accepted();
            }
            catch (Exception e)
            {
                _logger.LogError("Exception thrown: " + e.ToString());
                throw;
            }
        }

        private Task ProcessAuthorisationNotificationAsync(NotificationRequestItem notification)
        {
            // Read more here: https://docs.adyen.com/online-payments/tokenization/create-and-use-tokens?tab=subscriptions_2#pending-and-refusal-result-codes-1
            if (notification.EventCode != "AUTHORISATION")
            {
                return Task.CompletedTask;
            }

            // Perform your business logic here for the success:false scenario. 
            if (!notification.Success)
            {
                // We just log it for now. You would probably want to update your backend or send this the message to a queue.
                _logger.LogInformation($"Webhook unsuccessful: {notification.Reason} \n" +
                    $"EventCode: {notification.EventCode} \n" +
                    $"Merchant Reference ::{notification.MerchantReference} \n" +
                    $"PSP Reference ::{notification.PspReference} \n");

                return Task.CompletedTask;
            }

            _logger.LogInformation($"Received webhook with event: \n" +
                $"EventCode: {notification.EventCode} \n" +
                $"Merchant Reference: {notification.MerchantReference} \n" +
                $"PSP Reference: {notification.PspReference} \n");
            return Task.CompletedTask;
        }


        private Task ProcessRecurringContractNotificationAsync(NotificationRequestItem notification)
        {
            // Read more about EventCode "RECURRING_CONTRACT" here: https://docs.adyen.com/online-payments/tokenization/create-and-use-tokens?tab=subscriptions_2#pending-and-refusal-result-codes-1.
            if (notification.EventCode != "RECURRING_CONTRACT")
            {
                return Task.CompletedTask;
            }

            // Perform your business logic here for the success:false scenario.
            if (!notification.Success)
            {
                // We just log it for now. You would probably want to update your backend or send this the message to a queue.
                _logger.LogInformation($"Webhook unsuccessful: {notification.Reason} \n" +
                    $"EventCode: {notification.EventCode} \n" +
                    $"Merchant Reference ::{notification.MerchantReference} \n" +
                    $"PSP Reference ::{notification.PspReference} \n");

                return Task.CompletedTask;
            }

            // Perform your business logic here for the success:true scenario.
            // Get the `recurringDetailReference` from the `AdditionalData` property in the webhook.
            if (!notification.AdditionalData.TryGetValue("recurring.recurringDetailReference", out string recurringDetailReference))
            {
                return Task.CompletedTask;
            }

            // Get the `shopperReference` from the `AdditionalData` property in the webhook.
            if (!notification.AdditionalData.TryGetValue("recurring.shopperReference", out string shopperReference))
            {
                return Task.CompletedTask;
            }

            // Get and log the recurringProcessingModel below.
            if (notification.AdditionalData.TryGetValue("recurringProcessingModel", out string recurringProcessingModel))
            {
                _logger.LogInformation($"EventCode: {notification.EventCode} \nReceived recurringDetailReference: {recurringDetailReference} for {shopperReference} \n" +
                    $"RecurringProcessingModel: {recurringProcessingModel}");
            }

            // Save the paymentMethod, shopperReference and recurringDetailReference in our in-memory cache.
            _repository.Upsert(notification.PaymentMethod, shopperReference, recurringDetailReference);
            
            _logger.LogInformation($"Received webhook with event: \n" +
                $"EventCode: {notification.EventCode} \n" +
                $"Merchant Reference: {notification.MerchantReference} \n" +
                $"PSP Reference: {notification.PspReference} \n");
            return Task.CompletedTask;
        }
    }
}
