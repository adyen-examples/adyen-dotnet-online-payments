using Adyen.Model.Notification;
using Adyen.Service;
using Adyen.Util;
using adyen_dotnet_paybylink_example.Options;
using adyen_dotnet_paybylink_example.Repositories;
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
        private readonly IPaymentLinkRepository _paymentLinkRepository;
        private readonly ILogger<WebhookController> _logger;
        private readonly HmacValidator _hmacValidator;
        private readonly string _hmacKey;
        
        public WebhookController(IPaymentLinkRepository paymentLinkRepository, ILogger<WebhookController> logger, IOptions<AdyenOptions> options, HmacValidator hmacValidator)
        {
            _logger = logger;
            _paymentLinkRepository = paymentLinkRepository;
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
            // For every payment paid using the Pay By Link, Adyen will send an AUTHORISATION webhook.
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
            // In this case, we get the PaymentLinkId from the AdditionalData.
            if (!notification.AdditionalData.TryGetValue("paymentLinkId", out string paymentLinkId))
            {
                return Task.CompletedTask;
            }

            // Perform your business logic (e.g. insert into a message broker), we simply log it for now.
            _logger.LogInformation($"[AUTHORISATION]\n" +
                $"PaymentLinkId: {paymentLinkId}\n" +
                $"Payment method: {notification.PaymentMethod}\n" +
                $"Currency: {notification.Amount?.Currency}\n" +
                $"Value: {notification.Amount?.Value}\n" +
                $"PspReference: {notification.PspReference}");

            return Task.CompletedTask;
        }
    }
}