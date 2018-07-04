using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml.Linq;
using AutoMapper;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Utilities.Logging;
using Microsoft.AspNet.Identity.Owin;
using System.Web.Hosting;
using DealnetPortal.Api.Integration.Interfaces;

namespace DealnetPortal.Api.Controllers
{
    [RoutePrefix("api/Storage")]
    public class StorageController : BaseApiController
    {        
        private readonly IDocumentService _documentService;

        public StorageController(ILoggingService loggingService,IDocumentService documentService) : base(loggingService)
        {            
            _documentService = documentService;
        }

        //[AllowAnonymous]
        //[Route("UploadAgreementTemplate")]
        //[HttpPost]
        //public async Task<IHttpActionResult> PostUploadAgreementTemplate(AgreementTemplateDTO newAgreementTemplate)
        //{
        //    try
        //    {
        //        var result = await Task.Run(() =>
        //        {
        //            var newAgreement = Mapper.Map<AgreementTemplate>(newAgreementTemplate);

        //            if (string.IsNullOrEmpty(newAgreement.DealerId) &&
        //                !string.IsNullOrEmpty(newAgreementTemplate.DealerName))
        //            {
        //                var userManager = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
        //                var user = userManager?.FindByNameAsync(newAgreementTemplate.DealerName).GetAwaiter().GetResult();
        //                if (user != null)
        //                {
        //                    newAgreement.DealerId = user.Id;
        //                }
        //            }

        //            var addedAgreement = _fileRepository.AddOrUpdateAgreementTemplate(newAgreement);
        //            _unitOfWork.Save();
        //            return addedAgreement;
        //        });
        //        var resDto = Mapper.Map<AgreementTemplateDTO>(result);                
        //        return Ok(resDto);
        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}


        [AllowAnonymous]
        [Route("NotifySignatureStatus")]
        [HttpPost]
        public async Task<IHttpActionResult> PostNotifySignatureStatus(HttpRequestMessage request)
        {
            try
            {
                var requestStr = await request.Content.ReadAsStringAsync();
                var alerts = await _documentService.ProcessSignatureEvent(requestStr);
                if (alerts?.All(a => a.Type != AlertType.Error) == true)
                {
                    return Ok();
                }
                return BadRequest(alerts?.FirstOrDefault()?.Message);

            }
            catch (Exception ex)
            {
                LoggingService.LogError("Error occurred when received request from DocuSign", ex);
                return InternalServerError();
            }            
        }         
    }
}
