using Adyen.Model.Nexo;
using Adyen.Model.Notification;
using Adyen.Util;
using adyen_dotnet_authorisation_adjustment_example.Models;
using adyen_dotnet_authorisation_adjustment_example.Options;
using adyen_dotnet_authorisation_adjustment_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace adyen_dotnet_authorisation_adjustment_example.Controllers
{
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly IHotelPaymentRepository _repository;
        private readonly HmacValidator _hmacValidator;
        private readonly string _hmacKey;

        public WebhookController(ILogger<WebhookController> logger, IOptions<AdyenOptions> options, IHotelPaymentRepository repository, HmacValidator hmacValidator)
        {
            _logger = logger;
            _repository = repository;
            _hmacValidator = hmacValidator;
            _hmacKey = options.Value.ADYEN_HMAC_KEY;
        }

        [HttpPost("api/webhooks/notifications")]
        public async Task<ActionResult<string>> Webhooks(NotificationRequest notificationRequest)
        {
            _logger.LogInformation($"Webhook received: \n{notificationRequest}\n");

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

                // Process AUTHORISATION notification asynchronously.
                // Read more about authorisation here: https://docs.adyen.com/get-started-with-adyen/payment-glossary#authorisation
                await ProcessAuthorisationNotificationAsync(container.NotificationItem);

                // Process AUTHORISATION_ADJUSTMENT notification asynchronously.
                // Documentation: https://docs.adyen.com/online-payments/adjust-authorisation#adjust-authorisation
                await ProcessAuthorisationAdjustmentNotificationAsync(container.NotificationItem);

                // Process CAPTURE notification asynchronously.
                // Documentation: https://docs.adyen.com/online-payments/capture
                await ProcessCaptureNotificationAsync(container.NotificationItem);

                // Process CAPTURE_FAILED notification asynchronously. 
                // Testing this scenario: https://docs.adyen.com/online-payments/classic-integrations/modify-payments/capture#testing-failed-captures
                // Failure reasons: https://docs.adyen.com/online-payments/capture/failure-reasons
                await ProcessCaptureFailedNotificationAsync(container.NotificationItem);

                // Process CANCEL_OR_REFUND notification asynchronously (also known as: `reversals`).
                // Documentation: https://docs.adyen.com/online-payments/reversal
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

        private Task ProcessAuthorisationNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode != "AUTHORISATION")
            {
                return Task.CompletedTask;
            }

            var hotelPayment = new HotelPaymentModel()
            {
                PspReference = notification.PspReference,
                OriginalReference = notification.OriginalReference,
                Reference = _repository.FindReferenceByPspReference(notification.PspReference),
                Amount = notification.Amount?.Value,
                Currency = notification.Amount?.Currency,
                DateTime = DateTimeOffset.Parse(notification.EventDate),
                ResultCode = notification.EventCode,
                RefusalReason = notification.Success ? null : notification.Reason,
                PaymentMethodBrand = notification.PaymentMethod
            };

            if (!_repository.Insert(hotelPayment))
            {
                _logger.LogInformation($"Could not insert {notification.PspReference}");
                return Task.CompletedTask;
            }

            _logger.LogInformation($"Received {(notification.Success ? "successful" : "unsuccessful")} {notification.EventCode} webhook::\n" +
                                   $"Merchant Reference ::{notification.MerchantReference} \n" +
                                   $"PSP Reference ::{notification.PspReference} \n");

            return Task.CompletedTask;
        }

        private Task ProcessAuthorisationAdjustmentNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode != "AUTHORISATION_ADJUSTMENT")
            {
                return Task.CompletedTask;
            }

            notification.AdditionalData.TryGetValue("bookingDate", out string dateTime);

            var hotelPayment = new HotelPaymentModel()
            {
                PspReference = notification.PspReference,
                OriginalReference = notification.OriginalReference,
                Reference = _repository.FindReferenceByPspReference(notification.OriginalReference),
                Amount = notification.Amount?.Value,
                Currency = notification.Amount?.Currency,
                DateTime = dateTime == null ? DateTimeOffset.Parse(notification.EventDate) : DateTimeOffset.Parse(dateTime),
                ResultCode = notification.EventCode,
                RefusalReason = notification.Success ? null : notification.Reason,
                PaymentMethodBrand = notification.PaymentMethod
            };

            if (!_repository.Insert(hotelPayment))
            {
                _logger.LogInformation($"Could not insert {notification.PspReference}");
                return Task.CompletedTask;
            }

            _logger.LogInformation($"Received {(notification.Success ? "successful" : "unsuccessful")} {notification.EventCode} webhook::\n" +
                                   $"Merchant Reference ::{notification.MerchantReference} \n" +
                                   $"PSP Reference ::{notification.PspReference} \n" +
                                   $"Original Reference ::{notification.OriginalReference} \n");

            return Task.CompletedTask;
        }

        private Task ProcessCaptureNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode != "CAPTURE")
            {
                return Task.CompletedTask;
            }

            var hotelPayment = new HotelPaymentModel()
            {
                PspReference = notification.PspReference,
                OriginalReference = notification.OriginalReference,
                Reference = _repository.FindReferenceByPspReference(notification.OriginalReference),
                Amount = notification.Amount?.Value,
                Currency = notification.Amount?.Currency,
                DateTime = DateTimeOffset.Parse(notification.EventDate),
                ResultCode = notification.EventCode,
                RefusalReason = notification.Success ? null : notification.Reason,
                PaymentMethodBrand = notification.PaymentMethod
            };

            if (!_repository.Insert(hotelPayment))
            {
                _logger.LogInformation($"Could not insert {notification.PspReference}");
                return Task.CompletedTask;
            }

            _logger.LogInformation($"Received {(notification.Success ? "successful" : "unsuccessful")} {notification.EventCode} webhook::\n" +
                                   $"Merchant Reference ::{notification.MerchantReference} \n" +
                                   $"PSP Reference ::{notification.PspReference} \n" +
                                   $"Original Reference ::{notification.OriginalReference} \n");

            return Task.CompletedTask;
        }

        private Task ProcessCaptureFailedNotificationAsync(NotificationRequestItem notification)
        {
            if (notification.EventCode != "CAPTURE_FAILED")
            {
                return Task.CompletedTask;
            }

            var hotelPayment = new HotelPaymentModel()
            {
                PspReference = notification.PspReference,
                OriginalReference = notification.OriginalReference,
                Reference = _repository.FindReferenceByPspReference(notification.OriginalReference),
                Amount = notification.Amount?.Value,
                Currency = notification.Amount?.Currency,
                DateTime = DateTimeOffset.Parse(notification.EventDate),
                ResultCode = notification.EventCode,
                RefusalReason = notification.Reason,
                PaymentMethodBrand = notification.PaymentMethod
            };

            if (!_repository.Insert(hotelPayment))
            {
                _logger.LogInformation($"Could not insert {notification.PspReference}");
                return Task.CompletedTask;
            }

            _logger.LogInformation($"Received {(notification.Success ? "successful" : "unsuccessful")} {notification.EventCode} webhook::\n" +
                                   $"Merchant Reference ::{notification.MerchantReference} \n" +
                                   $"PSP Reference ::{notification.PspReference} \n" +
                                   $"Original Reference ::{notification.OriginalReference} \n");

            return Task.CompletedTask;
        }

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

            var hotelPayment = new HotelPaymentModel()
            {
                PspReference = notification.PspReference,
                OriginalReference = notification.OriginalReference,
                Reference = _repository.FindReferenceByPspReference(notification.OriginalReference),
                Amount = notification.Amount?.Value,
                Currency = notification.Amount?.Currency,
                DateTime = DateTimeOffset.Parse(notification.EventDate),
                ResultCode = notification.EventCode,
                RefusalReason = notification.Success ? null : notification.Reason,
                PaymentMethodBrand = notification.PaymentMethod
            };

            if (!_repository.Insert(hotelPayment))
            {
                _logger.LogInformation($"Could not insert {notification.PspReference}");
                return Task.CompletedTask;
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

            var hotelPayment = new HotelPaymentModel()
            {
                PspReference = notification.PspReference,
                OriginalReference = notification.OriginalReference,
                Reference = _repository.FindReferenceByPspReference(notification.OriginalReference),
                Amount = notification.Amount?.Value,
                Currency = notification.Amount?.Currency,
                DateTime = DateTimeOffset.Parse(notification.EventDate),
                ResultCode = notification.EventCode,
                RefusalReason = notification.Success ? null : notification.Reason,
                PaymentMethodBrand = notification.PaymentMethod
            };

            if (!_repository.Insert(hotelPayment))
            {
                _logger.LogInformation($"Could not insert {notification.PspReference}");
                return Task.CompletedTask;
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

            var hotelPayment = new HotelPaymentModel()
            {
                PspReference = notification.PspReference,
                OriginalReference = notification.OriginalReference,
                Reference = _repository.FindReferenceByPspReference(notification.OriginalReference),
                Amount = notification.Amount?.Value,
                Currency = notification.Amount?.Currency,
                DateTime = DateTimeOffset.Parse(notification.EventDate),
                ResultCode = notification.EventCode,
                RefusalReason = notification.Success ? null : notification.Reason,
                PaymentMethodBrand = notification.PaymentMethod
            };

            if (!_repository.Insert(hotelPayment))
            {
                _logger.LogInformation($"Could not insert {notification.PspReference}");
                return Task.CompletedTask;
            }

            _logger.LogInformation($"Received {(notification.Success ? "successful" : "unsuccessful")} {notification.EventCode} webhook::\n" +
                                   $"Merchant Reference ::{notification.MerchantReference} \n" +
                                   $"PSP Reference ::{notification.PspReference} \n" +
                                   $"Original Reference ::{notification.OriginalReference} \n");

            return Task.CompletedTask;
        }
    }
}