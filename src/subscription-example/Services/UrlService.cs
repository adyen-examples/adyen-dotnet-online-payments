using Microsoft.AspNetCore.Http;
using System.Linq;

namespace adyen_dotnet_subscription_example.Services
{
    public interface IUrlService
    {
        /// <summary>
        /// Retrieves the host url using the origin in the headers of the request.
        /// This is used to generate the correct host url when running on either http(s)://localhost:* or http(s)://*.gitpod. 
        /// For usage see <see cref="adyen_dotnet_subscription_example.Controllers.ApiController.Sessions()"/>
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
            return GetForwardedHostUrl() ?? _httpContextAccessor.HttpContext.Request.Headers["Origin"].FirstOrDefault();
        }

        /// <summary>
        /// If the request was forwarded (due to f.e. port tunneling), build the url and return the forwarded host url instead.
        /// </summary>
        /// <returns>The forwarded host url.</returns>
        private string GetForwardedHostUrl()
        {
            string forwardedHost = _httpContextAccessor.HttpContext.Request.Headers["x-forwarded-host"].FirstOrDefault();

            if (forwardedHost == null)
                return null;

            string forwardedScheme = _httpContextAccessor.HttpContext.Request.Headers["x-forwarded-scheme"].FirstOrDefault();
            return $"{forwardedScheme.Replace('{', ' ').Replace('}', ' ')}://{forwardedHost}";
        }
    }
}
