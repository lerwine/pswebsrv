using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Threading;

namespace Erwine.Leonard.T.PSWebSrv
{
    public class PSHttpHandlerInfo : IEquatable<PSHttpHandlerInfo>
    {
        private static readonly StringComparer NameComparer = StringComparer.InvariantCultureIgnoreCase;
        private PSHttpListener _listener;
        private PSHttpHandler _handler;

        internal PSHttpHandlerInfo(PSHttpListener listener, PSHttpHandler handler)
        {
            _listener = listener;
            _handler = handler;
            listener.Closing += PSHttpListener_Closing;
        }

        internal bool Matches(string name) { return NameComparer.Equals(name, _handler.Name); }

        public bool Equals(PSHttpHandlerInfo other)
        {
            if (other == null)
                return false;
            PSHttpListener x = _listener;
            PSHttpListener y = other._listener;
            if (x == null)
            {
                if (y != null)
                    return false;
            }
            else if (y == null || !ReferenceEquals(x, y))
                return false;
            return NameComparer.Equals(_handler.Name, other._handler.Name);
        }

        public override bool Equals(object obj) { return Equals(obj as PSHttpHandlerInfo); }

        public override int GetHashCode() { return NameComparer.GetHashCode(_handler.Name); }

        public override string ToString() { return _handler.Name; }

        private void PSHttpListener_Closing(object sender, PSHttpListenerCloseEventArgs e)
        {
            PSHttpListener listener;
            if (sender != null && (listener = _listener) != null && ReferenceEquals(sender, listener))
            {
                _listener = null;
                listener.Closing -= PSHttpListener_Closing;
            }
        }
    }
}