using AlarmManagerT.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using AlarmManagerT.Interfaces;
using INavigation = AlarmManagerT.Interfaces.INavigation;
using AlarmManagerT.Models;
using System.IO;
using Plugin.SimpleAudioPlayer;

namespace AlarmManagerT.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AlertPage : ContentPage
    {
        public static readonly string soundFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Resources.Sounds.pagerbuddy_sound.mp3");

        AlertPageViewModel viewModel;

        ISimpleAudioPlayer audioPlayer;

        private int groupID;

        public AlertPage(Alert alert)
        {
            InitializeComponent();

            this.groupID = alert.chatID;
            BindingContext = viewModel = new AlertPageViewModel(alert.title, alert.text, alert.configID, alert.hasPic);
            viewModel.RequestCancel += cancel;
            viewModel.RequestConfirm += confirm;

            startAnimation();
            playSound(); //TODO: Testing
        }

        private void playSound() {
            audioPlayer = CrossSimpleAudioPlayer.Current;
            audioPlayer.Load(soundFile);
            audioPlayer.Loop = true;
            audioPlayer.Play();
        }

        private void stopSound() {
            if(audioPlayer != null && audioPlayer.IsPlaying) {
                audioPlayer.Stop();
            }
        }

        private async void startAnimation() {
            await MainIcon.ScaleTo(1.3, 1000);
            await MainIcon.ScaleTo(1, 1000);
            //await Task.Delay(500);
            startAnimation();
        }

        private void cancel(object sender, EventArgs args)
        {
            stopSound();
            INavigation nav = DependencyService.Get<INavigation>();
            nav.quitApplication();
        }

        private void confirm(object sender, EventArgs args)
        {
            stopSound();
            INavigation nav = DependencyService.Get<INavigation>();
            nav.navigateTelegramChat(groupID);
        }
    }
}