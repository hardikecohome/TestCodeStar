using System.Threading.Tasks;
using DealnetPortal.Api.Models.Notification;
using MailChimp.Net.Models;

namespace DealnetPortal.Api.Integration.Interfaces
{
    public interface IMailChimpService
    {
        Task AddNewSubscriberAsync(string listid, MailChimpMember member);
        Task<Queue> SendUpdateNotification(string email);
        Task<bool> isSubscriber(string listid, string emailid);
    }
}