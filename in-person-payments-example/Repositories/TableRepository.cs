using adyen_dotnet_in_person_payments_example.Models;
using System.Collections.Generic;

namespace adyen_dotnet_in_person_payments_example.Repositories
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
            
            // Add tables for refusals
            Tables.Add(new TableModel()
            {
                TableName = "Table 1.24 - NOT_ENOUGH_BALANCE",
                Amount = 1.24M,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.NotPaid
            });
            Tables.Add(new TableModel()
            {
                TableName = "Table 1.25 - BLOCK_CARD",
                Amount = 1.25M,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.NotPaid
            });
            Tables.Add(new TableModel()
            {
                TableName = "Table 1.26 - CARD_EXPIRED",
                Amount = 1.26M,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.NotPaid
            });
            Tables.Add(new TableModel()
            {
                TableName = "Table 1.27 - INVALID_AMOUNT",
                Amount = 1.27M,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.NotPaid
            });
            Tables.Add(new TableModel()
            {
                TableName = "Table 1.28 - INVALID_CARD",
                Amount = 1.28M,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.NotPaid
            });
            Tables.Add(new TableModel()
            {
                TableName = "Table 1.34 - WrongPin",
                Amount = 1.34M,
                Currency = "EUR",
                PaymentStatus = PaymentStatus.NotPaid
            });
        }
    }
}
