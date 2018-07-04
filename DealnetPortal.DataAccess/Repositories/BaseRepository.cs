using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Domain;

namespace DealnetPortal.DataAccess.Repositories
{
    public class BaseRepository
    {
        protected readonly ApplicationDbContext _dbContext;

        public BaseRepository(IDatabaseFactory databaseFactory)
        {
            _dbContext = databaseFactory.Get();
        }

        protected ApplicationUser GetUserById(string userId)
        {
            return _dbContext.Users.FirstOrDefault(u => u.Id == userId);
        }
    }
}
