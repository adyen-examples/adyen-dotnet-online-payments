using Adyen.Model.Notification;
using Adyen.Util;
using adyen_dotnet_checkout_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace adyen_dotnet_checkout_example.Controllers
{
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly string _hmacKey;
        
        public WebhookController(ILogger<WebhookController> logger, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _hmacKey = options.Value.ADYEN_HMAC_KEY;
        }

        [HttpPost("api/webhooks/notifications")]
        public async Task<ActionResult<string>> Webhooks(NotificationRequest notificationRequest)
        {
            var hmacValidator = new HmacValidator();

            _logger.LogInformation($"Webhook received::\n{notificationRequest}\n");

            foreach(NotificationRequestItemContainer container in notificationRequest.NotificationItemContainers)
            {
                // We always recommend to activate HMAC validation in the webhooks for security reasons.
                // Read more here: https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures.
                try
                {
                    if (!hmacValidator.IsValidHmac(container.NotificationItem, _hmacKey))
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
            }

            return Ok("[accepted]");
        }
    }
}