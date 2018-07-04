using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Utilities.Logging;
// ReSharper disable All

namespace DealnetPortal.Api.Controllers
{
    [Authorize]
    [RoutePrefix("api/MortgageBroker")]
    public class MortgageBrokerController : BaseApiController
    {
        private IMortgageBrokerService _mortgageBrokerService { get; set; }

        public MortgageBrokerController(ILoggingService loggingService, IMortgageBrokerService mortgageBrokerService) : base(loggingService)
        {
            _mortgageBrokerService = mortgageBrokerService;
        }

        //[Route("GetCreatedContracts")]
        // GET: api/MortgageBroker
        [HttpGet]
        public IHttpActionResult GetCreatedContracts()
        {
            try
            {
                var contractsOffers = _mortgageBrokerService.GetCreatedContracts(LoggedInUser.UserId);
                return Ok(contractsOffers);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get contracts created by User {LoggedInUser.UserId}", ex);
                return InternalServerError(ex);
            }
        }

        //[Route("CreateContractForCustomer")]
        // POST: api/MortgageBroker
        [HttpPost]
        public async Task<IHttpActionResult> CreateContractForCustomer(NewCustomerDTO customerFormData)
        {
            var alerts = new List<Alert>();
            try
            {
                var creationResult = await _mortgageBrokerService.CreateContractForCustomer(LoggedInUser?.UserId, customerFormData);

                if (creationResult.Item1 == null)
                {
                    alerts.AddRange(creationResult.Item2);
                }
                return Ok(creationResult);
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.ContractCreateFailed,
                    Message = ex.ToString()
                });
                LoggingService.LogError(ErrorConstants.ContractCreateFailed, ex);
            }
            return Ok(new Tuple<ContractDTO, IList<Alert>>(null, alerts));
        }
    }
}