using PagerBuddy.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Types = Telega.Rpc.Dto.Types;

namespace PagerBuddy.Models {
    public class TelegramPeer : Group{

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public enum TYPE {CHANNEL, CHAT, USER};

        public Types.InputFileLocation photoLocation;

        public TelegramPeer(TYPE type, long id, bool isMegaGroup, string name, Types.InputFileLocation photo, string pagerbuddyserver, long accessHash = 0) : base(name, id, isMegaGroup, type, pagerbuddyserver, accessHash) {
            this.photoLocation = photo;
        }

        public async Task loadImage(CommunicationService client) {
            MemoryStream file = await client.getProfilePic(photoLocation);
            if (file != null) {
                image = file;
                hasImage = image != null;
            } else {
                Logger.Info("Could not load peer pic.");
            }
        }

        public static Collection<TelegramPeer> getPeerCollection(IReadOnlyList<Types.Chat> chatList, string pagerbuddyserver) {
            Collection<TelegramPeer> peerList = new Collection<TelegramPeer>();

            foreach(Types.Chat chat in chatList) {
                if (chat.Channel != null) {
                    Types.Chat.ChannelTag c = chat.Channel;
                    Types.InputPeer peer = new Types.InputPeer.ChannelTag(c.Id, c.AccessHash.GetValueOrDefault());
                    peerList.Add(new TelegramPeer(TYPE.CHANNEL, c.Id, c.Megagroup, c.Title, getPhotoLocation(peer, c.Photo), pagerbuddyserver, c.AccessHash.GetValueOrDefault()));

                }else if (chat.Default != null) { 
                    Types.Chat.DefaultTag c = chat.Default;
                    if (c.MigratedTo != null) {
                        //ignore chats that have migrated to channels as they will have double occurances
                        continue;
                    }

                    Types.InputPeer peer = new Types.InputPeer.ChatTag(c.Id);
                    peerList.Add(new TelegramPeer(TYPE.CHAT, c.Id, false, c.Title, getPhotoLocation(peer, c.Photo), pagerbuddyserver));
                } else {
                    Logger.Warn("Chat was of unexpected type " + chat.GetType().ToString() + " and was not added to the peer list.");
                    continue;
                }
            }

            return peerList;
        }

        private static Types.InputFileLocation getPhotoLocation(Types.InputPeer peer, Types.ChatPhoto photo) {
            if (photo?.Default != null) {
                return new Types.InputFileLocation.PeerPhotoTag(false, peer, photo.Default.PhotoId);
            }
            return null;

        }

    }
}
