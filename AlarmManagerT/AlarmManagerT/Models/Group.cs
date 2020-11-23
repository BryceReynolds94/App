using System;
using System.IO;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace PagerBuddy.Models
{
    public class Group
    {
        public string name;
        public int id;
        public MemoryStream image;
        public bool hasImage = false;

        public Group(string groupName, int groupID)
        {
            name = groupName;
            id = groupID;
        }

    }
}