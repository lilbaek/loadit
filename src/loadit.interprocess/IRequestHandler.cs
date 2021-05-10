using Loadit.Interprocess.Models;

namespace Loadit.Interprocess
{
    internal interface IRequestHandler
    {
        /// <summary>
        ///     Handles a request message received from a remote endpoint.
        /// </summary>
        void HandleRequest(InterprocessRequest request);
    }
}