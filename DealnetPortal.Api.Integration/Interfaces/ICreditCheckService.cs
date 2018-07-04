using System;
using System.Collections.Generic;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Models.Contract;

namespace DealnetPortal.Api.Integration.Interfaces
{
    public interface ICreditCheckService
    {
        Tuple<CreditCheckDTO, IList<Alert>> ContractCreditCheck(int contractId, string contractOwnerId);

        CustomerCreditReportDTO CheckCustomerCreditReport(int contractId, string contractOwnerId);
    }
}
