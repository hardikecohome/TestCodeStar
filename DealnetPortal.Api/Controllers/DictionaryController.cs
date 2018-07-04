using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using AutoMapper;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Api.Models;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.DealerOnboarding;
using DealnetPortal.Api.Models.UserSettings;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities;
using DealnetPortal.Utilities.Logging;

namespace DealnetPortal.Api.Controllers
{
    [RoutePrefix("api/dict")]
    public class DictionaryController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        private IContractRepository _contractRepository { get; set; }
        private IRateCardsRepository _rateCardsRepository { get; set; }
        private ISettingsRepository SettingsRepository { get; set; }
        private IAspireStorageReader AspireStorageReader { get; set; }
        private ICustomerFormService CustomerFormService { get; set; }
        private IContractService _contractService { get; set; }

        private readonly IDealerRepository _dealerRepository;
        private readonly ILicenseDocumentRepository _licenseDocumentRepository;

        public DictionaryController(IUnitOfWork unitOfWork, IContractRepository contractRepository, ISettingsRepository settingsRepository, ILoggingService loggingService, 
            IAspireStorageReader aspireStorageReader, ICustomerFormService customerFormService, IContractService contractService, IDealerRepository dealerRepository, 
            ILicenseDocumentRepository licenseDocumentRepository, IRateCardsRepository rateCardsRepository)
            : base(loggingService)
        {
            _unitOfWork = unitOfWork;
            _contractRepository = contractRepository;
            SettingsRepository = settingsRepository;
            AspireStorageReader = aspireStorageReader;
            CustomerFormService = customerFormService;
            _contractService = contractService;
            _dealerRepository = dealerRepository;
            _licenseDocumentRepository = licenseDocumentRepository;            
            _rateCardsRepository = rateCardsRepository;
        }             

        [Route("DocumentTypes")]
        [HttpGet]
        public IHttpActionResult GetAllDocumentTypes()
        {
            var alerts = new List<Alert>();
            try
            {
                var docTypes = Mapper.Map<IList<DocumentTypeDTO>>(_contractRepository.GetAllDocumentTypes());
                if (docTypes == null)
                {
                    var errorMsg = "Cannot retrieve Document Types";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.EquipmentTypesRetrievalFailed,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var result = new Tuple<IList<DocumentTypeDTO>, IList<Alert>>(docTypes, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve Document Types", ex);
                return InternalServerError(ex);
            }
        }

        [Route("DocumentTypes/{state}")]
        [HttpGet]
        public IHttpActionResult GetStateDocumentTypes(string state)
        {
            var alerts = new List<Alert>();
            try
            {
                var docTypes = Mapper.Map<IList<DocumentTypeDTO>>(_contractRepository.GetStateDocumentTypes(state));
                if (docTypes == null)
                {
                    var errorMsg = "Cannot retrieve Document Types";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.EquipmentTypesRetrievalFailed,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var result = new Tuple<IList<DocumentTypeDTO>, IList<Alert>>(docTypes, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve Document Types", ex);
                return InternalServerError(ex);
            }
        }

        [Route("DealerDocumentTypes/{state}")]
        [Authorize]
        [HttpGet]
        public IHttpActionResult GetDealerDocumentTypes(string state)
        {
            var alerts = new List<Alert>();
            try
            {
                var docTypes = Mapper.Map<IList<DocumentTypeDTO>>(_contractRepository.GetDealerDocumentTypes(state, LoggedInUser?.UserId));
                if (docTypes == null)
                {
                    var errorMsg = "Cannot retrieve Document Types";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.EquipmentTypesRetrievalFailed,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var result = new Tuple<IList<DocumentTypeDTO>, IList<Alert>>(docTypes, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve Document Types", ex);
                return InternalServerError(ex);
            }
        }

        [Route("DealerDocumentTypes")]
        [Authorize]
        [HttpGet]
        public IHttpActionResult GetDealerDocumentTypes()
        {
            var alerts = new List<Alert>();
            try
            {
                var provinceDocTypes = Mapper.Map<IDictionary<string, IList<DocumentTypeDTO>>>(_contractRepository.GetDealerDocumentTypes(LoggedInUser?.UserId));
                if (provinceDocTypes == null)
                {
                    var errorMsg = "Cannot retrieve Document Types";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.EquipmentTypesRetrievalFailed,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var result = new Tuple<IDictionary<string, IList<DocumentTypeDTO>>, IList<Alert>>(provinceDocTypes, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve Document Types", ex);
                return InternalServerError(ex);
            }
        }

        [Authorize]
        [Route("DealerEquipmentTypes")]
        [HttpGet]
        public IHttpActionResult GetDealerEquipmentTypes()
        {
            try
            {
                var result = _contractService.GetDealerEquipmentTypes(LoggedInUser?.UserId);
                if (result == null)
                {
                    var errorMsg = "Cannot retrieve Equipment Types";
                    
                    LoggingService.LogError(errorMsg);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve Equipment Types", ex);
                return InternalServerError(ex);
            }
        }

        [Route("AllEquipmentTypes")]
        [HttpGet]
        public IHttpActionResult GetAllEquipmentTypes()
        {
            var alerts = new List<Alert>();
            try
            {
                var equipmentTypes = _contractRepository.GetEquipmentTypes();
                var equipmentTypeDtos = Mapper.Map<IList<EquipmentTypeDTO>>(equipmentTypes);
                if (equipmentTypes == null)
                {
                    var errorMsg = "Cannot retrieve Equipment Types";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.EquipmentTypesRetrievalFailed,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var result = new Tuple<IList<EquipmentTypeDTO>, IList<Alert>>(equipmentTypeDtos, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve Equipment Types", ex);
                return InternalServerError(ex);
            }
        }

        [Route("AllLicenseDocuments")]
        [HttpGet]
        public IHttpActionResult GetAllLicenseDocuments()
        {
            var alerts = new List<Alert>();
            try
            {
                var licenseDocuments = _licenseDocumentRepository.GetAllLicenseDocuments();
                var licenseDocumentDtos = Mapper.Map<IList<LicenseDocumentDTO>>(licenseDocuments);
                if (licenseDocuments == null)
                {
                    var errorMsg = "Cannot retrieve License documents";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.LicenseDocumentsRetrievalFailed,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var result = new Tuple<IList<LicenseDocumentDTO>, IList<Alert>>(licenseDocumentDtos, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve License documents", ex);
                return InternalServerError(ex);
            }
        }

        [Route("{province}/ProvinceTaxRate")]
        [HttpGet]
        public IHttpActionResult GetProvinceTaxRate(string province)
        {
            var alerts = new List<Alert>();
            try
            {
                var provinceTaxRate = _contractRepository.GetProvinceTaxRate(province);
                var provinceTaxRateDto = Mapper.Map<ProvinceTaxRateDTO>(provinceTaxRate);
                if (provinceTaxRate == null)
                {
                    var errorMsg = "Cannot retrieve Province Tax Rate";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.ProvinceTaxRateRetrievalFailed,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var result = new Tuple<ProvinceTaxRateDTO, IList<Alert>>(provinceTaxRateDto, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve Province Tax Rate", ex);
                return InternalServerError(ex);
            }
        }

        [Route("AllProvinceTaxRates")]
        [HttpGet]
        public IHttpActionResult GetAllProvinceTaxRates()
        {
            var alerts = new List<Alert>();
            try
            {
                var provinceTaxRates = _contractRepository.GetAllProvinceTaxRates();
                var provinceTaxRateDtos = Mapper.Map<IList<ProvinceTaxRateDTO>>(provinceTaxRates);
                if (provinceTaxRates == null)
                {
                    var errorMsg = "Cannot retrieve all Province Tax Rates";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.ProvinceTaxRateRetrievalFailed,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var result = new Tuple<IList<ProvinceTaxRateDTO>, IList<Alert>>(provinceTaxRateDtos, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve all Province Tax Rates", ex);
                return InternalServerError(ex);
            }
        }

        [Route("{id}/VerificationId")]
        [HttpGet]
        public IHttpActionResult GetVerificationId(int id)
        {
            var alerts = new List<Alert>();
            try
            {
                var verificationId = _contractRepository.GetVerficationId(id);
                var verificationIdsDto = Mapper.Map<VarificationIdsDTO>(verificationId);
                if (verificationId == null)
                {
                    var errorMsg = "Cannot retrieve Province Tax Rate";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.ProvinceTaxRateRetrievalFailed,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var result = new Tuple<VarificationIdsDTO, IList<Alert>>(verificationIdsDto, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve Verification Id", ex);
                return InternalServerError(ex);
            }
        }

        [Route("AllVerificationIds")]
        [HttpGet]
        public IHttpActionResult GetAllVerificationIds()
        {
            var alerts = new List<Alert>();
            try
            {
                var verificationIds = _contractRepository.GetAllVerificationIds();
                var verificationIdsDtos = Mapper.Map<IList<VarificationIdsDTO>>(verificationIds);
                if (verificationIds == null)
                {
                    var errorMsg = "Cannot retrieve all Verification Ids";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = ErrorConstants.ProvinceTaxRateRetrievalFailed,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var result = new Tuple<IList<VarificationIdsDTO>, IList<Alert>>(verificationIdsDtos, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve all Province Tax Rates", ex);
                return InternalServerError(ex);
            }
        }

        [Route("CreditAmount")]
        [HttpGet]
        // GET api/dict/CreditAmount?creditScore={creditScore}
        public IHttpActionResult GetCreditAmount(int creditScore)
        {
            try
            {
                var creditAmount =  _rateCardsRepository.GetCreditAmount(creditScore);
                return Ok(creditAmount);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to retrieve credit amount settings for creditScore = {creditScore}", ex);
                return InternalServerError(ex);
            }            
        }


        [Authorize]
        [Route("GetDealerInfo")]
        [HttpGet]
        public IHttpActionResult GetDealerInfo()
        {
            try
            {
                var dealer = _contractRepository.GetDealer(LoggedInUser?.UserId);
                var dealerDto = Mapper.Map<ApplicationUserDTO>(dealer);

                try
                {                
                    dealerDto.UdfSubDealers = Mapper.Map<IList<SubDealerDTO>>(AspireStorageReader.GetSubDealersList(dealer.AspireLogin ?? dealer.UserName));
                }
                catch (Exception ex)
                {
                    //it's a not critical error on this step and we continue flow
                    LoggingService.LogError("Failed to get subdealers from Aspire", ex);
                }

                return Ok(dealerDto);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Authorize]
        [HttpGet]
        // GET api/dict/GetDealerCulture
        [Route("GetDealerCulture")]
        public string GetDealerCulture()
        {
            return _contractRepository.GetDealer(LoggedInUser?.UserId).Culture ?? _dealerRepository.GetDealerProfile(LoggedInUser?.UserId)?.Culture;
        }

        [HttpGet]
        // GET api/dict/GetDealerCulture?dealer=dealer
        [Route("GetDealerCulture")]
        public string GetDealerCulture(string dealer)
        {
            var dealerId = _dealerRepository.GetUserIdByName(dealer);
            var culture = _contractRepository.GetDealer(dealerId).Culture ?? _dealerRepository.GetDealerProfile(dealerId)?.Culture;
            return culture;
        }

        [Authorize]
        [HttpPut]
        // GET api/dict/PutDealerCulture
        [Route("PutDealerCulture")]
        public IHttpActionResult PutDealerCulture(string culture)
        {
            try
            {
                _contractRepository.GetDealer(LoggedInUser?.UserId).Culture = culture;
                var profile = _dealerRepository.GetDealerProfile(LoggedInUser?.UserId);
                if (profile != null)
                {
                    profile.Culture = culture;
                }                    
                _unitOfWork.Save();
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Authorize]
        [HttpGet]
        // GET api/dict/GetDealerSettings
        [Route("GetDealerSettings")]
        public IHttpActionResult GetDealerSettings()
        {
            IList<StringSettingDTO> list = null;
	        LoggingService.LogInfo($"Get dealer skins settings for dealer: {LoggedInUser.UserName}");
            var settings = SettingsRepository.GetUserStringSettings(LoggedInUser?.UserId);
            if (settings?.Any() ?? false)
            {
	            LoggingService.LogInfo($"There are {settings.Count} variables for dealer: {LoggedInUser.UserName}");
                list = Mapper.Map<IList<StringSettingDTO>>(settings);
            }
            return Ok(list);
        }

        [HttpGet]
        // GET api/dict/GetDealerSettings?dealer={dealer}
        [Route("GetDealerSettings")]
        public IHttpActionResult GetDealerSettings(string hashDealerName)
        {
            IList<StringSettingDTO> list = null;
	        LoggingService.LogInfo($"Get dealer skins settings for dealer: {hashDealerName}");
            var settings = SettingsRepository.GetUserStringSettingsByHashDealerName(hashDealerName);
            if (settings?.Any() ?? false)
            {
	            LoggingService.LogInfo($"There are {settings.Count} variables for dealer: {hashDealerName}");
                list = Mapper.Map<IList<StringSettingDTO>>(settings);
            }
            return Ok(list);
        }

        [Authorize]
        [HttpGet]
        // GET api/dict/GetDealerBinSetting?settingType={settingType}
        [Route("GetDealerBinSetting")]
        public IHttpActionResult GetDealerBinSetting(int settingType)
        {
            SettingType sType = (SettingType) settingType;
            var binSetting = SettingsRepository.GetUserBinarySetting(sType, LoggedInUser?.UserId);
            if (binSetting != null)
            {
                var bin = new BinarySettingDTO()
                {
                    Name = binSetting.Item?.Name,
                    ValueBytes = binSetting.BinaryValue
                };
                return Ok(bin);
            }
            return Ok();
        }

        [HttpGet]
        // GET api/dict/GetDealerBinSetting?settingType={settingType}&dealer={dealer}
        [Route("GetDealerBinSetting")]
        public IHttpActionResult GetDealerBinSetting(int settingType, string hashDealerName)
        {
            SettingType sType = (SettingType)settingType;
            var binSetting = SettingsRepository.GetUserBinarySettingByHashDealerName(sType, hashDealerName);
            if (binSetting != null)
            {
                var bin = new BinarySettingDTO()
                {
                    Name = binSetting.Item?.Name,
                    ValueBytes = binSetting.BinaryValue
                };
                return Ok(bin);
            }
            return Ok();
        }

        [Authorize]
        [HttpGet]
        // GET api/Account/CheckDealerSkinExist
        [Route("CheckDealerSkinExist")]
        public IHttpActionResult CheckDealerSkinExist()
        {
            try
            {
                return Ok(SettingsRepository.CheckUserSkinExist(LoggedInUser?.UserId));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }            
        }

        [HttpGet]
        // GET api/Account/CheckDealerSkinExist?dealer={dealer}
        [Route("CheckDealerSkinExist")]
        public IHttpActionResult CheckDealerSkinExist(string dealer)
        {
            try
            {
                return Ok(SettingsRepository.CheckUserSkinExist(dealer));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Authorize]
        [HttpGet]
        // GET api/dict/GetCustomerLinkSettings
        [Route("GetCustomerLinkSettings")]
        public IHttpActionResult GetCustomerLinkSettings()
        {
            var linkSettings = CustomerFormService.GetCustomerLinkSettings(LoggedInUser?.UserId);
            if (linkSettings != null)
            {
                return Ok(linkSettings);
            }
            return NotFound();
        }

        [HttpGet]
        // GET api/dict/GetCustomerLinkSettings?dealer={dealer}
        [Route("GetCustomerLinkSettings")]
        public IHttpActionResult GetCustomerLinkSettings(string dealer)
        {
            var linkSettings = CustomerFormService.GetCustomerLinkSettingsByDealerName(dealer);
            if (linkSettings != null)
            {
                return Ok(linkSettings);
            }
            return NotFound();
        }

        [Authorize]
        [HttpPut]
        // GET api/dict/UpdateCustomerLinkSettings
        [Route("UpdateCustomerLinkSettings")]
        public IHttpActionResult UpdateCustomerLinkSettings(CustomerLinkDTO customerLinkSettings)
        {
            try
            {
                var alerts = CustomerFormService.UpdateCustomerLinkSettings(customerLinkSettings, LoggedInUser?.UserId);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }        

        [HttpGet]
        // GET api/dict/GetCustomerLinkLanguageOptions?dealer={dealer}&lang={lang}
        [Route("GetCustomerLinkLanguageOptions")]
        public IHttpActionResult GetCustomerLinkLanguageOptions(string hashDealerName, string lang)
        {
            var linkSettings = CustomerFormService.GetCustomerLinkLanguageOptions(hashDealerName, lang);
            if (linkSettings != null)
            {
                return Ok(linkSettings);
            }
            return NotFound();
        }

        [Authorize]
        [Route("AllRateReductionCards")]
        [HttpGet]
        public IHttpActionResult GetAllRateReductionCards()
        {
            var alerts = new List<Alert>();
            try
            {
                var reductionCards = _rateCardsRepository.GetRateReductionCard();
                if (reductionCards == null)
                {
                    var errorMsg = "Cannot retrieve Rate Reduction Cards";
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Message = errorMsg
                    });
                    LoggingService.LogError(errorMsg);
                }
                var reductionCardsDtos = Mapper.Map<IList<RateReductionCardDTO>>(reductionCards);
                
                var result = new Tuple<IList<RateReductionCardDTO>, IList<Alert>>(reductionCardsDtos, alerts);
                return Ok(result);
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to retrieve Equipment Types", ex);
                return InternalServerError(ex);
            }
        }
    }
}
