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

namespace DealnetPortal.Api.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("api/CustomerForm")]
    public class CustomerFormController : BaseApiController
    {
        private ICustomerFormService _customerFormService { get; set; }

        public CustomerFormController(ILoggingService loggingService, ICustomerFormService customerFormService) 
            : base(loggingService)
        {
            _customerFormService = customerFormService;
        }
        
        [HttpPost]
        public IHttpActionResult SubmitCustomerForm(CustomerFormDTO customerFormData)
        {
            try
            {
                var submitResult = _customerFormService.SubmitCustomerFormData(customerFormData);
                return Ok(submitResult);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("{contractId}/{dealerName}")]
        [Route("{contractId}")]
        [HttpGet]
        public IHttpActionResult GetCustomerContractInfo(int contractId, string dealerName)
        {
            try
            {
                var submitResult = _customerFormService.GetCustomerContractInfo(contractId, dealerName);
                return Ok(submitResult);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}