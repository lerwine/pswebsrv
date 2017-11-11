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
	[OutputType(typeof(PSHttpHandler))]
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
		/// The <seealso cref="PSHttpListener" /> to register the HTTP handler with.
		/// </summary>
		[Parameter(Mandatory = true, HelpMessage = "The PSHttpListener to register the HTTP handler with.")]
		[ValidateNotNull()]
		public PSHttpListener Listener { get; set; }

		/// <summary>
		/// ScriptBlock which will handle HTTP Requests.
		/// </summary>
		[Parameter(Mandatory = true, ParameterSetName = ParameterSetName_Script, HelpMessage = "ScriptBlock which will handle HTTP Requests.")]
		[ValidateNotNull()]
		public ScriptBlock Script { get; set; }

		/// <summary>
		/// Path to location of files to be served.
		/// </summary>
		[Parameter(Mandatory = true, ParameterSetName = ParameterSetName_Path, HelpMessage = "Path to location of files to be served.")]
		[ValidateLocalPath()]
		public string PagePath { get; set; }

		/// <summary>
		/// Location of local Data Store
		/// </summary>
		[Parameter(Mandatory = true, ParameterSetName = ParameterSetName_DataStore, HelpMessage = "Location of local Data Store")]
		[ValidateLocalPath()]
		public string DataStore { get; set; }

		/// <summary>
		/// Virtual path for requests.
		/// </summary>
		[Parameter(Mandatory = true, HelpMessage = "Virtual path for requests.")]
		[ValidateVirtualPath()]
		public string VirtualPath { get; set; }

		/// <summary>
		/// Case-insensitive pattern to match, relative to the Virtual Directory, which will be handled.
		/// </summary>
		[Parameter(HelpMessage = "Case-insensitive pattern to match, relative to the Virtual Directory, which will be handled.")]
		[ValidateRegexPattern()]
		public string Pattern { get; set; }

		/// <summary>
		/// Case-insensitive pattern to match, relative to the Virtual Directory, which will be explicitly excluded.
		/// </summary>
		[Parameter(HelpMessage = "Case-insensitive pattern to match, relative to the Virtual Directory, which will be explicitly excluded.")]
		[ValidateRegexPattern()]
		public string Exclude { get; set; }

		protected override void ProcessRecord()
		{
            try
            {
				switch (ParameterSetName)
				{
					case ParameterSetName_Path:
						WriteObject(Listener.RegisterPathHandler(this, Name, PagePath, VirtualPath, Pattern, Exclude));
						break;
					case ParameterSetName_DataStore:
						WriteObject(Listener.RegisterDataStoreHandler(this, Name, DataStore, VirtualPath, Pattern, Exclude));
						break;
					default:
						WriteObject(Listener.RegisterScriptHandler(this, Name, Script, VirtualPath, Pattern, Exclude));
						break;
				}
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "Register_HttpHandler", ErrorCategory.InvalidOperation, MyInvocation.BoundParameters));
                throw;
            }
		}
	}
}