using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using NextGenLauncher.Data;

namespace NextGenLauncher.Messages
{
    public class ServerListUpdatedMessage : MessageBase
    {
        public ServerListUpdatedMessage(IList<Server> newList)
        {
            NewList = newList;
        }

        public IList<Server> NewList { get; }
    }
}