using Adyen.Model.Checkout;
using Adyen.Service.Checkout;
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
    public interface ILinksService
    {
        Task<PaymentLinkResponse> CreatePaymentLinkAsync(string reference, long amount, bool isReusable, CancellationToken cancellationToken = default);

        Task<ConcurrentDictionary<string, PaymentLinkModel>> GetAndUpdatePaymentLinksAsync(CancellationToken cancellationToken = default);
    }

    public class LinksService : ILinksService
    {
        private readonly IPaymentLinksService _paymentLinksService;
        private readonly IPaymentLinkRepository _paymentLinkRepository;
        private readonly ILogger<LinksService> _logger;
        private readonly IUrlService _urlService;
        private readonly string _merchantAccount;

        public LinksService(IPaymentLinksService paymentLinksService, IPaymentLinkRepository paymentLinkRepository,  IUrlService urlService, IOptions<AdyenOptions> options, ILogger<LinksService> logger)
        {
            _paymentLinksService = paymentLinksService;
            _paymentLinkRepository = paymentLinkRepository;
            _logger = logger;
            _urlService = urlService;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        public async Task<PaymentLinkResponse> CreatePaymentLinkAsync(string reference, long amount, bool isReusable, CancellationToken cancellationToken)
        {
            var createPayByLinkRequest = new PaymentLinkRequest(
                merchantAccount: _merchantAccount,          // Required.
                amount: new Amount("EUR", amount),          // Required, value in minor units.
                reference: reference,                       // Required, use for example: new Guid().
                reusable: isReusable,                       // Optional, if set to true, the link can be used multiple times for a payment.
                returnUrl: $"{_urlService.GetHostUrl()}"    // To direct the customer to your page after completing a Pay by Link payment, include a returnUrl in your /paymentLinks request.
                                                            // With this request, a continue button will appear on the page. If customers click the button, they’re redirected to the specified URL.
            );

            try
            {
                
                var response = await _paymentLinksService.PaymentLinksAsync(createPayByLinkRequest, cancellationToken: cancellationToken);
                _paymentLinkRepository.Upsert(
                    id: response.Id, 
                    reference: response.Reference, response.Url, 
                    expiresAt: response.ExpiresAt, 
                    status: response.Status.ToString(),
                    isReusable: response.Reusable.HasValue ? response.Reusable.Value : false
                );
                _logger.LogInformation($"Response from API:\n{response}\n");
                return response;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request failed for Pay By Link:\n{e.ResponseBody}\n");
                throw;
            }
        }

        public async Task<ConcurrentDictionary<string, PaymentLinkModel>> GetAndUpdatePaymentLinksAsync(CancellationToken cancellationToken)
        {
            // Update the payment links. 
            foreach (var kvp in _paymentLinkRepository.PaymentLinks)
            {
                try
                {
                    PaymentLinkResponse response = await _paymentLinksService.GetPaymentLinkAsync(kvp.Value.Id, cancellationToken: cancellationToken);
                    
                    // Update each individual payment link.
                    _paymentLinkRepository.Upsert(
                        id: response.Id,
                        reference: response.Reference, response.Url,
                        expiresAt: response.ExpiresAt,
                        status: response.Status.ToString(),
                        isReusable: response.Reusable.HasValue ? response.Reusable.Value : false
                    );
                }
                catch (Adyen.HttpClient.HttpClientException e)
                {
                    _logger.LogError($"Request failed for Pay By Link:\n{e.ResponseBody}\n");
                    throw;
                }
            }
            return _paymentLinkRepository.PaymentLinks;
        }
    }
}
