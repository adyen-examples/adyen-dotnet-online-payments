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
            _logger.LogInformation($"Webhook received::\n{notificationRequest}\n");

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

                // Return if webhook is not successful.
                if (!container.NotificationItem.Success)
                {
                    _logger.LogError($"Webhook unsuccessful: {container.NotificationItem.Reason} \n" +
                        $"EventCode: {container.NotificationItem.EventCode}");
                    return BadRequest($"Webhook unsuccessful: {container.NotificationItem.Reason}");
                }

                // Process notifications asynchronously.
                await ProcessAuthorisationNotificationAsync(container.NotificationItem);

                await ProcessOrderOpenedNotificationAsync(container.NotificationItem);

                await ProcessOrderClosedNotificationAsync(container.NotificationItem);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while calculating HMAC signature::\n{e}\n");
                throw;
            }

            return Ok("[accepted]");
        }

        private Task ProcessAuthorisationNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode == "AUTHORISATION")
            {
                return Task.CompletedTask;
            }

            // The amount that is authorised on the final payment. E.g. if you paid €110,- with a €50,- giftcard and another €50,- giftcard.
            // This amount should be `1000` (in units of 100s) which is equivalent to €10,-
            _logger.LogInformation($"Payment method: {notification.PaymentMethod}\n" +
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

            _logger.LogInformation($"MerchantOrderReference: {notification.MerchantReference}\n" +
                $"Currency: {notification.Amount.Currency}\n" +
                $"Value: {notification.Amount.Value}\n" + // Total order amount `11000` (in units of 100s) which is equivalent to €110,-
                $"PspReference: {notification.PspReference}");

            return Task.CompletedTask;
        }

        private Task ProcessOrderClosedNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode != "ORDER_CLOSED")
            {
                return Task.CompletedTask;
            }


            bool isReading = true;
            for (int i = 1; i < 10 && isReading; i++)
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

                _logger.LogInformation($"orderPaymentMethod: {orderPaymentMethod}\n" + // The payment method that is used to make the purchase (e.g. 'visa').
                    $"orderPspReference: {orderPspReference}\n" + // Foreach partial payment, you get a new (unique) PspReference.
                    $"orderPaymentAmount: {orderPaymentAmount}"); // Format: 'EUR 50.00'.
            }


            _logger.LogInformation($"MerchantOrderReference: {notification.MerchantReference}\n" +
                $"Currency: {notification.Amount.Currency}\n" +
                $"Value: {notification.Amount.Value}\n" + // Total order amount `11000` (in units of 100s) which is equivalent to €110,-
                $"PspReference: {notification.PspReference}"); 

            return Task.CompletedTask;
        }
    }
}