using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RiasBot.Core.Modules.Administration.Commons
{
    public class MuteTime
    {
        private MuteTime () {}
        
        private static readonly Regex Regex = new Regex(@"^(?:(?<months>\d)mo)?(?:(?<weeks>\d{1,2})w)?(?:(?<days>\d{1,2})d)?(?:(?<hours>\d{1,4})h)?(?:(?<minutes>\d{1,5})m)?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);
        
        public static TimeSpan GetMuteTime(string input)
        {
            var match = Regex.Match(input);

            if (match.Length == 0)
                throw new ArgumentException("Invalid string input format.");

            var timeValues = new Dictionary<string, int>();
            
            foreach (var group in Regex.GetGroupNames())
            {
                if (group == "0") continue;
                if (!int.TryParse(match.Groups[group].Value, out var value))
                {
                    timeValues[group] = 0;
                    continue;
                }
                
                if (value < 1 ||
                    (group == "months" && value > 3) ||    //3 months maximum
                    (group == "weeks" && value > 13) ||    //3 months maximum in weeks
                    (group == "days" && value >= 90) ||    //3 months maximum in days
                    (group == "hours" && value > 2000) ||  //3 months maximum in hours
                    (group == "minutes" && value > 43000)) //1 month maximum in minutes
                {
                    throw new ArgumentException("Invalid time format value.");
                }

                timeValues[group] = value;
            }
            
            var timeSpan = new TimeSpan(30 * timeValues["months"] + 7 * timeValues["weeks"] + timeValues["days"],
                timeValues["hours"], timeValues["minutes"], 0);
                
            if (timeSpan > TimeSpan.FromDays(90))
            {
                throw new ArgumentException("The time is too long.");
            }

            return timeSpan;
        }
    }
}