using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace AlarmManagerT.Models
{
    public class ActiveTimeConfig
    {
        public List<DayOfWeek> activeDays;

        public TimeSpan activeStartTime = new TimeSpan(10, 0, 0);
        public TimeSpan activeStopTime = new TimeSpan(22, 0, 0);

        
        public void initDays() {
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
            if (!dayActive) //TODO: Check this - seems to be wrong logic (day stackup)
            {
                foreach (DayOfWeek setDay in activeDays) {
                    if (setDay.ToString().Equals(day.ToString())) {
                        activeDays.Remove(setDay);
                        return;
                    }
                }
            }
            else if(!checkDay(day))
            {
                activeDays.Add(day);
            }
        }

        public bool checkDay(DayOfWeek day) {
            foreach(DayOfWeek setDay in activeDays) {
                if(setDay.ToString().Equals(day.ToString())) {
                    return true;
                }
            }
            return false;
        }

        public bool isActiveTime(DateTime compareTime)
        {
           if (checkDay(compareTime.DayOfWeek))
            {
                bool beforeStopTime;
                bool afterStartTime;
                if (activeStartTime > activeStopTime) {
                    beforeStopTime = compareTime.TimeOfDay < activeStartTime;
                    afterStartTime = compareTime.TimeOfDay > activeStopTime;
                } else {
                    beforeStopTime = compareTime.TimeOfDay < activeStopTime;
                    afterStartTime = compareTime.TimeOfDay > activeStartTime;
                }
                
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
                DateTime dateTime = new DateTime(1,1,(int) day + 7);
                string days = dateTime.ToString("ddd");
                outString = outString + days  + ", ";
            }
            return outString + activeStartTime.ToString(@"hh\:mm") + " - " + activeStopTime.ToString(@"hh\:mm"); //TODO: Possibly make this more compact - deal with field overflow in XAML
        }

    }
}
