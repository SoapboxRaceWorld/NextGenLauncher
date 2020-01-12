using GalaSoft.MvvmLight.Messaging;
using NextGenLauncher.Services.Servers;

namespace NextGenLauncher.Messages
{
    public class AuthenticationInfoUpdatedMessage : MessageBase
    {
        public AuthenticationInfoUpdatedMessage(AuthenticationInfo info)
        {
            Info = info;
        }

        public AuthenticationInfo Info { get; }
    }
}