using System.Collections.Generic;

namespace adyen_dotnet_subscription_example.Services
{
    public interface ISubscriptionService
    {
        bool Upsert(string recurringDetailReference)
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly Dictionary<string, string> _keyValuePairs = new Dictionary<string, string>();
    }
}
