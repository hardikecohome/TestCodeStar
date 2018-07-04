using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Integration;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Api.Models;
using DealnetPortal.Api.Models.Scanning;
using DealnetPortal.Utilities;
using DealnetPortal.Utilities.Logging;

namespace DealnetPortal.Api.Controllers
{
    [Authorize]
    [RoutePrefix("api/ScanProcessing")]
    public class ScanProcessingController : ApiController
    {
        private readonly ILoggingService _loggingService;

        public ScanProcessingController(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        [Route("PostLicenseScanProcessing")]
        [HttpPost]
        public async Task<IHttpActionResult> PostLicenseScanProcessing(ScanningRequest scanningRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await Task.Run(() =>
            {
                ImageScanService scanService = new ImageScanService();
                var res = scanService.ReadDriverLicense(scanningRequest);
                return res;
            });
           
            _loggingService.LogInfo("Recieved request for scan driver license");


            if (result?.Item2?.Any(a => a.Type == AlertType.Warning) ?? false)
            {
                var warnMsg = string.Format("Scan warnings: {0}",
                    result?.Item2.Where(a => a.Type == AlertType.Warning).Aggregate(string.Empty, (current, alert) =>
                        current + $"{alert.Header}: {alert.Message};"));
                _loggingService.LogWarning(warnMsg);
            }

            if (result != null && result.Item2.All(al => al.Type != AlertType.Error))
            {
                _loggingService.LogInfo("Driver license scanned successfully");
                //return Ok(result.Item1);
            }
            else
            {
                var errorMsg = string.Format("Failed to scan driver license: {0}",
                    result?.Item2.Where(a => a.Type == AlertType.Error).Aggregate(string.Empty, (current, alert) =>
                        current + $"{alert.Header}: {alert.Message};"));
                _loggingService.LogError(errorMsg);
                ModelState.AddModelError(ErrorConstants.ScanFailed, errorMsg);
                //return BadRequest();
            }
            return Ok(result);
        }

        [Route("PostChequeScanProcessing")]
        [HttpPost]
        public async Task<IHttpActionResult> PostChequeScanProcessing(ScanningRequest scanningRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await Task.Run(() =>
            {
                ImageScanService scanService = new ImageScanService();
                var res = scanService.ReadVoidCheque(scanningRequest);
                return res;
            });

            _loggingService.LogInfo("Recieved request for scan void cheque");

            if (result != null && result.Item2.All(al => al.Type != AlertType.Error))
            {
                _loggingService.LogInfo("Void cheque scanned successfully");
                //return Ok(result.Item1);
            }
            else
            {
                var errorMsg = string.Format("Failed to scan void cheque: {0}",
                    result?.Item2.Aggregate(string.Empty, (current, alert) =>
                        current + $"{alert.Header}: {alert.Message};"));
                _loggingService.LogError(errorMsg);
                ModelState.AddModelError(ErrorConstants.ScanFailed, errorMsg);
                //return BadRequest();
            }
            return Ok(result);
        }
    }
}