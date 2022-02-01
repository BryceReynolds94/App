using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.ViewModels {
    class ActiveTimePopupViewModel : BaseViewModel {

        //Active time picker
        private Collection<DayOfWeek> activeDays;
        private TimeSpan fromTime;
        private TimeSpan toTime;
        private bool invertTime;

        public Command<string> ToggleDay { get; set; }
        public Command Confirm { get; set; }
        public Command Cancel { get; set; }

        public ActiveTimePopupViewModel(Collection<DayOfWeek> activeDays, TimeSpan fromTime, TimeSpan toTime, bool invertTime) {
            this.activeDays = activeDays;
            this.fromTime = fromTime;
            this.toTime = toTime;
            this.invertTime = invertTime;

            ToggleDay = new Command<string>((string rawDay) => toggleDay(rawDay));
            Cancel = new Command(() => RequestCancel?.Invoke(this, null));
            Confirm = new Command(() => ActiveTimeResult?.Invoke(this.activeDays, this.fromTime, this.toTime, this.invertTime));
        }

        public EventHandler RequestCancel;
        public ActiveTimeHandler ActiveTimeResult;
        public delegate void ActiveTimeHandler(Collection<DayOfWeek> activeDays, TimeSpan fromTime, TimeSpan toTime, bool invertTime);

        private void toggleDay(string rawDay) {
            int.TryParse(rawDay, out int intDay);
            DayOfWeek day = (DayOfWeek) intDay;

            if (activeDays.Contains(day)) {
                activeDays.Remove(day);
            } else {
                activeDays.Add(day);
            }
            OnPropertyChanged(nameof(ActiveDayMonday));
            OnPropertyChanged(nameof(ActiveDayTuesday));
            OnPropertyChanged(nameof(ActiveDayWednesday));
            OnPropertyChanged(nameof(ActiveDayThursday));
            OnPropertyChanged(nameof(ActiveDayFriday));
            OnPropertyChanged(nameof(ActiveDaySaturday));
            OnPropertyChanged(nameof(ActiveDaySunday));
        }

        public bool ActiveDayMonday => activeDays.Contains(DayOfWeek.Monday);
        public bool ActiveDayTuesday => activeDays.Contains(DayOfWeek.Tuesday);
        public bool ActiveDayWednesday => activeDays.Contains(DayOfWeek.Wednesday);
        public bool ActiveDayThursday => activeDays.Contains(DayOfWeek.Thursday);
        public bool ActiveDayFriday => activeDays.Contains(DayOfWeek.Friday);
        public bool ActiveDaySaturday => activeDays.Contains(DayOfWeek.Saturday);
        public bool ActiveDaySunday => activeDays.Contains(DayOfWeek.Sunday);

        public TimeSpan FromTime {
            get {
                return fromTime;
            }
            set {
                fromTime = value;
            }
        }

        public TimeSpan ToTime {
            get {
                return toTime;
            }
            set {
                toTime = value;
            }
        }

        public bool NotInvertTime {
            get => !invertTime;
            set {
                invertTime = !value;
                OnPropertyChanged(nameof(NotInvertTime));
            }
        }

    }
}
