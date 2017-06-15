using System;
using EtsyServicer.DomainObjects;

namespace EtsyServices
{
    public static class Extensions
    {
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);
        }

        public static bool IsValid(this AuthToken _token)
        {
            if (_token != null)
            {
                if (!_token.ApiKey.IsNullOrEmpty() && !_token.SharedSecret.IsNullOrEmpty() &&
                    !_token.Key.IsNullOrEmpty() && !_token.AuthTokenSecret.IsNullOrEmpty())
                {
                    return true;
                }
            }
            return false;
        }

        public static string StringValue(this Enum value)
        {
            if (value == null)
            {
                throw new Exception("Unable to get string value from enum.");
            }

            return StringEnum.GetStringValue(value);
        }
    }
}
