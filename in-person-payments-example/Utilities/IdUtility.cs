using System;
using System.Text;

namespace adyen_dotnet_in_person_payments_example.Extensions
{
    public static class IdUtility
    {
        private static string AlphanumericCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        private static Random Random = new Random();

        /// <summary>
        /// Gets a random selection from any <see cref="AlphanumericCharacters"/> with the specified <paramref name="length"/>.
        /// </summary>
        /// <param name="length">Length of the generated id.</param>
        /// <returns>Alphanumeric Id.</returns>
        public static string GetRandomAlphanumericId(int length)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                sb.Append(AlphanumericCharacters[Random.Next(length)]);
            }

            return sb.ToString();
        }
    }
}
