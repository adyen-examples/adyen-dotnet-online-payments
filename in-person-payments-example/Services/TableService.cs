using adyen_dotnet_in_person_payments_example.Models;
using System.Collections.Generic;

namespace adyen_dotnet_in_person_payments_example.Services
{
    public interface ITableService
    {
        List<TableModel> Tables { get; }
    }

    public class TableService : ITableService
    {
        public List<TableModel> Tables { get; }

        public TableService()
        {
            Tables = new List<TableModel>();

            // Add dummy values.
            for (int i = 1; i < 13; i++)
            {
                Tables.Add(new TableModel()
                {
                    Name = "Table " + i,
                    Amount = 11.11M * i,
                    Currency = "EUR"
                });
            }
        }
    }
}
