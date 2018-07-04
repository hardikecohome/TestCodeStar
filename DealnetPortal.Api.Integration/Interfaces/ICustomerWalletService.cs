using System.Collections.Generic;
using System.Threading.Tasks;
using DealnetPortal.Api.Core.Types;

namespace DealnetPortal.Api.Integration.Interfaces
{
    public interface ICustomerWalletService
    {
        Task<IList<Alert>> CreateCustomerByContractList(List<Domain.Contract> contracts, string contractOwnerId);
        Task<IList<Alert>> CheckCustomerExisting(string login);
    }
}
