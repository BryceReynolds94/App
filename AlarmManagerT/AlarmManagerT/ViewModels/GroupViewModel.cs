using AlarmManagerT.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Xamarin.Forms;

namespace AlarmManagerT.ViewModels
{
    public class GroupViewModel
    {
        public Group group;

        public GroupViewModel(Group group)
        {
            this.group = group;
        }

        public string Name => group.name;
        public bool ShowCustomPic => group.hasImage;
        public bool ShowDefaultPic => !group.hasImage;

        public ImageSource GroupPic {
            get {

                if (group.image == null)
                {
                    return null;
                }

                return ImageSource.FromStream(() => group.image);
            }
        }
    }
}
