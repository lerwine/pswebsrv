using System;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class PSHttpListenerCloseEventArgs : EventArgs
    {
        public bool IsAbort { get; private set; }
        public PSHttpListenerCloseEventArgs(bool isAbort) { IsAbort = isAbort; }
    }
}