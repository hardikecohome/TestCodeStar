using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Common.Constants
{
    public static class ErrorCodes
    {
        public const int CantGetContractFromDb = 101;
        public const int FailedToUpdateContract = 102;
        public const int FailedToUpdateSettings = 103;
        public const int CantGetUserFromDb = 104;
        public const int ContractCreateFailed = 105;

        public const int AspireConnectionFailed = 1001;
        public const int AspireTransactionNotCreated = 1002;

        public const int AspireDatabaseConnectionFailed = 2001;
    }
}
