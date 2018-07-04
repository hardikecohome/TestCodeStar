using System.Collections.Generic;

namespace DealnetPortal.Domain.Repositories
{
    public interface ILicenseDocumentRepository
    {
        IList<LicenseDocument> GetAllLicenseDocuments();
    }
}
