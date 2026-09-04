using System.Security.Cryptography;

namespace UI.Services.Model
{
    public class PasswordGenerator
    {
        public static string Generate()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", lower = "abcdefghijkmnpqrstuvwxyz",
                     digits = "23456789", special = "!@#$%&*";
            var all = upper + lower + digits + special;

            var chars = new List<char>
            {
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                special[RandomNumberGenerator.GetInt32(special.Length)]
            };
            while (chars.Count < 12) chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

            // перемешать
            for (var i = chars.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars.ToArray());
        }
    }
}
