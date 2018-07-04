using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DealnetPortal.Domain.Repositories
{
    public interface IFileRepository
    {
        AgreementTemplate AddOrUpdateAgreementTemplate(AgreementTemplate agreementTemplate);

        AgreementTemplate FindAgreementTemplate(Expression<Func<AgreementTemplate, bool>> predicate);

        IList<AgreementTemplate> FindAgreementTemplates(Expression<Func<AgreementTemplate, bool>> predicate);

        AgreementTemplate GetAgreementTemplate(int templateId);

        AgreementTemplate RemoveAgreementTemplate(AgreementTemplate agreementTemplate);
    }
}
