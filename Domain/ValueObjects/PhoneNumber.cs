using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Domain.ValueObjects
{
    public class PhoneNumber : ValueObject
    {
        public string Value { get; }

        private PhoneNumber(string value)
        {
            Value = value;
        }

        public static PhoneNumber Create(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Номер телефона не может быть пустым", nameof(phoneNumber));

            phoneNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // Формат +375XXXXXXXXX 
            var pattern = @"^(\+375)\d{9}$";
            if (!Regex.IsMatch(phoneNumber, pattern))
                throw new ArgumentException("Некорректный формат номера телефона. Используйте +375XXXXXXXXX", nameof(phoneNumber));

            return new PhoneNumber(phoneNumber);
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
