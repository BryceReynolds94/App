using System;
using System.IO;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace AlarmManagerT.Models
{
    public class Group
    {
        public string name;
        public int id;
        public MemoryStream image;
        public bool hasImage = false;

        public int lastMessageID = 0;

        public Group(string groupName, int groupID)
        {
            name = groupName;
            id = groupID;
        }

    }
}