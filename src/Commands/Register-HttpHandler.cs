using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
	/// <summary>
	/// Adds an HTTP Handler to an HTTP listener for handling requests.
	/// </summary>
	[Cmdlet(VerbsLifecycle.Register, "HttpHandler", DefaultParameterSetName = ParameterSetName_Script)]
	public class Register_HttpHandler : PSCmdlet
	{
		public const string ParameterSetName_Script = "Script";
		public const string ParameterSetName_Path = "Path";
		public const string ParameterSetName_DataStore = "DataStore";

		/// <summary>
		/// Name which will uniquely identify the HTTP handler.
		/// </summary>
		[Parameter(Mandatory = true, HelpMessage = "Name which will uniquely identify the HTTP handler.")]
		[ValidateNotNullOrEmpty()]
		public string Name { get; set; }

		/// <summary>
		/// String which identifies the associated HTTP listener.
		/// </summary>
		[Parameter(Mandatory = true, HelpMessage = "String which identifies the associated HTTP listener.")]
		[ValidateNotNullOrEmpty()]
		public string Listener { get; set; }

		/// <summary>
		/// ScriptBlock which will handle HTTP Requests.
		/// </summary>
		[Parameter(Mandatory = true, ParameterSetName = ParameterSetName_Script, HelpMessage = "ScriptBlock which will handle HTTP Requests.")]
		[ValidateNotNullOrEmpty()]
		public ScriptBlock Script { get; set; }

		/// <summary>
		/// Path to location of files to be served.
		/// </summary>
		[Parameter(Mandatory = true, ParameterSetName = ParameterSetName_Path, HelpMessage = "Path to location of files to be served.")]
		[ValidateNotNullOrEmpty()]
		public string PagePath { get; set; }

		/// <summary>
		/// Location of local Data Store
		/// </summary>
		[Parameter(Mandatory = true, ParameterSetName = ParameterSetName_DataStore, HelpMessage = "Location of local Data Store")]
		[ValidateNotNullOrEmpty()]
		public string DataStore { get; set; }

		/// <summary>
		/// Virtual path for requests.
		/// </summary>
		[Parameter(HelpMessage = "Virtual path for requests.")]
		[ValidateNotNullOrEmpty()]
		public string VirtualPath { get; set; }

		/// <summary>
		/// Case-insensitive pattern to match, relative to the Virtual Directory, which will be handled.
		/// </summary>
		[Parameter(HelpMessage = "Case-insensitive pattern to match, relative to the Virtual Directory, which will be handled.")]
		[ValidateNotNullOrEmpty()]
		public string Pattern { get; set; }

		/// <summary>
		/// Case-insensitive pattern to match, relative to the Virtual Directory, which will be explicitly excluded.
		/// </summary>
		[Parameter(HelpMessage = "Case-insensitive pattern to match, relative to the Virtual Directory, which will be explicitly excluded.")]
		[ValidateNotNullOrEmpty()]
		public string Exclude { get; set; }

		protected override void ProcessRecord()
		{
            try
            {
				HttpListenerInfo listener = HttpListenerInfo.Get(this, Listener);
				if (listener == null)
					return;
				
				HttpHandlerInfo handler;
				switch (ParameterSetName)
				{
					case ParameterSetName_DataStore:
						handler = new DataStoreHttpHandlerInfo(Name, DataStore, VirtualPath, Pattern, Exclude);
						break;
					case ParameterSetName_Path:
						handler = new PathHttpHandlerInfo(Name, PagePath, VirtualPath, Pattern, Exclude);
						break;
					default:
						handler = new ScriptHttpHandlerInfo(Name, Script, VirtualPath, Pattern, Exclude);
						break;
				}
				
				if (listener.RegisterHandler(handler))
					WriteObject(handler);
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "RegisterHttpHandler", ErrorCategory.ReadError, MyInvocation.BoundParameters));
                throw;
            }
		}
	}
}