using System;

namespace LykkePublicAPI
{
    public class AppSettingException : Exception
    {
        public AppSettingException() { }
        public AppSettingException(string message) : base(message) { }
        public AppSettingException(string message, Exception inner) : base(message, inner) { }
    }
}
