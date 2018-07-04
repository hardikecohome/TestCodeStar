using System;

namespace DealnetPortal.Aspire.Integration.Models
{
    [Serializable]
    public class RequestHeader
    {
        public From From { get; set; }

        public string UserId { get; set; }
        public string Password { get; set; }

        public string Status { get; set; }        
    }

    [Serializable]
    public class ResponseHeader
    {        
        public To To { get; set; }

        public string UserId { get; set; }
        public string Password { get; set; }

        public string Status { get; set; }
        public string Code { get; set; }
        public string ErrorMsg { get; set; }
        public string Message { get; set; }
    }

    [Serializable]
    public class From
    {
        public string Domain { get; set; }
        public string AccountNumber { get; set; }
        public string Password { get; set; }
    }

    [Serializable]
    public class To
    {
        public string Domain { get; set; }
    }
}
