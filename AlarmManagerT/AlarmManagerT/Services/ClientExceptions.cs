using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PagerBuddy.Services
{
    public static class ClientExceptions
    {
         public enum TException
        {
            AUTH_RESTART,

            API_ID_INVALID,
            API_ID_PUBLISHED_FLOOD,
            //BOT_METHOD_INVALID,
            //INPUT_REQUEST_TOO_LONG,
            NETWORK_MIGRATE_X,
            PHONE_MIGRATE_X,
            //PHONE_NUMBER_APP_SIGNUP_FORBIDDEN,
            PHONE_NUMBER_BANNED,
            PHONE_NUMBER_FLOOD,
            PHONE_NUMBER_INVALID,
            PHONE_PASSWORD_FLOOD,
            PHONE_PASSWORD_PROTECTED,
            SMS_CODE_CREATE_FAILED,
            PASSWORD_HASH_INVALID,

            UNKNOWN,
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
            UNKNOWN,
            OK

        }

        public static TException getTException(string exceptionString)
        {
            TException parsedException;
            if(!Enum.TryParse(exceptionString, out parsedException))
            {
                parsedException = TException.UNKNOWN;
            }
            return parsedException;
        }
    }
}
