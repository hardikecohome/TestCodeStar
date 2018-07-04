using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;

namespace DealnetPortal.DataAccess.Repositories
{
    public class ApplicationRepository : BaseRepository, IApplicationRepository
    {
        public ApplicationRepository(IDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }

        public Application GetApplication(string id)
        {
            return _dbContext.Applications.FirstOrDefault(x => x.Id == id);
        }
    }
}
