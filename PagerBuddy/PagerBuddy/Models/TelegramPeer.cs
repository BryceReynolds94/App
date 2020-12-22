using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using TeleSharp.TL;

namespace PagerBuddy.Models {
    public class TelegramPeer : Group{

        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public enum TYPE {CHANNEL, CHAT, USER};

        public TLFileLocation photoLocation;

        public TelegramPeer(TYPE type, int id, string name, TLFileLocation photoLocation) : base(name, id, type) {
            this.photoLocation = photoLocation;
        }

        public static Collection<TelegramPeer> getPeerCollection(TLVector<TLAbsChat> chatList, TLVector<TLAbsUser> userList) {
            Collection<TelegramPeer> peerList = new Collection<TelegramPeer>();

            foreach(TLAbsChat chat in chatList) {
                if(chat is TLChat) {
                    TLChat c = chat as TLChat;
                    if(c.MigratedTo != null) {
                        //ignore chats that have migrated to channels as they will have double occurances
                        continue;
                    }
                    peerList.Add(new TelegramPeer(TYPE.CHAT, c.Id, c.Title, getPhotoLocation(c.Photo)));
                }else if(chat is TLChannel) {
                    TLChannel c = chat as TLChannel;
                    peerList.Add(new TelegramPeer(TYPE.CHANNEL, c.Id, c.Title, getPhotoLocation(c.Photo)));
                } else {
                    Logger.Warn("Chat was of unexpected type " + chat.GetType().ToString() + " and was not added to the peer list.");
                    continue;
                }
            }

            foreach(TLAbsUser user in userList) {
                if(user is TLUser) {
                    TLUser u = user as TLUser;

                    string userName = u.FirstName + " " + u.LastName;
                    if (userName.Length < 3) {
                        userName = u.Username;
                    }
                    peerList.Add(new TelegramPeer(TYPE.USER, u.Id, userName, getPhotoLocation(u.Photo)));
                } else {
                    Logger.Warn("User was of unexpected type " + user.GetType().ToString() + " and was not added to the peer list.");
                    continue;
                }
            }
            return peerList;
        }

        private static TLFileLocation getPhotoLocation(TLAbsChatPhoto photo) {
            if(photo is TLChatPhoto) {
                TLChatPhoto p = photo as TLChatPhoto;
                if(p != null && p.PhotoSmall is TLFileLocation) {
                    return p.PhotoSmall as TLFileLocation;
                }
            }
            return null;
        }

        private static TLFileLocation getPhotoLocation(TLAbsUserProfilePhoto photo) {
            if (photo is TLUserProfilePhoto) {
                TLUserProfilePhoto p = photo as TLUserProfilePhoto;
                if (p != null && p.PhotoSmall is TLFileLocation) {
                    return p.PhotoSmall as TLFileLocation;
                }
            }
            return null;
        }

    }
}
