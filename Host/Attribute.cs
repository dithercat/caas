using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CaaS
{
    public class CaaSEndpoint : Attribute
    {
        public readonly string Endpoint;
        public readonly string ContentType;
        public CaaSEndpoint(string endpoint, string ctype = "text/plain")
        {
            this.Endpoint = endpoint;
            this.ContentType = ctype;
        }
    }
}
