using Adyen.Model.Checkout;
using Adyen.Service;
using adyen_dotnet_paybylink_example.Models;
using adyen_dotnet_paybylink_example.Options;
using adyen_dotnet_paybylink_example.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_paybylink_example.Services
{
    public interface IPaymentLinkService
    {
        Task<PaymentLinkResponse> CreatePaymentLinkAsync(string reference, int amount, CancellationToken cancellationToken = default);

        ConcurrentDictionary<string, PaymentLinkModel> GetPaymentLinks(CancellationToken cancellationToken = default);
    }

    public class PaymentLinkService : IPaymentLinkService
    {
        private readonly Checkout _checkout;
        private readonly IPaymentLinkRepository _paymentLinkRepository;
        private readonly ILogger<PaymentLinkService> _logger;
        private readonly IUrlService _urlService;
        private readonly string _merchantAccount;

        public PaymentLinkService(Checkout checkout, IPaymentLinkRepository paymentLinkRepository, ILogger<PaymentLinkService> logger, IUrlService urlService, IOptions<AdyenOptions> options)
        {
            _checkout = checkout;
            _paymentLinkRepository = paymentLinkRepository;
            _logger = logger;
            _urlService = urlService;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        public async Task<PaymentLinkResponse> CreatePaymentLinkAsync(string reference, int amount, CancellationToken cancellationToken)
        {
            var orderRef = Guid.NewGuid(); // TODO: Check whether user is allowed to generate this? Probably not.
            var createPayByLinkRequest = new CreatePaymentLinkRequest()
            {
                MerchantAccount = _merchantAccount, // Required.
                Amount = new Amount("EUR", amount), // Value in minor units.
                Reference = orderRef.ToString(),    // Required.
                ReturnUrl = $"{_urlService.GetHostUrl()}/redirect?orderRef={orderRef}", // Required for 3DS2 redirect flow.
            };

            try
            {
                var response = await _checkout.PaymentLinksAsync(createPayByLinkRequest);
                _paymentLinkRepository.Upsert(response.Id, response.Reference, DateTime.Parse(response.ExpiresAt), response.Status.ToString());
                _logger.LogInformation($"Response Payments API:\n{response}\n");
                return response;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request failed for Sessions:\n{e.ResponseBody}\n");
                throw;
            }
        }

        public ConcurrentDictionary<string, PaymentLinkModel> GetPaymentLinks(CancellationToken cancellationToken)
        {
            return _paymentLinkRepository.PaymentLinks;
        }
    }
}
