using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Domain;
using DealnetPortal.Domain.Repositories;

namespace DealnetPortal.DataAccess.Repositories
{
    public class FileRepository : BaseRepository, IFileRepository
    {
        public FileRepository(IDatabaseFactory databaseFactory) : base(databaseFactory)
        {
        }

        public AgreementTemplate AddOrUpdateAgreementTemplate(AgreementTemplate agreementTemplate)
        {
            _dbContext.Set<AgreementTemplate>().AddOrUpdate(agreementTemplate);
            return agreementTemplate;
        }

        public AgreementTemplate FindAgreementTemplate(Expression<Func<AgreementTemplate, bool>> predicate)
        {
            return _dbContext.Set<AgreementTemplate>().FirstOrDefault(predicate);
        }

        public IList<AgreementTemplate> FindAgreementTemplates(Expression<Func<AgreementTemplate, bool>> predicate)
        {
            return _dbContext.Set<AgreementTemplate>().Where(predicate).ToList();
        }

        public AgreementTemplate GetAgreementTemplate(int templateId)
        {
            return _dbContext.Set<AgreementTemplate>().FirstOrDefault(t => t.Id == templateId);
        }

        public AgreementTemplate RemoveAgreementTemplate(AgreementTemplate agreementTemplate)
        {
            return _dbContext.Set<AgreementTemplate>().Remove(agreementTemplate);
        }
    }
}
