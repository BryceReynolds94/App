using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Xamarin.Forms;
using FFImageLoading.Svg.Forms;
using FFImageLoading.Forms;

namespace PagerBuddy.ViewModels {
    public class GroupViewModel : BaseViewModel{
        public Group group;

        public GroupViewModel(Group group) {
            this.group = group;
            group.imageLoaded += ImageUpdated;
        }
        public string Name => group.name;

        private void ImageUpdated(object sender, EventArgs args) {
            OnPropertyChanged(nameof(GroupPic));
        }

        public ImageSource GroupPic {
            get {
                if (group.hasImage) {
                    return ImageSource.FromStream(() => new MemoryStream(group.image.ToArray()));
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.group_default.svg");
                }
            }
        }
    }
}
