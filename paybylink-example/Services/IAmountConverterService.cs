using System;

namespace adyen_dotnet_paybylink_example.Services
{
    public interface IAmountConverterService
    {
        public int ConvertToMinorUnits(string amount);
    }

    public class AmountConverterService : IAmountConverterService
    {
        public int ConvertToMinorUnits(string amount)
        {
            throw new Exception();
        }
    }
}