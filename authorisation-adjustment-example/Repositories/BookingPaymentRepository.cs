﻿using adyen_dotnet_authorisation_adjustment_example.Models;
using System;
using System.Collections.Concurrent;

namespace adyen_dotnet_authorisation_adjustment_example.Repositories
{
    /// <summary>
    /// Acts as a local (memory) repository to store <see cref="BookingPayment"/>s.
    /// These can be used to make authorisation adjustments.
    /// </summary>
    public interface IBookingPaymentRepository
    {
        ConcurrentDictionary<string, BookingPayment> BookingPayments { get; }
       
        bool Remove(string pspReference);

        bool Upsert(string pspReference, string reference, int amount, string currency);
    }

    public class BookingPaymentRepository : IBookingPaymentRepository
    {
        public ConcurrentDictionary<string, BookingPayment> BookingPayments { get; }

        public BookingPaymentRepository()
        {
            BookingPayments = new ConcurrentDictionary<string, BookingPayment>();
        }

        public bool Remove(string pspReference)
        {
            return BookingPayments.TryRemove(pspReference, out var _);
        }

        public bool Upsert(string pspReference, string reference, int amount, string currency)
        {
            return BookingPayments.TryAdd(
                pspReference,
                new BookingPayment()
                {
                    PspReference = pspReference,
                    Reference = reference,
                    Amount = amount,
                    Currency = currency,
                    DateTime = DateTime.UtcNow
                }
            );
        }
    }
}