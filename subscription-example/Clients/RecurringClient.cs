using Adyen.Model.Recurring;
using adyen_dotnet_subscription_example.Options;
using adyen_dotnet_subscription_example.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Recurring = Adyen.Service.Recurring;

namespace adyen_dotnet_subscription_example.Clients
{
    public interface IRecurringClient
    {
        /// <summary>
        /// Disables the <paramref name="recurringDetailReference"/> (token) for the given <paramref name="shopperReference"/>.
        /// </summary>
        /// <param name="shopperReference">The unique shopper reference (usually a GUID to identify your shopper).</param>
        /// <param name="recurringDetailReference">The <paramref name="recurringDetailReference"/> token that is retrieved from <see cref="Controllers.WebhookController.Webhooks"/>.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<DisableResult> DisableRecurringDetailAsync(string shopperReference, string recurringDetailReference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all recurring details given a <paramref name="shopperReference"/>.
        /// </summary>
        /// <param name="shopperReference">The unique shopper reference (usually a GUID to identify your shopper).</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
        /// <returns><see cref="RecurringDetailsResult"/></returns>
        Task<RecurringDetailsResult> ListRecurringDetailAsync(string shopperReference, CancellationToken cancellationToken = default);
    }

    public class RecurringClient : IRecurringClient
    {
        private readonly ILogger<RecurringClient> _logger;
        private readonly string _merchantAccount;
        private readonly Recurring _recurring;
        private readonly ISubscriptionRepository _repository;

        public RecurringClient(ILogger<RecurringClient> logger, Recurring recurring, ISubscriptionRepository repository, IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _recurring = recurring;
            _repository = repository;
            _merchantAccount = options.Value.ADYEN_MERCHANT_ACCOUNT;
        }

        public async Task<RecurringDetailsResult> ListRecurringDetailAsync(string shopperReference, CancellationToken cancellationToken)
        {
            var request = new RecurringDetailsRequest()
            {
                MerchantAccount = _merchantAccount,
                ShopperReference = shopperReference
            };

            try
            {
                var recurringDetailsResult = await _recurring.ListRecurringDetailsAsync(request);
                _logger.LogInformation($"Response for Recurring API::\n{recurringDetailsResult}\n");
                return recurringDetailsResult;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for listing recurring details failed::\n{e.ResponseBody}\n");
                throw;
            }
        }

        public async Task<DisableResult> DisableRecurringDetailAsync(string shopperReference, string recurringDetailReference, CancellationToken cancellationToken = default)
        {
            var request = new DisableRequest()
            {
                MerchantAccount = _merchantAccount,
                ShopperReference = shopperReference,
                RecurringDetailReference = recurringDetailReference
            };

            try
            {
                var disableResult = await _recurring.DisableAsync(request);
                _repository.Remove(shopperReference, recurringDetailReference);

                _logger.LogInformation($"Response for Recurring API::\n{disableResult}\n");
                return disableResult;
            }
            catch (Adyen.HttpClient.HttpClientException e)
            {
                _logger.LogError($"Request for listing recurring details failed::\n{e.ResponseBody}\n");
                throw;
            }
        }
    }
}
