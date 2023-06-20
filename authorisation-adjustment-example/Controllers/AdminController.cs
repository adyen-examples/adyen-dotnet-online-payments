using adyen_dotnet_authorisation_adjustment_example.Models;
using adyen_dotnet_authorisation_adjustment_example.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_authorisation_adjustment_example.Controllers
{
    public class AdminController : Controller
    {
        private readonly IPspReferenceRepository _repository;

        public AdminController(IPspReferenceRepository repository)
        {
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

        [Route("admin/decremental-adjustment/{pspReference}/{amount}")]
        public async Task<IActionResult> DecrementalAdjustment(string pspReference, int amount, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
            //return View();
        }

        [Route("admin/incremental-adjustment/{pspReference}/{amount}")]
        public async Task<IActionResult> IncrementalAdjustment(string pspReference, int amount, CancellationToken cancellationToken = default)
        {
            //try
            //{
            //    PaymentResponse result = await _checkoutClient.MakePaymentAsync(ShopperReference.Value, recurringDetailReference, cancellationToken);

            //    switch (result.ResultCode)
            //    {
            //        // Handle other payment response cases here.
            //        case PaymentResponse.ResultCodeEnum.Authorised:
            //            ViewBag.Message = $"Successfully authorised a payment with RecurringDetailReference: {recurringDetailReference}.";
            //            ViewBag.Img = "success";
            //            break;
            //        default:
            //            ViewBag.Message = $"Payment failed for RecurringDetailReference {recurringDetailReference}. See error logs for the message.";
            //            ViewBag.Img = "failed";
            //            break;
            //    }
            //}   
            //catch (HttpClientException)
            //{
            //    ViewBag.Message = $"Payment failed for RecurringDetailReference {recurringDetailReference}. See error logs for the exception.";
            //    ViewBag.Img = "failed";
            //}
            throw new System.NotImplementedException();
            //return View();
        }

        [Route("admin/extend-duration/{pspReference}")]
        public async Task<IActionResult> ExtendDuration(string pspReference, CancellationToken cancellationToken = default)
        {
            //try
            //{
            //    DisableResult result = await _recurringClient.DisableRecurringDetailAsync(ShopperReference.Value, recurringDetailReference, cancellationToken);

            //    switch (result.Response)
            //    {
            //        case "[detail-successfully-disabled]":
            //            ViewBag.Message = $"Disabled RecurringDetailReference {recurringDetailReference}.";
            //            ViewBag.Img = "success";
            //            break;
            //        default:
            //            ViewBag.Message = $"Disable failed for RecurringDetailReference {recurringDetailReference}. See logs for the response message."; ;
            //            ViewBag.Img = "failed";
            //            break;
            //    }
            //}
            //catch (HttpClientException)
            //{
            //    ViewBag.Message = $"Disable failed for RecurringDetailReference {recurringDetailReference}. See error logs for the exception.";
            //    ViewBag.Img = "failed";
            //}
            throw new System.NotImplementedException();
            //return View();
        }
    }
}
