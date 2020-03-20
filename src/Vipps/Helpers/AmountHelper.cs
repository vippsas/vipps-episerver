using System;

namespace Vipps.Helpers
{
    public static class AmountHelper
    {
        public static int FormatAmountToVipps(this decimal amount)
        {
            return Convert.ToInt32(amount * 100);
        }

        public static decimal FormatAmountFromVipps(this int amount)
        {
            return Convert.ToDecimal(amount) / 100;
        }
    }
}