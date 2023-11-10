using adyen_dotnet_in_person_payments_loyalty_example.Utilities;
using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Services.CardAcquisition
{
    public interface IPosCardAcquisitionAbortService
    {
        Task<SaleToPOIResponse> SendAbortRequestAfterSignUpAsync(string serviceId, string poiId, string saleId, string textTitle, string textDescription, CancellationToken cancellationToken = default);
    }

    public class PosCardAcquisitionAbortService : IPosCardAcquisitionAbortService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosCardAcquisitionAbortService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendAbortRequestAfterSignUpAsync(string serviceId, string poiId, string saleId, string textTitle, string textDescription, CancellationToken cancellationToken = default)
        {
            SaleToPOIRequest request = new SaleToPOIRequest()
            {
                MessageHeader = new MessageHeader()
                {
                    MessageCategory = MessageCategoryType.EnableService,
                    MessageClass = MessageClassType.Service,
                    MessageType = MessageType.Request,
                    POIID = poiId,
                    SaleID = saleId,
                    ServiceID = serviceId,
                },
                MessagePayload = new EnableServiceRequest()
                {
                    TransactionAction = TransactionActionType.AbortTransaction,
                    DisplayOutput = new DisplayOutput()
                    {
                        Device = DeviceType.CustomerDisplay,
                        InfoQualify = InfoQualifyType.Display,
                        OutputContent = new OutputContent()
                        {
                            PredefinedContent = new PredefinedContent()
                            {
                                // Possible icon values:
                                // "Accepted": green check mark.
                                // "AcceptedAnimated": animated green check mark.
                                // "Declined": red cross.
                                // "DeclinedAnimated": animated red cross.
                                // "Idle": no icon.
                                ReferenceID = "AcceptedAnimated"
                            },
                            OutputFormat = OutputFormatType.Text,
                            OutputText = new OutputText[]
                            {
                                // We include a DisplayOutput object to show a message to the shopper that they have signed-up for the loyalty program.
                                // Otherwise, if you omit the DisplayOutput with this AbortTransaction action, the terminal will show a red cross (e.g. 'Transaction canceled') and that can be confusing for the shopper.
                                new OutputText { Text = textTitle },
                                new OutputText { Text = textDescription }
                            },
                        }
                    }
                }
            };

            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }
    }
}