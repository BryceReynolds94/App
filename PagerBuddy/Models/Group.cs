using Newtonsoft.Json;
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
        public long id;
        public long accessHash;
        public bool isMegaGroup;

        public string pagerbuddyserver = "pagerbuddyserverbot"; //default to make legacy transition easier
        
        [JsonIgnore] //do not try to serialise Stream
        public MemoryStream image = null;
        public bool hasImage = false;

        public TelegramPeer.TYPE type;

        public long serverID {
            get {
                long extendedID = id;
                if (isMegaGroup) { //This seems a bit hacky - watch this closely for breaking changes in the future
                    extendedID += (long)100E10;
                }
                if(type == TelegramPeer.TYPE.CHANNEL || type == TelegramPeer.TYPE.CHAT) {
                    extendedID *= -1;
                }
                return extendedID; //Server needs chat ID notation with "-", and appended 100 for megagroups
            }
        }

        [JsonConstructor]
        public Group(string name, long id, bool isMegaGroup, long accessHash, TelegramPeer.TYPE type, bool hasImage, string pagerbuddyserver = "pagerbuddyserverbot")
        {
            this.name = name;
            this.id = id;
            this.isMegaGroup = isMegaGroup;
            this.accessHash = accessHash;
            this.type = type;
            this.hasImage = hasImage;
            this.pagerbuddyserver = pagerbuddyserver;
        }

        public Group(string name, long id, bool isMegaGroup, TelegramPeer.TYPE type, string pagerbuddyserver, long accessHash = 0) {
            this.name = name;
            this.id = id;
            this.isMegaGroup = isMegaGroup;
            this.accessHash = accessHash;
            this.type = type;
            this.pagerbuddyserver=pagerbuddyserver;
        }


    }
}