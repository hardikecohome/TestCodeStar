using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;

namespace DealnetPortal.DataAccess.Repositories
{
    public class SettingsRepository : BaseRepository, ISettingsRepository
    {
        public SettingsRepository(IDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }

        public IList<SettingValue> GetUserStringSettings(string dealerId)
        {
            var settings = GetUserSettings(dealerId);
            return settings?.SettingValues?.Where(s => s.Item?.SettingType == SettingType.StringValue).ToList() ?? new List<SettingValue>();
        }

        public IList<SettingValue> GetUserBinarySettings(string dealerId)
        {
            var settings = GetUserSettings(dealerId);
            return settings.SettingValues?.Where(s => s.Item?.SettingType != SettingType.StringValue).ToList() ?? new List<SettingValue>();
        }

        public IList<SettingValue> GetUserStringSettingsByHashDealerName(string hashDealerName)
        {
            var settings = GetUserSettingsByHashDealerName(hashDealerName);
            return settings?.SettingValues?.Where(s => s.Item?.SettingType == SettingType.StringValue).ToList() ?? new List<SettingValue>();
        }

        public SettingValue GetUserBinarySettingByHashDealerName(SettingType settingType, string hashDealerName)
        {
            var settings = GetUserSettingsByHashDealerName(hashDealerName);            
            return settings?.SettingValues?.FirstOrDefault(s => s.Item?.SettingType == settingType);
        }

        public SettingValue GetUserBinarySetting(SettingType settingType, string dealerId)
        {
            var settings = GetUserSettings(dealerId);            
            return settings?.SettingValues?.FirstOrDefault(s => s.Item?.SettingType == settingType);
        }

        public UserSettings GetUserSettings(string dealerId)
        {
            var user = _dbContext.Users
                .Include(u => u.Settings)
                .FirstOrDefault(u => u.Id == dealerId || u.UserName == dealerId);
            if (user?.Settings?.SettingValues?.Any() ?? false)
            {
                return user.Settings;
            }
            var puser = user?.ParentDealer;
            if (puser != null)
            {
                if (puser.Settings != null)
                {
                    return puser.Settings;
                }
                _dbContext.Entry(puser).Reference(u => u.Settings).Load();
                return puser.Settings;
            }
            return null;                        
        }

        public UserSettings GetUserSettingsByHashDealerName(string hashDealerName)
        {
            var user = _dbContext.Users
                .Include(u => u.CustomerLink).Include(u => u.Settings)
                .FirstOrDefault(u => u.CustomerLink.HashLink == hashDealerName);
            if (user?.Settings?.SettingValues?.Any() ?? false)
            {
                return user.Settings;
            }
            var puser = user?.ParentDealer;
            if (puser != null)
            {
                if (puser.Settings != null)
                {
                    return puser.Settings;
                }
                _dbContext.Entry(puser).Reference(u => u.Settings).Load();
                return puser.Settings;
            }
            return null;                        
        }

        public bool CheckUserSkinExist(string dealerId)
        {
            return GetUserSettings(dealerId)?.SettingValues.Any() ?? false;
        }

        
    }
}
