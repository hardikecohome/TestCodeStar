using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Unity.Interception.Utilities;

namespace DealnetPortal.DataAccess.Repositories
{
    public class DealerRepository : BaseRepository, IDealerRepository
    {
        public DealerRepository(IDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }

        public string GetParentDealerId(string dealerId)
        {
            return base.GetUserById(dealerId).ParentDealerId;
        }

        public string GetUserIdByName(string userName)
        {
            return _dbContext.Users.FirstOrDefault(u => u.UserName == userName)?.Id;
        }

        public string GetUserIdByOnboardingLink(string link)
        {
            return !string.IsNullOrEmpty(link) ? _dbContext.Users.FirstOrDefault(u => u.OnboardingLink == link)?.Id : null;
        }

        public IList<string> GetUserRoles(string dealerId)
        {
            var userRoles = _dbContext.Users.Find(dealerId)?.Roles.Select(r => _dbContext.Roles.Find(r.RoleId)?.Name).ToList();
            return userRoles;            
        }

        public string GetDealerNameByCustomerLinkId(int customerLinkId)
        {
            return _dbContext.Users
                .FirstOrDefault(u => u.CustomerLinkId == customerLinkId)?.UserName;
        }

        public DealerProfile GetDealerProfile(string dealerId)
        {
            return _dbContext.DealerProfiles.FirstOrDefault(x => x.DealerId == dealerId);
        }

        public DealerProfile UpdateDealerProfile(DealerProfile profile)
        {
            if (profile == null)
            {
                return null;
            }
            var dbProfile = GetDealerProfile(profile.DealerId);
            if (dbProfile == null)
            {
                _dbContext.Entry(profile).State = profile.Id == 0 ? EntityState.Added : EntityState.Modified;
                return profile;
            }
            profile.Id = dbProfile.Id;

            if (profile.Culture != null)
            {
                dbProfile.Culture = profile.Culture;
            }
            if (profile.EmailAddress != null)
            {
                dbProfile.EmailAddress = profile.EmailAddress;
            }
            if (profile.Phone != null)
            {
                dbProfile.Phone = profile.Phone;
            }
            if (profile.Address != null)
            {
                if (dbProfile.Address.City != profile.Address.City)
                {
                    dbProfile.Address.City = profile.Address.City;
                }
                if (dbProfile.Address.PostalCode != profile.Address.PostalCode)
                {
                    dbProfile.Address.PostalCode = profile.Address.PostalCode;
                }
                if (dbProfile.Address.State != profile.Address.State)
                {
                    dbProfile.Address.State = profile.Address.State;
                }
                if (dbProfile.Address.Street != profile.Address.Street)
                {
                    dbProfile.Address.Street = profile.Address.Street;
                }
                if (dbProfile.Address.Unit != profile.Address.Unit)
                {
                    dbProfile.Address.Unit = profile.Address.Unit;
                }
            }

            UpdateProfileEquipments(profile, dbProfile.Equipments?.ToList());
            UpdateProfileArears(profile, dbProfile.Areas?.ToList());
            return dbProfile;
        }

        public void UpdateDealer(ApplicationUser dealer)
        {
            _dbContext.Users.AddOrUpdate(dealer);
        }

        private void UpdateProfileEquipments(DealerProfile profile, ICollection<DealerEquipment> dbEquipments)
        {
            if (profile.Equipments == null)
            {
                dbEquipments.ForEach(e => _dbContext.DealerEquipments.Remove(e));
            }
            else
            {
                var existingEntities =
                    dbEquipments.Where(
                        a => profile.Equipments.Any(ee => ee.EquipmentId == a.EquipmentId)).ToList();
                var newEntities = new List<DealerEquipment>();

                profile.Equipments.ForEach(equipment =>
                {
                    var dbEquipment = dbEquipments.SingleOrDefault(x => x.EquipmentId == equipment.EquipmentId);
                    equipment.ProfileId = profile.Id;
                    if (dbEquipment != null)
                    {
                        equipment.Id = dbEquipment.Id;
                    }
                    else
                    {
                        newEntities.Add(equipment);
                        _dbContext.DealerEquipments.Add(equipment);
                    }
                });
                var entriesForDelete = dbEquipments.Except(existingEntities).Except(newEntities).ToList();
                entriesForDelete.ForEach(e => _dbContext.DealerEquipments.Remove(e));
            }
        }

        private void UpdateProfileArears(DealerProfile profile, ICollection<DealerArea> dbArears)
        {
            if (profile.Areas == null)
            {
                dbArears.ForEach(ar=> _dbContext.DealerArears.Remove(ar));
            }
            else
            {
                var existingEntities =
               dbArears.Where(
                   a => profile.Areas.Any(ee => ee.PostalCode == a.PostalCode)).ToList();
                var newEntities = new List<DealerArea>();

                profile.Areas.ForEach(area =>
                {
                    var dbArea = dbArears.SingleOrDefault(x => x.PostalCode == area.PostalCode);
                    area.ProfileId = profile.Id;
                    if (dbArea != null)
                    {
                        area.Id = dbArea.Id;
                    }
                    else
                    {
                        newEntities.Add(area);
                        _dbContext.DealerArears.Add(area);
                    }
                });
                var entriesForDelete = dbArears.Except(existingEntities).Except(newEntities).ToList();
                entriesForDelete.ForEach(e => _dbContext.DealerArears.Remove(e));
            }
           
        }
    } 

    
}
