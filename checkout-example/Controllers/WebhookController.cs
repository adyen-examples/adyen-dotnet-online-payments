using Adyen.Model.Notification;
using Adyen.Util;
using adyen_dotnet_checkout_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace adyen_dotnet_checkout_example.Controllers
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
        public ActionResult<string> Webhooks(NotificationRequest notificationRequest)
        {
            _logger.LogInformation($"Webhook received::\n{notificationRequest}\n");

            try
            {
                // JSON and HTTP POST notifications always contain a single `NotificationRequestItem` object.
                // Read more: https://docs.adyen.com/development-resources/webhooks/understand-notifications#notification-structure.
                NotificationRequestItemContainer container = notificationRequest.NotificationItemContainers.FirstOrDefault();

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

                _logger.LogInformation($"Received webhook with event::\n" +
                    $"Merchant Reference ::{container.NotificationItem.MerchantReference} \n" +
                    $"PSP Reference ::{container.NotificationItem.PspReference} \n"
                );
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while calculating HMAC signature::\n{e}\n");
                throw;
            }

            return Ok("[accepted]");
        }
    }
}