using Adyen.Model.Notification;
using Adyen.Util;
using adyen_dotnet_subscription_example.Options;
using adyen_dotnet_subscription_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

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
        public ActionResult<string> ReceiveWebhooksAsync(NotificationRequest notificationRequest)
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
                // Read more here: https://docs.adyen.com/development-resources/webhooks/verify-hmac-signatures & https://docs.adyen.com/development-resources/webhooks#accept-notifications
                if (!_hmacValidator.IsValidHmac(container.NotificationItem, _hmacKey))
                {
                    _logger.LogError($"Error while validating HMAC Key");
                    return BadRequest("[not accepted invalid hmac key]");
                }

                // Get the recurringDetailReference and shopperReference from the additionalData property in the webhook.
                // We store it, so that we can make payment requests on behalf of the shopper in the future.
                if (container.NotificationItem.AdditionalData.TryGetValue("recurring.recurringDetailReference", out string recurringDetailReference))
                {
                    if (container.NotificationItem.AdditionalData.TryGetValue("recurring.shopperReference", out string shopperReference))
                    {
                        _logger.LogInformation($"Received recurringDetailReference:: {recurringDetailReference} for {shopperReference}");
                        _repository.Upsert(container.NotificationItem.PaymentMethod, shopperReference, recurringDetailReference);
                    }
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