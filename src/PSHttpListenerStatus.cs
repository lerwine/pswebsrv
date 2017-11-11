namespace Erwine.Leonard.T.PSWebSrv
{
    public enum PSHttpListenerStatus
    {
        /// The <seealso cref="PSHttpListener" /> is not listening for incoming requests.
        Stopped,
        
        /// The <seealso cref="PSHttpListener" /> is listening for incoming requests.
        Listening,
        
        /// The <seealso cref="PSHttpListener" /> has an unprocessed incoming request.
        Received,
        
        /// The <seealso cref="PSHttpListener" /> has been shut down.
        Closed
    }
}