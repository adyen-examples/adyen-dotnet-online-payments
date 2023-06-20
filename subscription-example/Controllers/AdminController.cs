using Adyen.HttpClient;
using Adyen.Model.Checkout;
using Adyen.Model.Recurring;
using adyen_dotnet_subscription_example.Models;
using adyen_dotnet_subscription_example.Repositories;
using adyen_dotnet_subscription_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    public class AdminController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ICheckoutService _checkoutClient;
        private readonly ISubscriptionRepository _repository;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ISubscriptionService subscriptionService, ICheckoutService checkoutClient, ISubscriptionRepository repository, ILogger<AdminController> logger)
        {
            _subscriptionService = subscriptionService;
            _checkoutClient = checkoutClient;
            _repository = repository;
            _logger = logger;
        }

        [Route("admin")]
        public IActionResult Index()
        {
            List<SubscribedCustomer> details = new List<SubscribedCustomer>();

            // We fetch all shopperReferences that we have stored in our (local) repository and show it.
            foreach (KeyValuePair<string, SubscribedCustomer> kvp in _repository.SubscribedCustomers)
            {
                details.Add(kvp.Value);
            }
            ViewBag.Details = details;
            return View();
        }

        [Route("admin/makepayment/{recurringDetailReference}")]
        public async Task<IActionResult> MakePayment(string recurringDetailReference, CancellationToken cancellationToken = default)
        {
            try
            {
                PaymentResponse result = await _checkoutClient.MakePaymentAsync(ShopperReference.Value, recurringDetailReference, cancellationToken);
                
                switch (result.ResultCode)
                {
                    // Handle other payment response cases here.
                    case PaymentResponse.ResultCodeEnum.Authorised:
                        ViewBag.Message = $"Successfully authorised a payment with RecurringDetailReference: {recurringDetailReference}.";
                        ViewBag.Img = "success";
                        break;
                    default:
                        ViewBag.Message = $"Payment failed for RecurringDetailReference {recurringDetailReference}. See error logs for the message.";
                        ViewBag.Img = "failed";
                        break;
                }
            }   
            catch (HttpClientException e)
            {
                ViewBag.Message = $"Payment failed for RecurringDetailReference {recurringDetailReference}. See error logs for the exception.";
                ViewBag.Img = "failed";
                _logger.LogError($"Payment failed for RecurringDetailReference {recurringDetailReference}: \n{e.ResponseBody}\n");
            }
            return View();
        }

        [Route("admin/disable/{recurringDetailReference}")]
        public async Task<IActionResult> Disable(string recurringDetailReference, CancellationToken cancellationToken = default)
        {
            try
            {
                DisableResult result = await _subscriptionService.DisableRecurringDetailAsync(ShopperReference.Value, recurringDetailReference, cancellationToken);
                
                switch (result.Response)
                {
                    case "[detail-successfully-disabled]":
                        ViewBag.Message = $"Disabled RecurringDetailReference {recurringDetailReference}.";
                        ViewBag.Img = "success";
                        break;
                    default:
                        ViewBag.Message = $"Disable failed for RecurringDetailReference {recurringDetailReference}. See logs for the response message."; ;
                        ViewBag.Img = "failed";
                        break;
                }
            }
            catch (HttpClientException e)
            {
                ViewBag.Message = $"Disable failed for RecurringDetailReference {recurringDetailReference}. See error logs for the exception.";
                ViewBag.Img = "failed";
                _logger.LogError($"Disable failed for RecurringDetailReference {recurringDetailReference}: \n{e.ResponseBody}\n");
            }
            return View();
        }
    }
}
