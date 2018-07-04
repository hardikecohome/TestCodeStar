using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DealnetPortal.Utilities.Messaging
{
    public interface IEmailService
    {
        Task SendAsync(IList<string> recipients, string from, string subject, string body);

        Task SendAsync(MailMessage message);
    }
}
