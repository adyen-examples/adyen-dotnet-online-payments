using System.Collections.Generic;

namespace adyen_dotnet_subscription_example.Services
{
    public interface ISubscriptionService
    {
        void Upsert(string recurringDetailReference, string name);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly Dictionary<string, string> _keyValuePairs = new Dictionary<string, string>();

        public void Upsert(string recurringDetailReference, string name)
        {
            if (!_keyValuePairs.ContainsKey(recurringDetailReference))
            {
                _keyValuePairs.Add(recurringDetailReference, name);
                return;
            }
            _keyValuePairs[recurringDetailReference] = name;
        }
    }
}
