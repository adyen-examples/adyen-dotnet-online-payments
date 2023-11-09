using System.Text.RegularExpressions;

namespace adyen_dotnet_in_person_payments_loyalty_example.Utilities
{
    public static class StringUtility
    {
        private static Regex Regex = new Regex(@"^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$");

        public static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email) && Regex.IsMatch(email);
        }
    }
}