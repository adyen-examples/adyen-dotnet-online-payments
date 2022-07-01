using Microsoft.AspNetCore.Http;
using System.Linq;

namespace adyen_dotnet_online_payments.Services
{
    public interface IUrlService
    {
        /// <summary>
        ///     Retrieves the host url using the origin in the headers of the request.
        ///     This is used to generate the correct host url when running on either http(s)://localhost:* or http(s)://*.gitpod. 
        ///     For usage see <see cref="adyen_dotnet_online_payments.Controllers.ApiController.Sessions()"/>
        /// </summary>
        /// <returns>Returns the host url.</returns>
        string GetHostUrl();
    }

    public class UrlService : IUrlService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UrlService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public string GetHostUrl()
        {
            return _httpContextAccessor.HttpContext.Request.Headers["Origin"].FirstOrDefault();
        }
    }
}
