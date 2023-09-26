using Adyen.Model.Nexo;
using Adyen.Model.Nexo.Message;
using Adyen.Service;
using System.Threading;
using System.Threading.Tasks;

namespace adyen_dotnet_in_person_payments_loyalty_example.Services.CardAcquisition
{
    public interface IPosCardAcquisitionAbortService
    {
        Task<SaleToPOIResponse> SendAbortRequestAsync(string serviceId, string poiId, string saleId, bool success, int loyaltyPoints = 0, CancellationToken cancellationToken = default);
    }

    public class PosCardAcquisitionAbortService : IPosCardAcquisitionAbortService
    {
        private readonly IPosPaymentCloudApi _posPaymentCloudApi;

        public PosCardAcquisitionAbortService(IPosPaymentCloudApi posPaymentCloudApi)
        {
            _posPaymentCloudApi = posPaymentCloudApi;
        }

        public Task<SaleToPOIResponse> SendAbortRequestAsync(string serviceId, string poiId, string saleId, bool success, int loyaltyPoints = 0, CancellationToken cancellationToken = default)
        {
            SaleToPOIRequest request = GetAbortRequest(serviceId, poiId, saleId, success, loyaltyPoints);
            return _posPaymentCloudApi.TerminalApiCloudSynchronousAsync(request);
        }

        private SaleToPOIRequest GetAbortRequest(string serviceId, string poiId, string saleId, bool success, int loyaltyPoints)
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
                                /// Possible icon values:
                                /// "Accepted": green check mark.
                                /// "AcceptedAnimated": animated green check mark.
                                /// "Declined": red cross.
                                /// "DeclinedAnimated": animated red cross.
                                /// "Idle": no icon.
                                ReferenceID = success ? "AcceptedAnimated" : "Idle"
                            },
                            OutputFormat = OutputFormatType.Text,
                            OutputText = new OutputText[]
                            {
                                new OutputText { Text = success ? "Welcome!" : "Loyalty Program"},
                                new OutputText { Text = success ? $"You can get discounts {GetPercentage(loyaltyPoints)}when buying pizza now! Loyalty Points: {loyaltyPoints}" : "Feel free to sign-up for the program to get discounts." },
                            }
                        },
                        
                    }
                }
            };

            return request;
        }

        private string GetPercentage(int loyaltyPoints)
        {
            if (loyaltyPoints >= 3000)
            {
                return "(-50%) ";
            }
            else if (loyaltyPoints >= 2000)
            {
                return "(-25%) ";
            }
            else if (loyaltyPoints >= 1000)
            {
                return "(-10%) ";
            }

            return "";
        }
    }
}