using System;
using System.Collections.Generic;

namespace DealnetPortal.Domain.Repositories
{
    public interface IRateCardsRepository
    {
        Tier GetTierByDealerId(string dealerId, int? beacons, DateTime? validDate, string OverrideCustomerRiskGroup);
        Tier GetTierByName(string tierName);

        /// <summary>
        /// Get credit amount value for creditScore (beacon)
        /// </summary>
        /// <param name="creditScore">Beacon</param>
        /// <returns>Credit amount value</returns>
        decimal? GetCreditAmount(int creditScore);
        CreditAmountSetting GetCreditAmountSetting(int creditScore);
        CustomerRiskGroup GetCustomerRiskGroupByBeacon(int beaconScore);
        IList<RateReductionCard> GetRateReductionCard();
    }
}