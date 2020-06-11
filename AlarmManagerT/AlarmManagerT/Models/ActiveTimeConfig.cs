using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace AlarmManagerT.Models
{
    public class ActiveTimeConfig
    {
        public Collection<DayOfWeek> activeDays = new Collection<DayOfWeek> {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };

        public TimeSpan activeStartTime = TimeSpan.MinValue;
        public TimeSpan activeStopTime = TimeSpan.MaxValue;

        public void setDay(DayOfWeek day, bool dayActive)
        {
            bool daySet = activeDays.Contains(day);

            if (daySet && !dayActive)
            {
                activeDays.Remove(day);
            }
            else if(!daySet && dayActive)
            {
                activeDays.Add(day);
            }
        }

        public bool checkDay(DayOfWeek day)
        {
            return activeDays.Contains(day);
        }

        public bool isActiveTime(DateTime compareTime)
        {
            if (checkDay(compareTime.DayOfWeek))
            {
                bool beforeStopTime = compareTime.TimeOfDay < activeStopTime;
                bool afterStartTime = compareTime.TimeOfDay > activeStartTime;

                if(afterStartTime && beforeStopTime)
                {
                    return true;
                }
            }
            return false;
        }

        public string getActiveString()
        {
            string outString = "";
            foreach (DayOfWeek day in activeDays)
            {
                outString = outString + day.ToString("dd") + ", ";
            }
            return outString + activeStartTime.ToString("hh:mm - ") + activeStopTime.ToString("hh:mm");
        }

    }
}
