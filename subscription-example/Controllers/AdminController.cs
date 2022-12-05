﻿using Adyen.Model.Checkout;
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
        private readonly string _clientKey;
        private readonly IRecurringClient _recurringClient;
        private readonly ICheckoutClient _checkoutClient;
        private readonly ISubscriptionRepository _repository;

        public AdminController(IOptions<AdyenOptions> options, IRecurringClient recurringClient, ICheckoutClient checkoutClient, ISubscriptionRepository repository)
        {
            _clientKey = options.Value.ADYEN_CLIENT_KEY;
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
            PaymentResponse result = await _checkoutClient.MakePaymentAsync(ShopperReference.Value, recurringDetailReference);
            switch (result.ResultCode)
            {
                /// Handle your cases here.
                default:
                    ViewBag.Message = $"{Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented)};";
                    break;
            }
            return View();
        }

        [Route("admin/disable/{recurringDetailReference}")]
        public async Task<IActionResult> Disable(string recurringDetailReference)
        {
            DisableResult result = await _recurringClient.DisableRecurringDetailAsync(ShopperReference.Value, recurringDetailReference);
            ViewBag.Message = $"{result.Response}";
            return View();
        }
    }
}