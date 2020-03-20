using System;

namespace Vipps.Helpers
{
    public static class OrderNumberHelper
    {
        public static string GenerateOrderNumber()
        {
            var ticks = DateTime.Now.Ticks;
            var rand = new Random();
            return $"{ticks}{rand.Next(0, 999)}";
        }
    }
}
