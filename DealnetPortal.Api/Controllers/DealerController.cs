using System;
using System.Threading.Tasks;
using System.Web.Http;

using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Models.DealerOnboarding;
using DealnetPortal.Api.Models.Profile;
using DealnetPortal.Utilities.Logging;
using DealnetPortal.Api.Models.Notify;

namespace DealnetPortal.Api.Controllers
{
    [Authorize]
    [RoutePrefix("api/Dealer")]
    public class DealerController : BaseApiController
    {
        private IDealerService _dealerService { get; set; }

        public DealerController(ILoggingService loggingService, IDealerService dealerService)
            : base(loggingService)
        {
            _dealerService = dealerService;
        }

        [Route("GetDealerProfile")]
        [HttpGet]
        public IHttpActionResult GetDealerProfile()
        {
            try
            {            
                var dealerProfile = _dealerService.GetDealerProfile(LoggedInUser.UserId);
                return Ok(dealerProfile);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get profile for the Dealer {LoggedInUser.UserId}", ex);
                return InternalServerError(ex);
            }
        }
        [Route("UpdateDealerProfile")]
        [HttpPost]
        public IHttpActionResult UpdateDealerProfile(DealerProfileDTO dealerProfile)
        {
            try
            {
                dealerProfile.DealerId = LoggedInUser.UserId;
                var result =  _dealerService.UpdateDealerProfile(dealerProfile);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("UpdateDealerOnboardingInfo")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<IHttpActionResult> UpdateDealerOnboardingInfo(DealerInfoDTO dealerInfo)
        {
            try
            {
                var result = await _dealerService.UpdateDealerOnboardingForm(dealerInfo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }            
        }

        [Route("CheckOnboardingLink")]
        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult CheckOnboardingLink(string dealerLink)
        {
            try
            {
                var result = _dealerService.CheckOnboardingLink(dealerLink);                    
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("SubmitDealerOnboardingInfo")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<IHttpActionResult> SubmitDealerOnboardingInfo(DealerInfoDTO dealerInfo)
        {
            try
            {
                var result = await _dealerService.SubmitDealerOnboardingForm(dealerInfo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("SendDealerOnboardingDraftLink")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<IHttpActionResult> SendDealerOnboardingDraftLink(DraftLinkDTO link)
        {
            try
            {
                var result = await _dealerService.SendDealerOnboardingDraftLink(link);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("AddDocumentToDealerOnboarding")]
        [HttpPut]
        [AllowAnonymous]
        public async Task<IHttpActionResult> AddDocumentToDealerOnboarding(RequiredDocumentDTO document)
        {
            try
            {
                var result = await _dealerService.AddDocumentToOnboardingForm(document);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("DeleteDocumentFromOnboardingForm")]
        [HttpPut]
        [AllowAnonymous]
        public IHttpActionResult DeleteDocumentFromOnboardingForm(RequiredDocumentDTO document)
        {
            try
            {
                var result = _dealerService.DeleteDocumentFromOnboardingForm(document);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("GetDealerOnboardingInfo")]
        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult GetDealerOnboardingInfo(int dealerInfoId)
        {
            try
            {
                var dealerForm = _dealerService.GetDealerOnboardingForm(dealerInfoId);
                return Ok(dealerForm);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get dealer onboarding form with Id {dealerInfoId}", ex);
                return InternalServerError(ex);
            }
        }
        [Route("GetDealerParent")]
        [HttpGet]
        public IHttpActionResult GetDealerParent()
        {
            try
            {
                var dealerParentName = _dealerService.GetDealerParentName(LoggedInUser.UserId);
                return Ok(dealerParentName);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get parent for the Dealer {LoggedInUser.UserId}", ex);
                return InternalServerError(ex);
            }
        }

        [Route("GetDealerOnboardingInfo")]
        [HttpGet]
        [AllowAnonymous]
        public IHttpActionResult GetDealerOnboardingInfo(string accessKey)
        {
            try
            {
                var dealerForm = _dealerService.GetDealerOnboardingForm(accessKey);
                return Ok(dealerForm);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to get dealer onboarding form with access key {accessKey}", ex);
                return InternalServerError(ex);
            }
	}
        [Route("DealerSupportRequestEmail")]
        [HttpPost]
        public IHttpActionResult DealerSupportRequestEmail(SupportRequestDTO dealerSupportRequest)
        {
            try
            {
                //dealerProfile.DealerId = LoggedInUser.UserId;
                var result = _dealerService.DealerSupportRequestEmail(dealerSupportRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
