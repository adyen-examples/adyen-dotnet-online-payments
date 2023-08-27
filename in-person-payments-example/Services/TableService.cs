using adyen_dotnet_in_person_payments_example.Models;
using System.Collections.Generic;

namespace adyen_dotnet_in_person_payments_example.Services
{
    public interface ITableService
    {
        List<TableModel> Tables { get; }

        //void SettleBill();

        //void PrintReceipt();

        //void Reverse();
    }

    public class TableService : ITableService
    {
        public List<TableModel> Tables { get; }

        public TableService()
        {
            Tables = new List<TableModel>();

            // Add 16 dummy values.
            for (int i = 0; i < 16; i++)
            {
                int tableNumber = i + 1;
                Tables.Add(new TableModel()
                {
                    Name = "Table " + tableNumber,
                    Amount = 11.11M * tableNumber,
                    Currency = "EUR",
                    IsPaid = false
                });
            }
        }
    }
}
