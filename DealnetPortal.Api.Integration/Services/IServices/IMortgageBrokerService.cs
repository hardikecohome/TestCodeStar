using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Models.Contract;

namespace DealnetPortal.Api.Integration.Services
{
    public interface IMortgageBrokerService
    {
        /// <summary>
        /// Used for return contracts created by user and even assigned to other users at the moment
        /// </summary>
        /// <param name="userId">Id of an user</param>
        /// <returns></returns>
        IList<ContractDTO> GetCreatedContracts(string userId);
        Task<Tuple<ContractDTO, IList<Alert>>> CreateContractForCustomer(string contractOwnerId, NewCustomerDTO newCustomer);
    }
}
