using Adyen.HttpClient;
using Adyen.Model.Nexo;
using adyen_dotnet_in_person_payments_loyalty_example.Models;
using adyen_dotnet_in_person_payments_loyalty_example.Models.Requests;
using adyen_dotnet_in_person_payments_loyalty_example.Models.Responses;
using adyen_dotnet_in_person_payments_loyalty_example.Options;
using adyen_dotnet_in_person_payments_loyalty_example.Repositories;
using adyen_dotnet_in_person_payments_loyalty_example.Services;
using adyen_dotnet_in_person_payments_loyalty_example.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IPosAbortService _posAbortService;
        private readonly ITableRepository _tableService;
        private readonly string _saleId;
        private readonly string _poiId;

        public ApiController(ILogger<ApiController> logger,
            IPosAbortService posAbortService,
            ITableRepository tableService, 
            IOptions<AdyenOptions> options)
        {
            _logger = logger;
            _posAbortService = posAbortService;
            _tableService = tableService;
            _poiId = options.Value.ADYEN_POS_POI_ID;
            _saleId = options.Value.ADYEN_POS_SALE_ID;
        }

        [HttpGet("api/abort/{tableName}")]
        public async Task<ActionResult<SaleToPOIResponse>> Abort(string tableName, CancellationToken cancellationToken = default)
        {
            try
            {
                TableModel table = _tableService.Tables.FirstOrDefault(t => t.TableName == tableName);

                if (table?.PaymentStatusDetails?.ServiceId == null)
                {
                    return NotFound();
                }

                SaleToPOIResponse abortResponse = await _posAbortService.SendAbortRequestAsync(MessageCategoryType.Payment, table.PaymentStatusDetails.ServiceId, _poiId, _saleId, cancellationToken);
                return Ok(abortResponse);
            }
            catch (HttpClientException e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                throw;
            }
        }
    }
}