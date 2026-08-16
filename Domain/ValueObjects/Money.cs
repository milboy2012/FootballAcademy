using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ValueObjects
{
    public class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        private Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public static Money Create(decimal amount, string currency = "BYN")
        {
            if (amount < 0)
                throw new ArgumentException("Сумма не может быть отрицательной", nameof(amount));

            return new Money(Math.Round(amount, 2), currency);
        }

        public static Money Byn(decimal amount) => Create(amount, "BYN");
        public static Money Usd(decimal amount) => Create(amount, "USD");
        public static Money Eur(decimal amount) => Create(amount, "EUR");

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Нельзя складывать валюты {Currency} и {other.Currency}");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Нельзя вычитать валюты {Currency} и {other.Currency}");

            return new Money(Amount - other.Amount, Currency);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }
}
