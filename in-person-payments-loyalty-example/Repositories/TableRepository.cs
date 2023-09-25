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
            Tables.Add(new TableModel()
            {
                TableName = "Pizza 1",
                Amount = 11.99M,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.NotPaid
            });

            Tables.Add(new TableModel()
            {
                TableName = "Pizza 2",
                Amount = 19.99M,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.NotPaid
            });

            Tables.Add(new TableModel()
            {
                TableName = "Pizza 3",
                Amount = 9.99M,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.NotPaid
            });


            Tables.Add(new TableModel()
            {
                TableName = "Pizza 4",
                Amount = 14.99M,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.NotPaid
            });
        }
    }
}
