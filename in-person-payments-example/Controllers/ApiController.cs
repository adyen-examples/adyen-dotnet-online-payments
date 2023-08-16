using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_example.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IUrlService _urlService;
        private readonly InPersonPaymentService _inPersonPaymentService;

        public ApiController(ILogger<ApiController> logger, IUrlService urlService, InPersonPaymentService inPersonPaymentService)
        {
            _logger = logger;
            _urlService = urlService;
            _inPersonPaymentService = inPersonPaymentService;
        }

        [HttpPost("api/terminal-api/sync")]
        public async Task<ActionResult<SaleToPOIResponse>> CloudTerminalApi(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _inPersonPaymentService.SendSaleToPOIRequestAsync(, "POSMachine_V400m_Your_Custom_Id", "EUR", 42, cancellationToken);
                return Ok(response);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.ToString());
                throw;
            }
        }
    }
}