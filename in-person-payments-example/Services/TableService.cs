﻿using adyen_dotnet_in_person_payments_example.Models;
using System.Collections.Generic;

namespace adyen_dotnet_in_person_payments_example.Services
{
    public interface ITableService
    {
        List<TableModel> Tables { get; }

        // TODO
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

            // Add 16 random dummy values.
            for (int i = 0; i < 16; i++)
            {
                int tableNumber = i + 1;
                Tables.Add(new TableModel()
                {
                    TableName = "Table " + tableNumber,
                    Amount = 11.11M * tableNumber,
                    Currency = "EUR",
                    TableStatus = i % 2 == 0 ? TableStatus.Paid : (i % 3 == 0 ? TableStatus.NotPaid : TableStatus.Refunded), // TODO, remove 
                    PoiTransactionId = null,
                    SaleReferenceId = null
                });
            }
        }
    }
}
