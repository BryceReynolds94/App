using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace PagerBuddy.Services
{
    public static class ClientExceptions
    {
         public enum TException
        {
            AUTH_RESTART,
            API_ID_INVALID,
            API_ID_PUBLISHED_FLOOD,
            NETWORK_MIGRATE_X,
            PHONE_MIGRATE_X,
            PHONE_NUMBER_BANNED,
            PHONE_NUMBER_FLOOD,
            PHONE_NUMBER_INVALID,
            PHONE_PASSWORD_FLOOD,
            PHONE_PASSWORD_PROTECTED,
            PHONE_CODE_EXPIRED,
            SMS_CODE_CREATE_FAILED,
            PASSWORD_HASH_INVALID,
            YOU_BLOCKED_USER,
            MESSAGE_TOO_LONG,
            MESSAGE_EMPTY,
            UNKNOWN
        }

        public enum TStatus
        {
            WRONG_CLIENT_STATUS,
            PASSWORD_REQUIRED,
            INVALID_PHONE_NUMBER,
            NO_PHONE_NUMBER,
            INVALID_CODE,
            NO_CODE,
            INVALID_PASSWORD,
            OFFLINE,
            UNKNOWN,
            OK
        }

        public static TException getTException(string exceptionString)
        {
            //exception string from Telega contains "Unknown rpc error ({errorCode}, '{errorMessage}')."

            Match match = Regex.Match(exceptionString, "(?<= ')[A-Z_]*(?=')");

            if (!match.Success || !Enum.TryParse(match.Value, out TException parsedException)) {
                parsedException = TException.UNKNOWN;
            }
            return parsedException;
        }
    }
}
