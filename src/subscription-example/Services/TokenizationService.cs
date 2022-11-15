using adyen_dotnet_subscription_example.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace adyen_dotnet_subscription_example.Services
{
    public interface ITokenizationService 
    { 
    }

    public class TokenizationService : ITokenizationService
{
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenizationService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public void MakePayment()
        {
            //throw //....
        }
    }
}
