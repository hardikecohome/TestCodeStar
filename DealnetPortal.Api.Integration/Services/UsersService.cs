using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Aspire.Integration.Storage;
using DealnetPortal.DataAccess;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities.Configuration;
using DealnetPortal.Utilities.Logging;
using DocuSign.eSign.Model;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Unity.Interception.Utilities;

namespace DealnetPortal.Api.Integration.Services
{
    public class UsersService : IUsersService
    {
        private readonly IAspireStorageReader _aspireStorageReader;
        private readonly ILoggingService _loggingService;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IRateCardsRepository _rateCardsRepository;
        private readonly IDealerRepository _dealerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppConfiguration _сonfiguration;

        public UsersService(IAspireStorageReader aspireStorageReader, ILoggingService loggingService, IRateCardsRepository rateCardsRepository,
            ISettingsRepository settingsRepository, IDealerRepository dealerRepository, IUnitOfWork unitOfWork, IAppConfiguration appConfiguration)
        {
            _aspireStorageReader = aspireStorageReader;
            _loggingService = loggingService;
            _settingsRepository = settingsRepository;
            _rateCardsRepository = rateCardsRepository;
            _dealerRepository = dealerRepository;
            _unitOfWork = unitOfWork;
            _сonfiguration = appConfiguration;
        }        

        public IList<Claim> GetUserClaims(ApplicationUser user)
        {
            var settings = _settingsRepository.GetUserSettings(user.Id);

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));

            var aspireUserInfo = AutoMapper.Mapper.Map<DealerDTO>(_aspireStorageReader.GetDealerRoleInfo(user.UserName));
            var mbConfigRoles = _сonfiguration.GetSetting(WebConfigKeys.MB_ROLE_CONFIG_KEY).Split(',').Select(s => s.Trim()).ToArray();
            if (aspireUserInfo != null)
            {
                var dealerProvinceCode = aspireUserInfo.Locations?.FirstOrDefault(x => x.AddressType == AddressType.MainAddress)?.State?.ToProvinceCode();
                claims.Add(new Claim(ClaimNames.QuebecDealer, (dealerProvinceCode != null && dealerProvinceCode == "QC").ToString()));
                claims.Add(new Claim(ClaimNames.ClarityDealer, (!string.IsNullOrEmpty(aspireUserInfo.Ratecard) && aspireUserInfo.Ratecard == _сonfiguration.GetSetting(WebConfigKeys.CLARITY_TIER_NAME)).ToString()));
                claims.Add(new Claim(ClaimNames.MortgageBroker, (aspireUserInfo.Role != null && mbConfigRoles.Contains(aspireUserInfo.Role)).ToString()));
                claims.Add(new Claim(ClaimNames.LeaseTier, user.LeaseTier));
                claims.Add(new Claim(ClaimNames.IsEmcoDealer, (!string.IsNullOrEmpty(user.LeaseTier) && user.LeaseTier.ToLower() == _сonfiguration.GetSetting(WebConfigKeys.EMCO_LEASE_TIER_NAME).ToString().ToLower()).ToString()));
            }
            if (!string.IsNullOrEmpty(user.DealerType))
            {
                claims.Add(new Claim(ClaimNames.AgreementType, user.DealerType));
            }

            if (settings?.SettingValues != null)
            {
                if (settings.SettingValues.Any())
                {
                    claims.Add(new Claim(ClaimNames.ShowAbout, false.ToString()));
                    claims.Add(new Claim(ClaimNames.HasSkin, true.ToString()));
                }
                else
                {
                    claims.Add(new Claim(ClaimNames.ShowAbout, true.ToString()));
                }
            }
            else
            {
                claims.Add(new Claim(ClaimNames.ShowAbout, true.ToString()));
                claims.Add(new Claim(ClaimNames.HasSkin, false.ToString()));
            }

            return claims;
        }

        public async Task<IList<Alert>> SyncAspireUser(ApplicationUser user, UserManager<ApplicationUser> userManager = null)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException(nameof(userManager));
            }

            var alerts = new List<Alert>();

            //get user info from aspire DB
            DealerDTO aspireDealerInfo = null;
            try
            {
                aspireDealerInfo =
                    AutoMapper.Mapper.Map<DealerDTO>(_aspireStorageReader.GetDealerRoleInfo(user.UserName));

                if (aspireDealerInfo != null)
                {
                    var parentAlerts = await UpdateUserParent(user.Id, aspireDealerInfo, userManager);
                    if (parentAlerts.Any())
                    {
                        alerts.AddRange(parentAlerts);
                    }
                    var rolesAlerts = await UpdateUserRoles(user.Id, aspireDealerInfo, userManager);
                    if (rolesAlerts.Any())
                    {
                        alerts.AddRange(rolesAlerts);
                    }
                    if (user.Tier?.Name != aspireDealerInfo.Ratecard ||
                        user.LeaseTier != aspireDealerInfo.LeaseRatecard ||
                        user.DealerType != aspireDealerInfo.ProductType)
                    {
                        var tierAlerts = await UpdateUserTier(user.Id, aspireDealerInfo, userManager);
                        if (tierAlerts.Any())
                        {
                            alerts.AddRange(tierAlerts);
                        }
                    }
                    var profileAlerts = await UpdateDealerProfile(user.Id, aspireDealerInfo);
                    if (profileAlerts.Any())
                    {
                        alerts.AddRange(profileAlerts);
                    }                    
                }                
            }
            catch (Exception ex)
            {
                aspireDealerInfo = null;
                var errorMsg = $"Cannot connect to aspire database for get [{user.UserName}] info";
                _loggingService?.LogWarning(errorMsg);
                alerts.Add(new Alert()
                {
                    Code = ErrorCodes.AspireDatabaseConnectionFailed,
                    Header = errorMsg,
                    Type = AlertType.Warning,
                    Message = ex.ToString()
                });
            }

            return alerts;
        }

        public string GetUserPassword(string userId)
        {
            string aspirePassword = null;
            using (var secContext = new SecureDbContext())
            {
                try
                {                
                    var user = secContext.Users.Find(userId);
                    aspirePassword = user?.Secure_AspirePassword;
                }
                catch (Exception e)
                {
                    _loggingService?.LogError($"Cannot recieve password for user {userId}", e);
                }
            }
            return aspirePassword;
        }

        public void UpdateUserPassword(string userId, string newPassword)
        {
            using (var secContext = new SecureDbContext())
            {
                try
                {
                    var user = secContext.Users.Find(userId);
                    if (user.Secure_AspirePassword != newPassword)
                    {
                        user.Secure_AspirePassword = newPassword;
                        secContext.SaveChanges();
                        _loggingService?.LogInfo(
                            $"Password for Aspire user [{userId}] was updated successefully");
                    }
                }
                catch (Exception e)
                {
                    _loggingService?.LogError($"Cannot update password for user {userId}", e);
                }
            }
        }

        #region private
        private async Task<IList<Alert>> UpdateUserParent(string userId, DealerDTO aspireUser, UserManager<ApplicationUser> userManager)
        {
            var alerts = new List<Alert>();
            var parentUser = !string.IsNullOrEmpty(aspireUser?.ParentDealerUserName)
                    ? await userManager.FindByNameAsync(aspireUser.ParentDealerUserName)
                    : null;

            if (parentUser != null)
            {
                var updateUser = await userManager.FindByIdAsync(userId);
                if (updateUser.ParentDealerId != parentUser.Id)
                {
                    updateUser.ParentDealer = parentUser;
                    updateUser.ParentDealerId = parentUser.Id;
                    var updateRes = await userManager.UpdateAsync(updateUser);
                    if (updateRes.Succeeded)
                    {
                        _loggingService?.LogInfo(
                            $"Parent dealer for Aspire user [{userId}] was updated successefully");
                    }
                    else
                    {
                        updateRes.Errors?.ForEach(e =>
                        {
                            alerts.Add(new Alert()
                            {
                                Type = AlertType.Error,
                                Header = "Error during update Aspire user",
                                Message = e
                            });
                            _loggingService.LogError($"Error during update Aspire user: {e}");
                        });
                    }
                }
            }
            return alerts;
        }

        private async Task<IList<Alert>> UpdateUserRoles(string userId, DealerDTO aspireUser, UserManager<ApplicationUser> userManager)
        {
            var alerts = new List<Alert>();
            if (!string.IsNullOrEmpty(aspireUser.Role))
            {
                var dbRoles = await userManager.GetRolesAsync(userId);
                var mbConfigRoles = _сonfiguration.GetSetting(WebConfigKeys.MB_ROLE_CONFIG_KEY).Split(',').Select(s => s.Trim()).ToArray();

                if (!dbRoles.Contains(aspireUser.Role) && !(mbConfigRoles.Contains(aspireUser.Role) && dbRoles.Contains(UserRole.MortgageBroker.ToString())))
                {                    
                    var user = await userManager.FindByIdAsync(userId);
                    var removeRes = await userManager.RemoveFromRolesAsync(userId, dbRoles.ToArray());
                    IdentityResult addRes;
                    if (mbConfigRoles.Contains(aspireUser.Role))
                    {
                        addRes = await userManager.AddToRolesAsync(userId, new[] {UserRole.MortgageBroker.ToString()});
                    }
                    else
                    {
                        addRes = await userManager.AddToRolesAsync(userId, new[] {UserRole.Dealer.ToString()});
                    }
                    
                    var updateRes = await userManager.UpdateAsync(user);
                    if (addRes.Succeeded && removeRes.Succeeded && updateRes.Succeeded)
                    {
                        _loggingService?.LogInfo(
                            $"Roles for Aspire user [{userId}] was updated successefully");
                    }
                    else
                    {
                        removeRes.Errors?.ForEach(e =>
                        {
                            alerts.Add(new Alert()
                            {
                                Type = AlertType.Error,
                                Header = "Error during remove role",
                                Message = e
                            });
                            _loggingService.LogError($"Error during remove role for an user {userId}: {e}");
                        });
                        addRes.Errors?.ForEach(e =>
                        {
                            alerts.Add(new Alert()
                            {
                                Type = AlertType.Error,
                                Header = "Error during add role",
                                Message = e
                            });
                            _loggingService.LogError($"Error during add role for an user {userId}: {e}");
                        });
                    }

                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Error during update role",
                    Message = $"Error during getting user role from Aspire, for an user {userId}"
                });
                _loggingService.LogError($"Error during getting user role from Aspire, for an user {userId}");
            }
            return alerts;
        }

        private async Task<IList<Alert>> UpdateUserTier(string userId, DealerDTO aspireUser, UserManager<ApplicationUser> userManager)
        {
            var alerts = new List<Alert>();
            try
            {
                var tier = _rateCardsRepository.GetTierByName(aspireUser.Ratecard);               

                var updateUser = await userManager.FindByIdAsync(userId);
                if (updateUser != null)
                {
                    updateUser.LeaseTier = aspireUser.LeaseRatecard;
                    if (tier != null)
                    {
                        updateUser.TierId = tier.Id;
                    }
                    if (!string.IsNullOrEmpty(aspireUser.ProductType))
                    {                        
                        if (updateUser.DealerType != aspireUser.ProductType)
                        {
                            updateUser.DealerType = aspireUser.ProductType;
                        }
                    }
                    var updateRes = await userManager.UpdateAsync(updateUser);
                    if (updateRes.Succeeded)
                    {
                        {
                            _loggingService.LogInfo($"Tier [{aspireUser.Ratecard}] was set to an user [{updateUser.Id}]");
                            _loggingService.LogInfo($"Lease Tier [{aspireUser.LeaseRatecard}] was set to an user [{updateUser.Id}]");
                        }
                    }
                }
                else
                {
                    _loggingService.LogInfo($"Tier [{aspireUser.Ratecard}] tier is not available. Rate card is not configured for user  [{updateUser?.Id}]");
                }
            }
            catch(Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Error during update user tier",
                    Message = $"Error during update user tier for an user {userId}"
                });
                _loggingService.LogError($"Error during update user tier for an user {userId}", ex);
            }
            return alerts;
        }

        private async Task<IList<Alert>> UpdateDealerProfile(string userId, DealerDTO aspireUser)
        {
            var alerts = new List<Alert>();
            try
            {
                var dealerProfile = _dealerRepository.GetDealerProfile(userId) ?? new DealerProfile { DealerId = userId };
                dealerProfile.EmailAddress = aspireUser.Emails?.FirstOrDefault(e => !string.IsNullOrEmpty(e.EmailAddress))?.EmailAddress.Split(';').FirstOrDefault();// Hot fix, should be removed later
                dealerProfile.Phone = aspireUser.Phones?.FirstOrDefault(p => !string.IsNullOrEmpty(p.PhoneNum))?.PhoneNum;
                if (aspireUser.Locations?.Any() == true)
                {
                    var address = aspireUser.Locations.FirstOrDefault();
                    dealerProfile.Address = new Address()
                    {
                        City = address.City,
                        State = address.State,
                        PostalCode = address.PostalCode,
                        Street = address.Street,
                        Unit = address.Unit
                    };
                }
                _dealerRepository.UpdateDealerProfile(dealerProfile);
                _unitOfWork.Save();
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Error during update dealer profile",
                    Message = $"Error during update dealer profile for an user {userId}"
                });
                _loggingService.LogError($"Error during update dealer profile for an user {userId}", ex);
            }
            return await Task.FromResult(alerts);
        }

        #endregion
    }
}
