using AutoMapper;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain.Repositories;
using DealnetPortal.Utilities.Logging;

namespace DealnetPortal.Api.Integration.Services
{
    public class RateCardsService : IRateCardsService
    {
        private readonly IRateCardsRepository _rateCardsRepository;
        private readonly IContractRepository _contractRepository;
        private readonly ILoggingService _loggingService;

        public RateCardsService(IRateCardsRepository rateCardsRepository, IContractRepository contractRepository, ILoggingService loggingService)
        {
            _rateCardsRepository = rateCardsRepository;
            _contractRepository = contractRepository;
            _loggingService = loggingService;
        }

        public TierDTO GetRateCardsByDealerId(string dealerId)
        {
            var tier = _rateCardsRepository.GetTierByDealerId(dealerId, null, null, null);
            return Mapper.Map<TierDTO>(tier);
        }

        public TierDTO GetRateCardsForContract(int contractId, string dealerId)
        {
            var contract = _contractRepository.GetContract(contractId, dealerId);
            int beacons = contract.PrimaryCustomer.CreditReport?.Beacon ?? 0;
            _loggingService.LogInfo($"Getting tier for dealer {dealerId}, contract {contractId} with {beacons} beacons points.");
            var tier = _rateCardsRepository.GetTierByDealerId(dealerId, beacons, contract.DateOfSubmit, contract.Details.OverrideCustomerRiskGroup);
            _loggingService.LogInfo($"Got tier for dealer {dealerId}: tier id = {tier.Id}, rate cards count = {tier.RateCards.Count}.");
            return Mapper.Map<TierDTO>(tier);
        }
    }
}
