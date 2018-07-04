using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Dealer;
using DealnetPortal.Domain.Repositories;

namespace DealnetPortal.DataAccess.Repositories
{
    public class LicenseDocumentRepository : BaseRepository, ILicenseDocumentRepository
    {
        public LicenseDocumentRepository(IDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }

        public IList<LicenseDocument> GetAllLicenseDocuments()
        {
            return _dbContext.LicenseDocuments.ToList();
        }
    }
}
