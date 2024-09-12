
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Adyen.Model.TerminalApi;
using Adyen.Model.TerminalApi.Message;
using Adyen.Service;

namespace adyen_dotnet_in_person_payments_loyalty_example.Services
{
    public interface IPosInputService
    {
        Task<InputResult> GetConfirmationAsync(string serviceId, string poiId, string saleId, string text, int maxInputTime = 30, CancellationToken cancellationToken = default);
        Task<InputResult> GetTextAsync(string serviceId, string poiId, string saleId, string text, string defaultInputString, int maxInputTime = 90, CancellationToken cancellationToken = default);
    }
    
    public class PosInputService : IPosInputService
    {
        private readonly ITerminalCloudApi _terminalCloudApi;
        private readonly ILogger<PosInputService> _logger;

        public PosInputService(ITerminalCloudApi terminalCloudApi, ILogger<PosInputService> logger)
        {
            _terminalCloudApi = terminalCloudApi;
            _logger = logger;
        }

        public async Task<InputResult> GetConfirmationAsync(string serviceId, string poiId, string saleId, string text, int maxInputTime = 30, CancellationToken cancellationToken = default)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.Input,
                    MessageClass = MessageClassType.Device,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = serviceId, 
                },
                MessagePayload = new InputRequest()
                {
                    DisplayOutput = new DisplayOutput()
                    {
                      Device  = DeviceType.CustomerDisplay,
                      InfoQualify = InfoQualifyType.Display,
                      OutputContent = new OutputContent()
                      {
                          OutputFormat = OutputFormatType.Text,
                          PredefinedContent = new PredefinedContent()
                          {
                              ReferenceID = "GetConfirmation"
                          },
                          OutputText = new OutputText[]
                          {
                              new OutputText()
                              {
                                  Text = "Welcome!"
                              },
                              new OutputText()
                              {
                                  Text = text
                              },
                              new OutputText()
                              {
                                  Text = "No"
                              },
                              new OutputText()
                              {
                                  Text = "Yes"
                              }
                          }
                      }
                    },
                    InputData = new InputData()
                    {
                        Device = DeviceType.CustomerInput,
                        InfoQualify = InfoQualifyType.Input,
                        InputCommand = InputCommandType.GetConfirmation,
                        MaxInputTime = maxInputTime,
                    }
                }
            };
            SaleToPOIResponse response = await _terminalCloudApi.TerminalRequestSynchronousAsync(request);
            InputResponse inputResponse = response?.MessagePayload as InputResponse;
            return inputResponse?.InputResult;
        }

        public async Task<InputResult> GetTextAsync(string serviceId, string poiId, string saleId, string text, string defaultInputString, int maxInputTime = 90, CancellationToken cancellationToken = default)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.Input,
                    MessageClass = MessageClassType.Device,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = serviceId, 
                },
                MessagePayload = new InputRequest()
                {
                    DisplayOutput = new DisplayOutput()
                    {
                      Device  = DeviceType.CustomerDisplay,
                      InfoQualify = InfoQualifyType.Display,
                      OutputContent = new OutputContent()
                      {
                          OutputFormat = OutputFormatType.Text,
                          PredefinedContent = new PredefinedContent()
                          {
                              ReferenceID = "GetText"
                          },
                          OutputText = new OutputText[]
                          {
                              new OutputText()
                              {
                                  Text = text,
                              }
                          }
                      }
                    },
                    InputData = new InputData()
                    {
                        Device = DeviceType.CustomerInput,
                        InfoQualify = InfoQualifyType.Input,
                        InputCommand = InputCommandType.TextString,
                        MaxInputTime = maxInputTime,
                        DefaultInputString = defaultInputString
                    }
                }
            };
            var response = await _terminalCloudApi.TerminalRequestSynchronousAsync(request);
            InputResponse inputResponse = response?.MessagePayload as InputResponse;
            return inputResponse?.InputResult;
        }
    }
}