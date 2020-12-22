using Newtonsoft.Json;
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

        public TelegramPeer.TYPE type;

        [JsonConstructor]
        public Group(string groupName, int groupID, TelegramPeer.TYPE type, bool hasImage)
        {
            name = groupName;
            id = groupID;
            this.type = type;
            this.hasImage = hasImage;
        }

        public Group(TelegramPeer peer) {
            name = peer.name;
            id = peer.id;
            type = peer.type;
        }

    }
}