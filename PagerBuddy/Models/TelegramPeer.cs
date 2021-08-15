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

        public TelegramPeer(TYPE type, int id, string name, Types.InputFileLocation photo, long accessHash = 0) : base(name, id, type, accessHash) {
            this.photoLocation = photo;
        }

        public async Task loadImage(CommunicationService client) {
            MemoryStream file = await client.getProfilePic(photoLocation);
            if (file != null) {
                image = file;
                hasImage = image != null;

                imageLoaded.Invoke(this, null);
            } else {
                Logger.Info("Could not load peer pic.");
            }
        }

        public static Collection<TelegramPeer> getPeerCollection(IReadOnlyList<Types.Chat> chatList, IReadOnlyList<Types.User> userList) {
            Collection<TelegramPeer> peerList = new Collection<TelegramPeer>();


            foreach(Types.Chat chat in chatList) {
                if (chat.Channel != null) {
                    Types.Chat.ChannelTag c = chat.Channel;
                    Types.InputPeer peer = new Types.InputPeer.ChannelTag(c.Id, c.AccessHash.GetValueOrDefault());
                    peerList.Add(new TelegramPeer(TYPE.CHANNEL, c.Id, c.Title, getPhotoLocation(peer, c.Photo), c.AccessHash.GetValueOrDefault()));
                }else if (chat.Default != null) { 
                    Types.Chat.DefaultTag c = chat.Default;
                    if (c.MigratedTo != null) {
                        //ignore chats that have migrated to channels as they will have double occurances
                        continue;
                    }
                    Types.InputPeer peer = new Types.InputPeer.ChatTag(c.Id);
                    peerList.Add(new TelegramPeer(TYPE.CHAT, c.Id, c.Title, getPhotoLocation(peer, c.Photo)));
                } else {
                    Logger.Warn("Chat was of unexpected type " + chat.GetType().ToString() + " and was not added to the peer list.");
                    continue;
                }
            }

            foreach(Types.User user in userList) {
                if(user.Default != null) {
                    Types.User.DefaultTag u = user.Default;

                    string userName = u.FirstName + " " + u.LastName;
                    if (userName.Length < 3) {
                        userName = (string) u.Username;
                    }
                    Types.InputPeer peer = new Types.InputPeer.UserTag(u.Id, u.AccessHash.GetValueOrDefault());
                    peerList.Add(new TelegramPeer(TYPE.USER, u.Id, userName, getPhotoLocation(peer, u.Photo)));
                } else {
                    Logger.Warn("User was of unexpected type " + user.GetType().ToString() + " and was not added to the peer list.");
                    continue;
                }
            }
            return peerList;
        }

        private static Types.InputFileLocation getPhotoLocation(Types.InputPeer peer, Types.ChatPhoto photo) {
            if (photo.Default != null) {
                Types.FileLocation loc = photo.Default.PhotoSmall;
                return new Types.InputFileLocation.PeerPhotoTag(false, peer, loc.VolumeId, loc.LocalId);
            }
            return null;

        }

        private static Types.InputFileLocation getPhotoLocation(Types.InputPeer peer, Types.UserProfilePhoto photo) {
            if (photo != null && photo.Default != null) {
                Types.FileLocation loc = photo.Default.PhotoSmall;
                return new Types.InputFileLocation.PeerPhotoTag(false, peer, loc.VolumeId, loc.LocalId);
            }
            return null;

        }

    }
}
