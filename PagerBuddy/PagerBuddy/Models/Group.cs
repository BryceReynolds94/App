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
        
        [JsonIgnore] //do not try to serialise MemoryStream
        public MemoryStream image;
        public bool hasImage = false;

        public TelegramPeer.TYPE type;

        [JsonConstructor]
        public Group(string name, int id, TelegramPeer.TYPE type, bool hasImage)
        {
            this.name = name;
            this.id = id;
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