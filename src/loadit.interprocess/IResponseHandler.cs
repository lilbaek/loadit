using Loadit.Interprocess.Models;

namespace Loadit.Interprocess
{
    /// <summary>
    ///     Handles a response message received from a remote endpoint.
    /// </summary>
    internal interface IResponseHandler
    {
        void HandleResponse(InterprocessResponse response);
    }
}