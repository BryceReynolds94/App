using Newtonsoft.Json;
using PagerBuddy.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PagerBuddy.Models
{
    public class ActiveTimeConfig
    {
        [JsonProperty]
        private List<DayOfWeek> activeDays;

        public TimeSpan activeStartTime = new TimeSpan(10, 0, 0);
        public TimeSpan activeStopTime = new TimeSpan(22, 0, 0);

        public bool flipActiveTime = false;
        
        [JsonConstructor]
        public ActiveTimeConfig(List<DayOfWeek> activeDays, TimeSpan activeStartTime, TimeSpan activeStopTime, bool flipActiveTime) {
            this.activeDays = activeDays;
            this.activeStartTime = activeStartTime;
            this.activeStopTime = activeStopTime;
            this.flipActiveTime = flipActiveTime;
        }

        public ActiveTimeConfig() {
            activeDays = new List<DayOfWeek> {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            };
        }

        public void setDay(DayOfWeek day, bool dayActive)
        {
            if (!dayActive)
            {
                activeDays.Remove(day);
            }
            else if(!checkDay(day))
            {
                activeDays.Add(day);
                activeDays.Sort();
            }
        }

        public bool checkDay(DayOfWeek day) {
            return activeDays.Contains(day);
        }

        public bool isActiveTime(DateTime compareTime)
        {
           bool result = false;
           if (checkDay(compareTime.DayOfWeek))
            {
                bool inTime = compareTime.TimeOfDay < activeStopTime && compareTime.TimeOfDay > activeStartTime;
                bool flipAsOverDayBorder = activeStartTime > activeStopTime;

                result = inTime ^ flipAsOverDayBorder;
            }
            return result ^ flipActiveTime;
        }

        public string getActiveString()
        {
            string outString = "";

            if (flipActiveTime) {
                outString = AppResources.HomeStatusPage_FlipTime_Prefix + " ";
            }

            foreach (DayOfWeek day in activeDays)
            {
                DateTime dateTime = new DateTime(1,1,(int) day + 7);
                string days = dateTime.ToString("ddd");
                outString = outString + days  + ", ";
            }
            return outString + activeStartTime.ToString(@"hh\:mm") + " - " + activeStopTime.ToString(@"hh\:mm");
        }

    }
}
