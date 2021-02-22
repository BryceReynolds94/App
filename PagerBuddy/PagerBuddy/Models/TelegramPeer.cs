using LanguageExt;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Types = Telega.Rpc.Dto.Types;

namespace PagerBuddy.Models {
    public class TelegramPeer : Group{

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public enum TYPE {CHANNEL, CHAT, USER};

        public Types.FileLocation photoLocation;

        public TelegramPeer(TYPE type, int id, string name, Types.FileLocation photo, long accessHash = 0) : base(name, id, type, accessHash) {
            this.photoLocation = photo;
        }

        public static Collection<TelegramPeer> getPeerCollection(Arr<Types.Chat> chatList, Arr<Types.User> userList) {
            Collection<TelegramPeer> peerList = new Collection<TelegramPeer>();

            foreach(Types.Chat chat in chatList) {
                if (chat.AsChannelTag().IsSome) {
                    Types.Chat.ChannelTag c = (Types.Chat.ChannelTag) chat.AsChannelTag();
                    peerList.Add(new TelegramPeer(TYPE.CHANNEL, c.Id, c.Title, getPhotoLocation(c.Photo), c.AccessHash.IfNone(0)));
                }else if (chat.AsTag().IsSome) { 
                    Types.Chat.Tag c = (Types.Chat.Tag) chat.AsTag();
                    if (c.MigratedTo != null) {
                        //ignore chats that have migrated to channels as they will have double occurances
                        continue;
                    }
                    peerList.Add(new TelegramPeer(TYPE.CHAT, c.Id, c.Title, getPhotoLocation(c.Photo)));
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
                    peerList.Add(new TelegramPeer(TYPE.USER, u.Id, userName, getPhotoLocation(u.Photo.IfNoneUnsafe(() => null)), u.AccessHash.IfNone(0)));
                } else {
                    Logger.Warn("User was of unexpected type " + user.GetType().ToString() + " and was not added to the peer list.");
                    continue;
                }
            }
            return peerList;
        }

        private static Types.FileLocation getPhotoLocation(Types.ChatPhoto photo) {
            if (photo != null && photo.AsTag().IsSome) {
                Types.ChatPhoto.Tag photoTag = (Types.ChatPhoto.Tag)photo.AsTag();
                if (photoTag.PhotoSmall != null) {
                    return photoTag.PhotoSmall;
                }
            }
            return null;
        }

        private static Types.FileLocation getPhotoLocation(Types.UserProfilePhoto photo) {
            if (photo != null && photo.AsTag().IsSome) {
                Types.UserProfilePhoto.Tag photoTag = (Types.UserProfilePhoto.Tag) photo.AsTag();
                if (photoTag.PhotoSmall != null) {
                    return photoTag.PhotoSmall;
                }
            }
            return null;
        }

    }
}
