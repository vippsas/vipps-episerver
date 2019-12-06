using System;

namespace Vipps.Helpers
{
    public static class PriceHelper
    {
        public static int FormatAmountToVipps(this decimal amount)
        {
            return Convert.ToInt32(amount * 100);
        }
    }
}
