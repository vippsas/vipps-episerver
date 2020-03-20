using System.Text.RegularExpressions;

namespace Vipps.Helpers
{
    public static class PhoneNumberHelper
    {
        public static string Validate(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return string.Empty;

            phoneNumber = Regex.Replace(phoneNumber, "[^0-9]", "");

            if (phoneNumber.StartsWith("00"))
            {
                phoneNumber = phoneNumber.Remove(0, 4);
            }

            if (phoneNumber.StartsWith("47"))
            {
                phoneNumber = phoneNumber.Remove(0, 2);
            }

            return phoneNumber.Length == 8 
                ? phoneNumber
                : string.Empty;
        }
    }
}
