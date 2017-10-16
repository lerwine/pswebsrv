using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
	/// <summary>
	/// Removes an HTTP Handler to an HTTP listener from handling requests.
	/// </summary>
    [Cmdlet(VerbsLifecycle.Unregister, "HttpHandler")]
    public class Unregister_HttpHandler : HttpHandlerCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "String which uniquely identifies an HTTP Listener.")]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// String which uniquely identifies the associated HTTP Listener.
		/// </summary>
        public override string Listener { get { return base.Listener; } set { base.Listener = value; } }

        [Parameter(ValueFromPipeline = true, HelpMessage = "Name which uniquely identifies an HTTP Handler.")]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// Name which uniquely identifies an HTTP Handler to remove.
		/// </summary>
        public override string[] Name { get { return base.Name; } set { base.Name = value; } }

        protected override void BeginProcessing()
        {
        }

        protected override void ProcessRecord()
        {
        }

        protected override void EndProcessing()
        {
        }
    }
}