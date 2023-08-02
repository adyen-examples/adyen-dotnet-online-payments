using Adyen.Model.Notification;
using Adyen.Util;
using adyen_dotnet_giftcard_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace adyen_dotnet_giftcard_example.Controllers
{
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly HmacValidator _hmacValidator;
        private readonly string _hmacKey;
        
        public WebhookController(ILogger<WebhookController> logger, IOptions<AdyenOptions> options, HmacValidator hmacValidator)
        {
            _logger = logger;
            _hmacKey = options.Value.ADYEN_HMAC_KEY;
            _hmacValidator = hmacValidator;
        }

        [HttpPost("api/webhooks/notifications")]
        public async Task<ActionResult<string>> Webhooks(NotificationRequest notificationRequest)
        {
            _logger.LogInformation($"Webhook received::\n{notificationRequest.ToJson()}\n");

            try
            {
                // JSON and HTTP POST notifications always contain a single `NotificationRequestItem` object.
                // Read more: https://docs.adyen.com/development-resources/webhooks/understand-notifications#notification-structure.
                NotificationRequestItemContainer container = notificationRequest.NotificationItemContainers?.FirstOrDefault();

                if (container == null)
                {
                    return BadRequest("Container has no notification items.");
                }

                // We always recommend to activate HMAC validation in the webhooks for security reasons.
                // Read more here: https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures & https://docs.adyen.com/development-resources/webhooks#accept-notifications.
                if (!_hmacValidator.IsValidHmac(container.NotificationItem, _hmacKey))
                {
                    _logger.LogError($"Error while validating HMAC Key");
                    return BadRequest("[not accepted invalid hmac key]");
                }

                // Process notifications asynchronously.
                await ProcessAuthorisationNotificationAsync(container.NotificationItem);

                await ProcessOrderOpenedNotificationAsync(container.NotificationItem);

                await ProcessOrderClosedNotificationAsync(container.NotificationItem);

                return Ok("[accepted]");
            }
            catch (Exception e)
            {
                _logger.LogError("Exception thrown: " + e.ToString());
                throw;
            }
        }

        private Task ProcessAuthorisationNotificationAsync(NotificationRequestItem notification)
        {
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

            // Perform your business logic here for the success:true scenario.

            // The amount that is authorised on the final payment. For example: if you paid EUR 110 with a EUR 50 giftcard and another EUR 50 gift card.
            // This amount should be `1000` (in units of 100s) which is equivalent to EUR 10.
            _logger.LogInformation($"[AUTHORISATION]\n" +
                $"Payment method: {notification.PaymentMethod}\n" +
                $"Currency: {notification.Amount?.Currency}\n" +
                $"Value: {notification.Amount?.Value}\n" +
                $"PspReference: {notification.PspReference}");

            if (notification.AdditionalData.TryGetValue("merchantOrderReference", out string merchantOrderReference))
            {
                // This is used as reference for "ORDER_OPENED" / "ORDER_CLOSED".
                _logger.LogInformation($"merchantOrderReference: {merchantOrderReference}");
            }

            return Task.CompletedTask;
        }

        private Task ProcessOrderOpenedNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode != "ORDER_OPENED")
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

            // Perform your business logic here for the success:true scenario. In this case, we do some logging.
            _logger.LogInformation($"[ORDER_OPENED]\n" +
                $"merchantOrderReference: {notification.MerchantReference}\n" +
                $"Currency: {notification.Amount.Currency}\n" +
                $"Value: {notification.Amount.Value}\n" + // Total order amount `11000` (in units of 100s) which is equivalent to EUR 110.
                $"PspReference: {notification.PspReference}");

            return Task.CompletedTask;
        }

        private Task ProcessOrderClosedNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode != "ORDER_CLOSED")
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
            // We loop over every partial order: `order-1-...` -> `order-2-...` -> `order-3-...` until there are no more partial orders, we stop if we don't find the sequential number for `i`.
            bool isReading = true;
            for (int i = 1; i < notification.AdditionalData.Count && isReading; i++)
            {
                if (!notification.AdditionalData.TryGetValue($"order-{i}-paymentMethod", out string orderPaymentMethod))
                {
                    isReading = false;
                    continue;
                }

                
                if (!notification.AdditionalData.TryGetValue($"order-{i}-pspReference", out string orderPspReference))
                {
                    isReading = false;
                    continue;
                }

                
                if (!notification.AdditionalData.TryGetValue($"order-{i}-paymentAmount", out string orderPaymentAmount))
                {
                    isReading = false;
                    continue;
                }

                _logger.LogInformation($"[ORDER_CLOSED]\n" +
                    $"orderPaymentMethod: {orderPaymentMethod}\n" + // The payment method that is used to make the purchase (e.g. 'visa').
                    $"orderPspReference: {orderPspReference}\n" + // Foreach partial payment, you get a new (unique) PspReference.
                    $"orderPaymentAmount: {orderPaymentAmount}"); // Format: 'EUR 50.00'.
            }

            _logger.LogInformation($"MerchantOrderReference: {notification.MerchantReference}\n" +
                $"Currency: {notification.Amount.Currency}\n" +
                $"Value: {notification.Amount.Value}\n" + // Total order amount `11000` (in units of 100s) which is equivalent to EUR 110.
                $"PspReference: {notification.PspReference}"); 

            return Task.CompletedTask;
        }
    }
}