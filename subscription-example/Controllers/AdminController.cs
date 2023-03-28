using Adyen.HttpClient;
using Adyen.Model.Checkout;
using Adyen.Model.Recurring;
using adyen_dotnet_subscription_example.Clients;
using adyen_dotnet_subscription_example.Models;
using adyen_dotnet_subscription_example.Options;
using adyen_dotnet_subscription_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace adyen_dotnet_subscription_example.Controllers
{
    public class AdminController : Controller
    {
        private readonly IRecurringClient _recurringClient;
        private readonly ICheckoutClient _checkoutClient;
        private readonly ISubscriptionRepository _repository;

        public AdminController(IOptions<AdyenOptions> options, IRecurringClient recurringClient, ICheckoutClient checkoutClient, ISubscriptionRepository repository)
        {
            _recurringClient = recurringClient;
            _checkoutClient = checkoutClient;
            _repository = repository;
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
        public async Task<IActionResult> MakePayment(string recurringDetailReference)
        {
            try
            {
                PaymentResponse result = await _checkoutClient.MakePaymentAsync(ShopperReference.Value, recurringDetailReference);
                
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
            }
            return View();
        }

        [Route("admin/disable/{recurringDetailReference}")]
        public async Task<IActionResult> Disable(string recurringDetailReference)
        {
            try
            {
                DisableResult result = await _recurringClient.DisableRecurringDetailAsync(ShopperReference.Value, recurringDetailReference);
                
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
            }
            return View();
        }
    }
}
