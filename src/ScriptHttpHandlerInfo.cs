using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv
{
	public class ScriptHttpHandlerInfo : HttpHandlerInfo
	{
        public ScriptBlock Script { get; private set; }

        public ScriptHttpHandlerInfo(string name, ScriptBlock script, string virtualPath, string pattern, string exclude)
            : base(name, virtualPath, pattern, exclude)
        {
            if (script == null)
                throw new ArgumentNullException("script");
            Script = script;
        }
        
        protected override void SetResponse(HttpRequestInfo request, PSCmdlet hostCmdlet)
        {
            Collection<PSObject> results = Script.Invoke(request, VirtualPath, Name);
            if (results != null && results.Count > 0)
                hostCmdlet.WriteObject(results); 
        }

        public override bool Equals(HttpHandlerInfo other)
        {
            if (other != null && other is ScriptHttpHandlerInfo)
            {
                if (ReferenceEquals(this, other) || (NameComparer.Equals(Name, other.Name) &&
                        NameComparer.Equals(VirtualPath, other.VirtualPath) &&
                        ((Pattern == null) ? other.Pattern == null : other.Pattern != null && Pattern.ToString() == other.Pattern.ToString()) &&
                        ((Exclude == null) ? other.Exclude == null : other.Exclude != null && Exclude.ToString() == other.Exclude.ToString()) &&
                        Script.ToString() == (other as ScriptHttpHandlerInfo).Script.ToString()))
                    return true;
            }
            return false;
        }
    }
}