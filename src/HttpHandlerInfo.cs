using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Text.RegularExpressions;

namespace Erwine.Leonard.T.PSWebSrv
{
    public abstract class HttpHandlerInfo : IEquatable<HttpHandlerInfo>, IEquatable<string>
    {
        protected static readonly StringComparer NameComparer = StringComparer.InvariantCultureIgnoreCase;
        private static readonly Regex MultiWhiteSpaceRegex = new Regex(@"\s{2,}| *(?=\s)([\r\n]|[^ ])[\r\n\s]*", RegexOptions.Compiled);
        
        public string Name { get; private set; }
        
        public string VirtualPath { get; private set; }
        
        public Regex Pattern { get; private set; }
        
        public Regex Exclude { get; private set; }

        protected HttpHandlerInfo(string name, string virtualPath, string pattern, string exclude)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            name = name.Trim();
            Name = (MultiWhiteSpaceRegex.IsMatch(name)) ? MultiWhiteSpaceRegex.Replace(name, " ") : name;

            if (virtualPath == null)
                VirtualPath = "/";
            else
            {
                if (virtualPath.Contains('\\'))
                    virtualPath = virtualPath.Replace('\\', '/');
                if (virtualPath.Length == 0 || virtualPath[0] != '/')
                    virtualPath = "/" + virtualPath;
                if (virtualPath[virtualPath.Length - 1] != '/')
                    virtualPath += "/";
                VirtualPath = virtualPath;
            }
            if (String.IsNullOrEmpty(pattern))
                Pattern = null;
            else
                Pattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            if (String.IsNullOrEmpty(exclude))
                Exclude = null;
            else
                Exclude = new Regex(exclude, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public virtual bool CanHandle(HttpRequestInfo request, PSCmdlet hostCmdlet)
        {
            try
            {
                if (request == null || request.ListenerRequest == null)   
                    return false;
                string fullPath = request.ParentPath + request.LeafName + request.LeafExtension + "/";
                if (fullPath.Length < VirtualPath.Length || !NameComparer.Equals(VirtualPath, fullPath.Substring(0, VirtualPath.Length)))
                    return false;
                fullPath = request.ParentPath + request.LeafName + request.LeafExtension;
                if (Pattern != null && !Pattern.IsMatch(fullPath))
                    return false;
                return Exclude == null || !Exclude.IsMatch(fullPath);
            }
            catch (Exception e)
            {
                hostCmdlet.WriteError(new ErrorRecord(e, "CanHandle", ErrorCategory.ReadError, hostCmdlet.MyInvocation.BoundParameters));
            }
            return false;
        }

        public bool SendResponse(HttpRequestInfo request, PSCmdlet hostCmdlet)
        {
            bool success = false;
            try
            {
                if (request.ListenerResponse == null)
                    return false;
                try
                {
                    SetResponse(request, hostCmdlet);
                    success = true;
                }
                finally { request.ListenerResponse.Close(); }
            }
            catch (Exception e)
            {
                success = false;
                hostCmdlet.WriteError(new ErrorRecord(e, "SendResponse", ErrorCategory.ReadError, hostCmdlet.MyInvocation.BoundParameters));
            }
            return success;
        }

        protected abstract void SetResponse(HttpRequestInfo request, PSCmdlet hostCmdlet);

        public abstract bool Equals(HttpHandlerInfo other);

        public bool Equals(string other)
        {
            if (other == null)
                return false;
            other = other.Trim();
            return NameComparer.Equals((MultiWhiteSpaceRegex.IsMatch(other)) ? MultiWhiteSpaceRegex.Replace(other, " ") : other, Name);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is string)
                return Equals(obj as string);
            return Equals(obj as HttpHandlerInfo);
        }

        public override int GetHashCode() { return NameComparer.GetHashCode(Name); }

        public override string ToString() { return Name; }
    }
}