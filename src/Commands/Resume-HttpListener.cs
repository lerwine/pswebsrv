using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Class for Resume-HttpListener Cmdlet.
    /// </summary>
	[Cmdlet(VerbsLifecycle.Resume, "HttpListener")]
	public class Resume_HttpListener : HttpListenerCmdlet
	{
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "String which uniquely identifies the HTTP Listener to resume.")]
        [ValidateNotNullOrEmpty()]
		/// <summary>
		/// String which uniquely identifies the HTTP Listener to resume.
		/// </summary>
        public override string[] ID { get; set; }

		protected override void ProcessRecord()
		{
            foreach (HttpListenerInfo item in GetListeners().ToArray())
			{
				if (item.Resume(this))
                	WriteObject(item);
			}
		}
	}
}