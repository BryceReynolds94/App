using PagerBuddy.Models;
using PagerBuddy.Resources;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.ViewModels
{
    public class ConfigureKeywordPageViewModel : BaseViewModel
    {
        private AlertConfig alertConfig;

        public Command NextClickedCommand { get; set; }

        public ConfigureKeywordPageViewModel(AlertConfig alertConfig)
        {
            this.alertConfig = alertConfig;
            Title = AppResources.ConfigureKeywordPage_Title;
            NextClickedCommand = new Command(() => Next.Invoke(this, null));

        }

        public EventHandler Next;


        public TimeSpan FromTime {
            get => alertConfig.activeTimeConfig.activeStartTime;
            set => alertConfig.activeTimeConfig.activeStartTime = value;
        }

        public TimeSpan ToTime {
            get =>  alertConfig.activeTimeConfig.activeStopTime;
            set => alertConfig.activeTimeConfig.activeStopTime = value;
        }

        public bool TriggerTypeAny {
            get => alertConfig.triggerType == AlertConfig.TRIGGER_TYPE.ANY;
            set {
                if (value)
                {
                    alertConfig.triggerType = AlertConfig.TRIGGER_TYPE.ANY;
                }
            }
        }

        public bool TriggerTypeServer {
            get => alertConfig.triggerType == AlertConfig.TRIGGER_TYPE.SERVER;
            set {
                if (value)
                {
                    alertConfig.triggerType = AlertConfig.TRIGGER_TYPE.SERVER;
                }
            }
        }
        public bool TriggerTypeKeyword {
            get => alertConfig.triggerType == AlertConfig.TRIGGER_TYPE.KEYWORD;
            set {
                if (value)
                {
                        alertConfig.triggerType = AlertConfig.TRIGGER_TYPE.KEYWORD;
                }
            }
        }
        public string KeywordText {
            get => alertConfig.triggerKeyword;
            set => alertConfig.triggerKeyword = value;
        }


        public bool TimeTypeAny {
            get => !alertConfig.timeRestriction;
            set => alertConfig.timeRestriction = !value;
        }
        public bool TimeTypeSet {
            get => alertConfig.timeRestriction;
            set => alertConfig.timeRestriction = value;
        }
        public bool MondayActive {
            get => alertConfig.activeTimeConfig.checkDay(DayOfWeek.Monday);
            set => alertConfig.activeTimeConfig.setDay(DayOfWeek.Monday, value);
        }

        public bool TuesdayActive {
            get => alertConfig.activeTimeConfig.checkDay(DayOfWeek.Tuesday);
            set => alertConfig.activeTimeConfig.setDay(DayOfWeek.Tuesday, value);
        }

        public bool WednesdayActive {
            get => alertConfig.activeTimeConfig.checkDay(DayOfWeek.Wednesday);
            set => alertConfig.activeTimeConfig.setDay(DayOfWeek.Wednesday, value);
        }

        public bool ThursdayActive {
            get => alertConfig.activeTimeConfig.checkDay(DayOfWeek.Thursday);
            set => alertConfig.activeTimeConfig.setDay(DayOfWeek.Thursday, value);
        }

        public bool FridayActive {
            get => alertConfig.activeTimeConfig.checkDay(DayOfWeek.Friday);
            set => alertConfig.activeTimeConfig.setDay(DayOfWeek.Friday, value);
        }

        public bool SaturdayActive {
            get => alertConfig.activeTimeConfig.checkDay(DayOfWeek.Saturday);
            set => alertConfig.activeTimeConfig.setDay(DayOfWeek.Saturday, value);
        }

        public bool SundayActive {
            get => alertConfig.activeTimeConfig.checkDay(DayOfWeek.Sunday);
            set => alertConfig.activeTimeConfig.setDay(DayOfWeek.Sunday, value);
        }

    }
}
