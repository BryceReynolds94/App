using FFImageLoading.Svg.Forms;
using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace PagerBuddy.ViewModels
{
    public class AlertPageViewModel : BaseViewModel
    {
        public Command Cancel { get; set; }
        public Command Confirm { get; set; }

        public Dictionary<string, string> colorSetWhite => new Dictionary<string, string>() { { "black", Color.White.ToHex() } };

        private readonly bool hasPic;
        private readonly string picFile;

        public AlertPageViewModel(string alertTitle, string alertText, string alertID, bool hasPic)
        {
            this.hasPic = hasPic;
            this.picFile = DataService.profilePicSavePath(alertID);

            setAlertInfo(alertTitle, alertText);

            Cancel = new Command(() => RequestCancel?.Invoke(this, null));
            Confirm = new Command(() => RequestConfirm?.Invoke(this, null));
        }

        public EventHandler RequestCancel;
        public EventHandler RequestConfirm;

        private void setAlertInfo(string alertTitle, string alertText)
        {
            AlertTitle = alertTitle;
            AlertText = alertText;

            OnPropertyChanged(nameof(AlertText));
            OnPropertyChanged(nameof(AlertTitle));
        }

        public ImageSource GroupPic {
            get {
                if (hasPic) {
                    return ImageSource.FromFile(picFile);
                } else {
                    SvgImageSource source = SvgImageSource.FromResource("PagerBuddy.Resources.Images.group_default.svg");
                    Color accent = (Color)Application.Current.Resources["AccentColor"];
                    source.ReplaceStringMap = new Dictionary<string, string>() { { "black", accent.ToHex() } };
                    return source;
                }
            }
        }
        public string AlertText { get; private set; }
        public string AlertTitle { get; private set; }

        public ImageSource ClearPic => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_clear.svg");
        public ImageSource ConfirmPic => SvgImageSource.FromResource("PagerBuddy.Resources.Images.icon_confirm.svg");

    }
}
