using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ValueObjects
{
    public class DateRange : ValueObject
    {
        public DateTime Start { get; }
        public DateTime End { get; }
        public TimeSpan Duration => End - Start;
        private DateRange(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public static DateRange Create(DateTime start, DateTime end)
        {
            if (start >= end)
                throw new ArgumentException("Дата начала должна быть раньше даты окончания");

            return new DateRange(start, end);
        }

        public bool Overlaps(DateRange other)
        {
            return Start < other.End && other.Start < End;
        }

        public bool Contains(DateTime date)
        {
            return Start <= date && date <= End;
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Start;
            yield return End;
        }
    }
}
