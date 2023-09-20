using Adyen.Model.Notification;
using Adyen.Util;
using adyen_dotnet_giving_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace adyen_dotnet_giving_example.Controllers
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
            // Process the payment (AUTHORISATION) webhook.
            _logger.LogInformation($"Webhook received::\n{notificationRequest.ToJson()}");

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

                // Process notification asynchronously.
                await ProcessNotificationAsync(container.NotificationItem);

                return Ok("[accepted]");
            }
            catch (Exception e)
            {
                _logger.LogError("Exception thrown: " + e.ToString());
                throw;
            }
        }


        private Task ProcessNotificationAsync(NotificationRequestItem notification)
        {
            // Regardless of a success or not, you'd probably want to update your backend/database or (preferably) send the event to a queue for further processing.

            if (!notification.Success)
            {
                // Perform your business logic here, process the success:false event to update your backend. We log it for now.
                _logger.LogInformation($"Webhook unsuccessful: {notification.Reason} \n" +
                    $"EventCode: {notification.EventCode} \n" +
                    $"Merchant Reference ::{notification.MerchantReference} \n" +
                    $"PSP Reference ::{notification.PspReference} \n");

                return Task.CompletedTask;
            }

            // Perform your business logic here, process the success:true event to update your backend. We log it for now.
            _logger.LogInformation($"Received successful Webhook with event::\n" +
                                   $"EventCode: {notification.EventCode} \n" +
                                   $"Merchant Reference ::{notification.MerchantReference} \n" +
                                   $"PSP Reference ::{notification.PspReference} \n");

            return Task.CompletedTask;
        }

        [HttpPost("api/webhooks/giving")]
        public async Task<ActionResult<string>> GivingWebhooks(NotificationRequest notificationRequest)
        {
            /// You need to enable the "DONATION" (eventCode) webhook: https://docs.adyen.com/online-payments/donations/web-component/#get-the-donation-outcome.
            /// Use the originalReference to associate the donation to the shopper's original transaction.
            /// See other eventCodes for webhooks here: https://docs.adyen.com/development-resources/webhooks/webhook-types/#other-webhooks.
            _logger.LogInformation($"Giving Webhook received::\n{notificationRequest.ToJson()}");

            try
            {
                // JSON and HTTP POST notifications always contain a single `NotificationRequestItem` object.
                // Read more: https://docs.adyen.com/development-resources/webhooks/understand-notifications#notification-structure.
                NotificationRequestItemContainer container = notificationRequest.NotificationItemContainers?.FirstOrDefault();

                if (container == null)
                {
                    return BadRequest("Container has no notification items.");
                }

                // Process notification asynchronously.
                await ProcessGivingNotificationAsync(container.NotificationItem);

                return Ok("[accepted]");
            }
            catch (Exception e)
            {
                _logger.LogError("Exception thrown: " + e.ToString());
                throw;
            }
        }

        private Task ProcessGivingNotificationAsync(NotificationRequestItem notification)
        {
            // Regardless of a success or not, you'd probably want to update your backend/database or (preferably) send the event to a queue for further processing.

            if (!notification.Success)
            {
                // Perform your business logic here, process the success:false event to update your backend. We log it for now.
                _logger.LogInformation($"Giving Webhook unsuccessful: {notification.Reason} \n" +
                    $"EventCode: {notification.EventCode} \n" +
                    $"Merchant Reference ::{notification.MerchantReference} \n" +
                    $"PSP Reference ::{notification.PspReference} \n");

                return Task.CompletedTask;
            }

            // Perform your business logic here, process the success:true event to update your backend. We log it for now.
            _logger.LogInformation($"Received successful Giving Webhook with event::\n" +
                                   $"EventCode: {notification.EventCode} \n" +
                                   $"Merchant Reference ::{notification.MerchantReference} \n" +
                                   $"PSP Reference ::{notification.PspReference} \n");

            return Task.CompletedTask;
        }

    }
}