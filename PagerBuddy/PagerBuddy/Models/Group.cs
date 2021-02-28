﻿using Newtonsoft.Json;
using PagerBuddy.Services;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PagerBuddy.Models
{
    public class Group
    {
        public string name;
        public int id;
        public long accessHash;
        
        [JsonIgnore] //do not try to serialise Stream
        public MemoryStream image = null;
        public bool hasImage = false;

        public TelegramPeer.TYPE type;

        [JsonConstructor]
        public Group(string name, int id, long accessHash, TelegramPeer.TYPE type, bool hasImage)
        {
            this.name = name;
            this.id = id;
            this.accessHash = accessHash;
            this.type = type;
            this.hasImage = hasImage;
        }

        public Group(string name, int id, TelegramPeer.TYPE type, long accessHash = 0) {
            this.name = name;
            this.id = id;
            this.accessHash = accessHash;
            this.type = type;
        }

        [JsonIgnore]
        public EventHandler imageLoaded;

    }
}