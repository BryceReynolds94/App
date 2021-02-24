using LanguageExt;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Types = Telega.Rpc.Dto.Types;

namespace PagerBuddy.Models {
    public class TelegramPeer : Group{

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public enum TYPE {CHANNEL, CHAT, USER};

        public Types.InputFileLocation photoLocation;

        public TelegramPeer(TYPE type, int id, string name, Types.InputFileLocation photo, long accessHash = 0) : base(name, id, type, accessHash) {
            this.photoLocation = photo;
        }

        public static Collection<TelegramPeer> getPeerCollection(Arr<Types.Chat> chatList, Arr<Types.User> userList) {
            Collection<TelegramPeer> peerList = new Collection<TelegramPeer>();


            foreach(Types.Chat chat in chatList) {
                if (chat.AsChannelTag().IsSome) {
                    Types.Chat.ChannelTag c = (Types.Chat.ChannelTag) chat.AsChannelTag();
                    Types.InputPeer peer = new Types.InputPeer.ChannelTag(c.Id, c.AccessHash.IfNone(0));
                    peerList.Add(new TelegramPeer(TYPE.CHANNEL, c.Id, c.Title, getPhotoLocation(peer, c.Photo), c.AccessHash.IfNone(0)));
                }else if (chat.AsTag().IsSome) { 
                    Types.Chat.Tag c = (Types.Chat.Tag) chat.AsTag();
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
                if(user.AsTag().IsSome) {
                    Types.User.Tag u = (Types.User.Tag) user.AsTag();

                    string userName = u.FirstName + " " + u.LastName;
                    if (userName.Length < 3) {
                        userName = (string) u.Username;
                    }
                    Types.InputPeer peer = new Types.InputPeer.UserTag(u.Id, u.AccessHash.IfNone(0));
                    peerList.Add(new TelegramPeer(TYPE.USER, u.Id, userName, getPhotoLocation(peer, u.Photo.IfNoneUnsafe(() => null)), u.AccessHash.IfNone(0)));
                } else {
                    Logger.Warn("User was of unexpected type " + user.GetType().ToString() + " and was not added to the peer list.");
                    continue;
                }
            }
            return peerList;
        }

        private static Types.InputFileLocation getPhotoLocation(Types.InputPeer peer, Types.ChatPhoto photo) {
            if (photo.AsTag().IsSome) {
                Types.FileLocation loc = photo.AsTag().IfNoneUnsafe(() => null).PhotoSmall;
                return new Types.InputFileLocation.PeerPhotoTag(false, peer, loc.VolumeId, loc.LocalId);
            }
            return null;

        }

        private static Types.InputFileLocation getPhotoLocation(Types.InputPeer peer, Types.UserProfilePhoto photo) {
            if (photo != null && photo.AsTag().IsSome) {
                Types.FileLocation loc = photo.AsTag().IfNoneUnsafe(() => null).PhotoSmall;
                return new Types.InputFileLocation.PeerPhotoTag(false, peer, loc.VolumeId, loc.LocalId);
            }
            return null;

        }

    }
}
