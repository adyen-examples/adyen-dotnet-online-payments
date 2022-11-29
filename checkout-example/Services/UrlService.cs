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
            if (_httpContextAccessor.HttpContext?.Request?.Headers == null)
                return null;

            return GetForwardedHostUrl() ?? GetOriginUrl();
        }

        /// <summary>
        /// Gets the origin url from the header request.
        /// </summary>
        /// <returns>The origin host url.</returns>
        private string GetOriginUrl()
        {
            if (!_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Origin", out var result))
                return null;

            return result.FirstOrDefault();
        }

        /// <summary>
        /// Gets the url from a forwarded host.
        /// If the request was forwarded (due to port tunneling f.e.), we build the url and return the forwarded host url instead.
        /// </summary>
        /// <returns>The forwarded host url.</returns>
        private string GetForwardedHostUrl()
        {
            if (!_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("x-forwarded-host", out var forwardedHosts))
                return null;

            string forwardedHost = forwardedHosts.FirstOrDefault();

            if (forwardedHost == null)
                return null;

            if (!_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("x-forwarded-scheme", out var forwardedSchemes))
                return null;

            string forwardedScheme = forwardedSchemes.FirstOrDefault();
            return $"{forwardedScheme.Replace('{', ' ').Replace('}', ' ')}://{forwardedHost}";
        }
    }
}
