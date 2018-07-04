using DealnetPortal.Utilities.Logging;
using System;
using System.Linq;
using System.Web.Http;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Domain.Repositories;

namespace DealnetPortal.Api.BackgroundScheduler
{
    public class BackgroundSchedulerService : IBackgroundSchedulerService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IMailService _mailService;
        private readonly ILoggingService _loggingService;

        public BackgroundSchedulerService()
        {
            _loggingService = (ILoggingService)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(ILoggingService));
            _contractRepository = (IContractRepository)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IContractRepository));
            _mailService = (IMailService)GlobalConfiguration.Configuration.DependencyResolver.GetService(typeof(IMailService));
        }

        public void CheckExpiredLeads(DateTime currentDateTime, int minutesPeriod)
        {
            _loggingService.LogInfo($"Checking expired leads started at {DateTime.Now}.");
            var expiredDateTime = currentDateTime.AddMinutes(-minutesPeriod);
            try
            {
                var contracts = _contractRepository.GetExpiredContracts(expiredDateTime);
                _loggingService.LogInfo($"There are {contracts.Count} expired contracts.");
                foreach (var contract in contracts)
                {
                    //_loggingService.LogInfo($"Sending infromation for contract id = {contract.Id}...");
                    try
                    {
                        _mailService.SendNotifyMailNoDealerAcceptedLead12H(contract);
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogInfo($"Sending contract id = {contract.Id} trow exeption with message: {ex.Message}");
                    }
                    //_loggingService.LogInfo($"Infromation for contract id = {contract.Id} has sent.");
                }
                _loggingService.LogInfo($"Checking expired leads finished at {DateTime.Now}.");
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error in CheckExpiredLeads", ex);
            }
            _loggingService.LogInfo($"Checking expired leads finished at {DateTime.Now}.");
        }
    }
}
