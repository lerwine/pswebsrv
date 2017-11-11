using System;
using System.Linq;
using System.Management.Automation;
using System.Net;

namespace Erwine.Leonard.T.PSWebSrv.Commands
{
    /// <summary>
    /// Creates and registers a new <seealso cref="HttpListener" />.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "HttpListener")]
    [OutputType(typeof(PSHttpListener))]
    public class New_HttpListener : PSCmdlet
    {
        [Parameter(HelpMessage = "The realm, or resource partition, associated with this HttpListener object.")]
        [ValidateNotNullOrEmpty()]
        public string Realm { get; set; }

        [Parameter(HelpMessage = "When NTLM is used, additional requests using the same Transmission Control Protocol (TCP) connection are required to authenticate.")]
        public SwitchParameter UnsafeConnectionNtlmAuthentication { get; set; }

        [Parameter(HelpMessage = "Ignore any exceptions that occur when the internal HttpListener sends the response to the client.")]
        public SwitchParameter IgnoreWriteExceptions { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                PSHttpListener listener = new PSHttpListener();
                if (!String.IsNullOrEmpty(Realm))
                {
                    try { listener.Realm = Realm; }
                    catch (Exception err)
                    {
                        WriteError(new ErrorRecord(err, "HttpListener_Set_Realm", ErrorCategory.InvalidArgument, Realm));
                        return;
                    }
                }
                try { listener.IgnoreWriteExceptions = IgnoreWriteExceptions.IsPresent; }
                catch (Exception err)
                {
                    WriteError(new ErrorRecord(err, "HttpListener_Set_IgnoreWriteExceptions", ErrorCategory.InvalidArgument, IgnoreWriteExceptions.IsPresent));
                    return;
                }
                try { listener.UnsafeConnectionNtlmAuthentication = UnsafeConnectionNtlmAuthentication.IsPresent; }
                catch (Exception err)
                {
                    WriteError(new ErrorRecord(err, "HttpListener_Set_UnsafeConnectionNtlmAuthentication", ErrorCategory.InvalidArgument, UnsafeConnectionNtlmAuthentication.IsPresent));
                    return;
                }
                WriteObject(listener);
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "New_HttpListener", ErrorCategory.OpenError, MyInvocation.BoundParameters));
                throw;
            }
        }
    }
}