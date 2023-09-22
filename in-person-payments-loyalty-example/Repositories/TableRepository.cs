using System.Collections.Generic;
using adyen_dotnet_in_person_payments_loyalty_example.Models;

namespace adyen_dotnet_in_person_payments_loyalty_example.Repositories
{
    public interface ITableRepository
    {
        /// <summary>
        /// List of all <see cref="TableModel"/>s.
        /// </summary>
        List<TableModel> Tables { get; }
    }

    public class TableRepository : ITableRepository
    {
        public List<TableModel> Tables { get; }

        public TableRepository()
        {
            Tables = new List<TableModel>();

            // Add tables.
            for (int i = 0; i < 4; i++)
            {
                int tableNumber = i + 1;
                Tables.Add(new TableModel()
                {
                    TableName = "Table " + tableNumber,
                    Amount = 22.22M * tableNumber,
                    Currency = "EUR",
                    PaymentStatus = PaymentStatus.NotPaid
                });
            }
        }
    }
}
