using System.Collections.Generic;
using DealnetPortal.Api.Common.Enumeration;

namespace DealnetPortal.Domain.Repositories
{
    public interface ISettingsRepository
    {
        IList<SettingValue> GetUserStringSettings(string dealerId);
        IList<SettingValue> GetUserBinarySettings(string dealerId);
        IList<SettingValue> GetUserStringSettingsByHashDealerName(string hashDealerName);
        SettingValue GetUserBinarySettingByHashDealerName(SettingType settingType, string hashDealerName);
        SettingValue GetUserBinarySetting(SettingType settingType, string dealerId);
        /// <summary>
        /// return user settings entry of settings of a parent user (dealer)
        /// </summary>
        /// <param name="dealerId"></param>
        /// <returns></returns>
        UserSettings GetUserSettings(string dealerId);
        UserSettings GetUserSettingsByHashDealerName(string dealerId);
        bool CheckUserSkinExist(string dealerId);
    }
}
