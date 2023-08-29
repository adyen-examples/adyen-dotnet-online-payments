using Adyen.Model.Notification;
using Adyen.Util;
using adyen_dotnet_in_person_payments_example.Models;
using adyen_dotnet_in_person_payments_example.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using adyen_dotnet_in_person_payments_example.Services;

namespace adyen_dotnet_in_person_payments_example.Controllers
{
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly ITableService _tableService;
        private readonly HmacValidator _hmacValidator;
        private readonly string _hmacKey;

        public WebhookController(ILogger<WebhookController> logger, IOptions<AdyenOptions> options, ITableService tableService, HmacValidator hmacValidator)
        {
            _logger = logger;
            _tableService = tableService;
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

                // Process CANCEL_OR_REFUND notification asynchronously (also known as: `reversals`).
                // Documentation: https://docs.adyen.com/point-of-sale/basic-tapi-integration/refund-payment/refund-webhooks/#cancel-or-refund-webhook
                await ProcessCancelOrRefundNotificationAsync(container.NotificationItem);

                // Process REFUND_FAILED notification asynchronously.
                // Documentation: https://docs.adyen.com/online-payments/refund#refund-failed
                // Testing this scenario: https://docs.adyen.com/online-payments/refund#testing-failed-refunds
                await ProcessRefundFailedNotificationAsync(container.NotificationItem);

                // Process REFUNDED_REVERSED notification asynchronously.
                // Documentation: https://docs.adyen.com/online-payments/refund#refunded-reversed
                await ProcessRefundedReversedNotificationAsync(container.NotificationItem);

                return Ok("[accepted]");
            }
            catch (Exception e)
            {
                _logger.LogError("Exception thrown: " + e.ToString());
                throw;
            }
        }


        // Webhook: https://docs.adyen.com/online-payments/reversal/#cancel-or-refund-webhook
        // >> True: Adyen's validations were successful and we sent the refund request to the card scheme.
        // This usually means that the refund will be processed successfully. However, in rare cases the refund can be rejected by the card scheme, or reversed.
        // For information about these exceptions, see REFUND_FAILED webhook and REFUNDED_REVERSED webhook.
        // >> False: the refund validations failed.
        // The webhook includes a reason field with a short description of the problem. 
        // See refusal reasons: https://docs.adyen.com/point-of-sale/basic-tapi-integration/refund-payment/refund-webhooks/#cancel-or-refund-webhook
        private Task ProcessCancelOrRefundNotificationAsync(NotificationRequestItem notification)
        {
            if (!notification.Success)
            {
                // Perform your business logic here, you would probably want to process the success:false event to update your backend. We log it for now.
                _logger.LogInformation($"Webhook unsuccessful: {notification.Reason} \n" +
                    $"EventCode: {notification.EventCode} \n" +
                    $"Merchant Reference ::{notification.MerchantReference} \n" +
                    $"Original Reference ::{notification.OriginalReference} \n" +
                    $"PSP Reference ::{notification.PspReference} \n");

                return Task.CompletedTask;
            }

            if (notification.EventCode != "CANCEL_OR_REFUND")
            {
                return Task.CompletedTask;
            }

            if (notification.AdditionalData.TryGetValue("bookingDate", out string dateTime))
            {
                _logger.LogInformation("BookingDate: " + dateTime);
            }

            TableModel table = _tableService.Tables.FirstOrDefault(t => t.SaleReferenceId == notification.MerchantReference);
            
            if (table == null)
            {
                return Task.CompletedTask;
            }

            if (!notification.Success)
            {
                table.TableStatus = TableStatus.Refunded;
            }
            else
            {
                table.TableStatus = TableStatus.RefundFailed;
            }

            _logger.LogInformation($"Received {(notification.Success ? "successful" : "unsuccessful")} {notification.EventCode} webhook::\n" +
                                   $"Merchant Reference ::{notification.MerchantReference} \n" +
                                   $"PSP Reference ::{notification.PspReference} \n" +
                                   $"Original Reference ::{notification.OriginalReference} \n");

            return Task.CompletedTask;
        }

        private Task ProcessRefundFailedNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode != "REFUND_FAILED")
            {
                return Task.CompletedTask;
            }

            if (notification.AdditionalData.TryGetValue("bookingDate", out string dateTime))
            {
                _logger.LogInformation("BookingDate: " + dateTime);
            }

            TableModel table = _tableService.Tables.FirstOrDefault(t => t.SaleReferenceId == notification.MerchantReference);
            
            if (table == null)
            {
                return Task.CompletedTask;
            }

            if (notification.Success)
            {
                table.TableStatus = TableStatus.RefundFailed;
            }

            _logger.LogInformation($"Received {(notification.Success ? "successful" : "unsuccessful")} {notification.EventCode} webhook::\n" +
                                   $"Merchant Reference ::{notification.MerchantReference} \n" +
                                   $"PSP Reference ::{notification.PspReference} \n" +
                                   $"Original Reference ::{notification.OriginalReference} \n");

            return Task.CompletedTask;
        }


        private Task ProcessRefundedReversedNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode != "REFUNDED_REVERSED")
            {
                return Task.CompletedTask;
            }

            if (notification.AdditionalData.TryGetValue("bookingDate", out string dateTime))
            {
                _logger.LogInformation("BookingDate: " + dateTime);
            }

            TableModel table = _tableService.Tables.FirstOrDefault(t => t.SaleReferenceId == notification.MerchantReference);
            
            if (table == null)
            {
                return Task.CompletedTask;
            }

            if (notification.Success)
            {
                table.TableStatus = TableStatus.RefundedReversed;
            }

            _logger.LogInformation($"Received {(notification.Success ? "successful" : "unsuccessful")} {notification.EventCode} webhook::\n" +
                                   $"Merchant Reference ::{notification.MerchantReference} \n" +
                                   $"PSP Reference ::{notification.PspReference} \n" +
                                   $"Original Reference ::{notification.OriginalReference} \n");

            return Task.CompletedTask;
        }
    }
}