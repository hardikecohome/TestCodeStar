using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Api.Integration.Utility;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.Signature;
using DealnetPortal.Utilities.Logging;

namespace DealnetPortal.Api.Controllers
{
    [Authorize]
    [RoutePrefix("api/Profile")]
    public class ProfileController : BaseApiController
    {
        private IContractService ContractService { get; set; }
        private ICustomerFormService CustomerFormService { get; set; }

        public ProfileController(ILoggingService loggingService, IContractService contractService, ICustomerFormService customerFormService)
            : base(loggingService)
        {
            ContractService = contractService;
            CustomerFormService = customerFormService;
        }

        // GET: api/Contract
        [HttpGet]
        public IHttpActionResult GetContract()
        {
            try
            {            
                var contracts = ContractService.GetContracts(LoggedInUser.UserId);
                return Ok(contracts);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get contracts for the User {LoggedInUser.UserId}", ex);
                return InternalServerError(ex);
            }
        }

        [Route("GetCustomersContractsCount")]
        [HttpGet]
        public IHttpActionResult GetCustomersContractsCount()
        {
            try
            {
                var contracts = ContractService.GetCustomersContractsCount(LoggedInUser.UserId);
                return Ok(contracts);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get number of customers contracts for the User {LoggedInUser.UserId}", ex);
                return InternalServerError(ex);
            }
        }

        [Route("GetCompletedContracts")]
        [HttpGet]
        public IHttpActionResult GetCompletedContracts()
        {
            var contracts = ContractService.GetContracts(LoggedInUser.UserId);
            return Ok(contracts.Where(c => c.ContractState >= ContractState.Completed));
        }

        //Get: api/Contract/{contractId}
        [Route("{contractId}")]
        [HttpGet]
        public IHttpActionResult GetContract(int contractId)
        {
            var contract = ContractService.GetContract(contractId, LoggedInUser?.UserId);
            if (contract != null)
            {
                return Ok(contract);
            }
            return NotFound();
        }

        [Route("GetContracts")]
        [HttpPost]
        public IHttpActionResult GetContracts(IEnumerable<int> ids)
        {
            try
            {
                var contracts = ContractService.GetContracts(ids, LoggedInUser?.UserId);
                return Ok(contracts);                
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
       
    }
}
