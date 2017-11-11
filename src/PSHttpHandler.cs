using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Erwine.Leonard.T.PSWebSrv
{
    public abstract class PSHttpHandler
    {
        private Uri _virtualPath;
        private Regex _patternRegex;
        private Regex _excludeRegex;

        public string Name { get; set; }

        protected PSHttpHandler(string name, string virtualPath, string pattern, string exclude)
        {
            name = (name == null) ? "" : name.Trim();
            Uri uri;
            ValidateVirtualPathAttribute.AssertPath(virtualPath, out uri);
            _virtualPath = uri;
            Regex regex;
            ValidateRegexPatternAttribute.AssertRegexPattern(pattern, true, true, out regex);
            _patternRegex = regex;
            ValidateRegexPatternAttribute.AssertRegexPattern(pattern, true, true, out regex);
            _excludeRegex = regex;
        }

        internal virtual void OnRegistered(PSCmdlet hostCmdlet, PSHttpListener listener)
        {

        }
    }
}