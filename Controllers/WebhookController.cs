using Adyen.Model.Notification;
using Adyen.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace adyen_dotnet_online_payments.Controllers
{
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly string _hmac_key;
        private readonly ILogger<WebhookController> _logger;
        public WebhookController(ILogger<WebhookController> logger)
        {
            _logger = logger;
            _hmac_key = Environment.GetEnvironmentVariable("ADYEN_HMAC_KEY");
        }

        [HttpPost("api/webhooks/notifications")]
        public ActionResult<string> Webhooks(NotificationRequest notificationRequest)
        {
            var hmacValidator = new HmacValidator();

            _logger.LogInformation($"Webhook received::\n{notificationRequest}\n");

            foreach(NotificationRequestItemContainer container in notificationRequest.NotificationItemContainers)
            {
                // We recommend to activate HMAC validation in the webhooks for security reasons
                try
                {
                    if (hmacValidator.IsValidHmac(container.NotificationItem, _hmac_key))
                    {
                        _logger.LogInformation($"Received webhook with event::\n" +
                            $"Merchant Reference ::{container.NotificationItem.MerchantReference} \n" +
                            $"PSP Reference ::{container.NotificationItem.PspReference} \n"
                        );
                    }
                    else
                    {
                        _logger.LogError($"Error while validating HMAC Key");
                        return BadRequest("[not accepted invalid hmac key]");
                    }
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