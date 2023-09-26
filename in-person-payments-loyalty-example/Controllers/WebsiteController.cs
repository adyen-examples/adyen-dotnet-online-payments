using Microsoft.AspNetCore.Mvc;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    public class WebsiteController : Controller
    {
        public WebsiteController()
        {
        }

        [Route("/website")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
