using System;
using System.Net.Http;

namespace FireSharp
{
    public class FirebaseApiException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public HttpResponseMessage Response { get; private set; }

        public FirebaseApiException(HttpResponseMessage response)
        {
            Response = response;
        }

        public FirebaseApiException(string message, HttpResponseMessage response)
            : base(message)
        {
            Response = response;
        }

        public FirebaseApiException(string message, HttpResponseMessage response, Exception inner)
            : base(message, inner)
        {
            Response = response;
        }
    }
}