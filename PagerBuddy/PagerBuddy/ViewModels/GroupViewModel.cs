using PagerBuddy.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Xamarin.Forms;
using FFImageLoading.Svg.Forms;
using FFImageLoading.Forms;

namespace PagerBuddy.ViewModels {
    public class GroupViewModel {
        public Group group;

        public GroupViewModel(Group group) {
            this.group = group;
        }
        public string Name => group.name;

        public ImageSource GroupPic {
            get {
                if (group.hasImage) {
                    return ImageSource.FromStream(() => group.image);
                } else {
                    return SvgImageSource.FromResource("PagerBuddy.Resources.Images.group_default.svg");
                }
            }
        }
    }
}
