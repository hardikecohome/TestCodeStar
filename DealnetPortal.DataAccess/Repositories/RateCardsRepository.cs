using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Objects;
using System.Linq;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;

namespace DealnetPortal.DataAccess.Repositories
{
    public class RateCardsRepository: BaseRepository, IRateCardsRepository
    {
        public RateCardsRepository(IDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }

        public Tier GetTierByDealerId(string dealerId, int? beacons, DateTime? validDate, string OverrideCustomerRiskGroup)
        {
            var dealer = _dbContext.Users
                .Include(x => x.Tier)
                .Include(x => x.Tier.RateCards)
                .SingleOrDefault(u => u.Id == dealerId);
            var date = validDate ?? DateTime.Now;
            dealer.Tier.RateCards = dealer.Tier.RateCards.Where(x =>
            (x.ValidFrom == null && x.ValidTo == null) ||
            (x.ValidFrom <= date && x.ValidTo > date) ||
            (x.ValidFrom <= date && x.ValidTo == null) ||
            (x.ValidFrom == null && x.ValidTo > date)).ToList();
            if (string.IsNullOrEmpty(OverrideCustomerRiskGroup) && beacons.HasValue)
            {
                dealer.Tier.RateCards = dealer.Tier.RateCards.Where(x =>!x.CustomerRiskGroupId.HasValue ||
                (x.CustomerRiskGroup.BeaconScoreFrom <= beacons && beacons <= x.CustomerRiskGroup.BeaconScoreTo)).ToList();
            }
            if (!string.IsNullOrEmpty(OverrideCustomerRiskGroup))
            {
                dealer.Tier.RateCards = dealer.Tier.RateCards.Where(x => !x.CustomerRiskGroupId.HasValue ||
                (x.CustomerRiskGroup.GroupName == OverrideCustomerRiskGroup)).ToList();
            }
            return dealer.Tier;
        }

        public Tier GetTierByName(string tierName)
        {
            return _dbContext.Tiers.SingleOrDefault(t => t.Name == tierName);
        }

        public decimal? GetCreditAmount(int creditScore)
        {
            return _dbContext.CreditAmountSettings.FirstOrDefault(ca => creditScore >= ca.CreditScoreFrom && creditScore <= ca.CreditScoreTo)?.CreditAmount;
        }

        public CreditAmountSetting GetCreditAmountSetting(int creditScore)
        {
            return _dbContext.CreditAmountSettings.FirstOrDefault(ca => creditScore >= ca.CreditScoreFrom && creditScore <= ca.CreditScoreTo);
        }

        public CustomerRiskGroup GetCustomerRiskGroupByBeacon(int beaconScore)
        {
            return _dbContext.CustomerRiskGroups.FirstOrDefault(crg => beaconScore >= crg.BeaconScoreFrom && beaconScore <= crg.BeaconScoreTo);
        }

        public IList<RateReductionCard> GetRateReductionCard()
        {
            return _dbContext.RateReductionCards.OrderBy(r => r.CustomerReduction).ToList();
        }
    }
}
