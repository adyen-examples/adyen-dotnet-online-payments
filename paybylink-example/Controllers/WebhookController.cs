using Adyen.Model.Notification;
using Adyen.Util;
using adyen_dotnet_paybylink_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace adyen_dotnet_paybylink_example.Controllers
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
                    return Ok("[accepted]"); // The webhook was delivered (but was unsuccessful), hence why we'll return a [accepted] response to confirm that we've received it.
                }

                // Process notifications asynchronously.
                await ProcessAuthorisationNotificationAsync(container.NotificationItem);
            }
            catch (Exception e)
            {
                _logger.LogError("Exception thrown: " + e.ToString());
                throw;
            }

            return Ok("[accepted]");
        }

        private Task ProcessAuthorisationNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode != "AUTHORISATION")
            {
                return Task.CompletedTask;
            }

            // The amount that is authorised on the final payment. For example: if you paid EUR 110 with a EUR 50 giftcard and another EUR 50 giftcard.
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
    }
}