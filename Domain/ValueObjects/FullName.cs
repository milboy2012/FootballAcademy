using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.ValueObjects
{
    public class FullName : ValueObject
    {
        public string Fam{ get; }
        public string Im{ get; }
        public string Ot { get; }
        private FullName(string fam, string im, string ot)
        {
            Fam = fam; Im = im; Ot = ot;
        }
        

        public static FullName Create(string fam, string im, string ot)
        {
            if (string.IsNullOrWhiteSpace(fam) || string.IsNullOrWhiteSpace(im) || string.IsNullOrWhiteSpace(ot))
                throw new ArgumentException("Фамилия, имя и отчество обязательны для заполнения", nameof(fam));

            return new FullName(
                fam.Trim(),
                im.Trim(),
                ot.Trim()
            );
        }

        public string GetFullName() => $"{Fam} {Im} {Ot}".Trim();
        public string GetInitials() => $"{Fam[0]}{Im[0]}{Ot[0]}".Trim();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Fam.ToLowerInvariant();
            yield return Im.ToLowerInvariant();
            yield return Ot.ToLowerInvariant();
        }
    }
}
