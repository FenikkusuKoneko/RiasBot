using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Rias.Core.Attributes;

namespace Rias.Core.Extensions
{
    public static class RiasExtensions
    {
        /// <summary>
        /// This will inject the values from the Dependency Injection into the fields marked with the InjectAttribute.
        /// Thanks Casino.
        /// </summary>
        public static void Inject(this IServiceProvider services, object obj)
        {
            var members = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0)
                .ToArray();

            foreach (var member in members)
            {
                switch (member)
                {
                    case FieldInfo fieldInfo:
                        var type = fieldInfo.FieldType;

                        var value = services.GetService(type);

                        if (value is null)
                            continue;

                        fieldInfo.SetValue(obj, value);
                        break;
                }
            }
        }

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
        ///     Shuffle the items of a list.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            var rnd = new Random();
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rnd.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}