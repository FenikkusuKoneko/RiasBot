using System;
using System.Collections.Generic;

namespace Rias.Core.Extensions
{
    public static class RiasExtensions
    {
        /// <summary>
        ///     Convert a TimeSpan to a digital string format: HH:mm:ss.
        /// </summary>
        /// <param name="timeSpan"></param>
        public static string DigitalTimeSpanString(this TimeSpan timeSpan)
        {
            var hoursInt = (int) timeSpan.TotalHours;
            var minutesInt = timeSpan.Minutes;
            var secondsInt = timeSpan.Seconds;

            var hours = hoursInt.ToString();
            var minutes = minutesInt.ToString();
            var seconds = secondsInt.ToString();

            if (hoursInt < 10)
                hours = "0" + hours;
            if (minutesInt < 10)
                minutes = "0" + minutes;
            if (secondsInt < 10)
                seconds = "0" + seconds;

            return hours + ":" + minutes + ":" + seconds;
        }

        /// <summary>
        ///     Swap the two items of a list.
        /// </summary>
        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            var tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        /// <summary>
        ///     Shuffles the items of a list.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list, Random? random = null)
        {
            random ??= new Random();
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}