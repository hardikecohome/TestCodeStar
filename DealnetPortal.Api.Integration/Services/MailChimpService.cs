using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Models.CustomerWallet;
using MailChimp.Net;
using MailChimp.Net.Core;
using MailChimp.Net.Models;
using DealnetPortal.Api.Models.Notification;

namespace DealnetPortal.Api.Integration.Services
{
    public class MailChimpService : IMailChimpService
    {
        private readonly MailChimpManager _manager = new MailChimpManager(ConfigurationManager.AppSettings["MailChimpApiKey"]); 

        public async Task AddNewSubscriberAsync(string listid, MailChimpMember member)
        {
            Member subscriber = new Member();
            subscriber.EmailAddress = member.Email;
            subscriber.MergeFields["FNAME"] = member.FirstName;
            subscriber.MergeFields["LNAME"] = member.LastName;
            subscriber.EmailType = "HTML";
            subscriber.StatusIfNew = Status.Pending;
            subscriber.ListId = listid;
            subscriber.MergeFields["ADDRESS"] = new
            {
                addr1 = member.address.Street,
                addr2 = member.address.Unit,
                city = member.address.City,
                state = member.address.State,
                zip = member.address.PostalCode,
                country = member.address.Country
            };
            subscriber.MergeFields["CREDITAMT"] = member.CreditAmount;
            subscriber.MergeFields["APPSTATUS"] = member.ApplicationStatus;
            subscriber.MergeFields["EQUIPINFO"] = member.EquipmentInfoRequired;
            subscriber.MergeFields["TPASSWORD"] = member.TemporaryPassword;
            subscriber.MergeFields["CLOSINGREQ"] = member.ClosingDateRequired;
            subscriber.MergeFields["CWLINK"] = member.OneTimeLink;
            subscriber.MergeFields["DEALERLEAD"] = member.DealerLeadAccepted;
            try
            {

                await _manager.Members.AddOrUpdateAsync(listid, subscriber);
            }
            catch (MailChimpException mce)
            {
                throw mce;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<Queue> SendUpdateNotification(string email)
        {
            try
            {
                Queue q = await _manager.AutomationEmailQueues.AddSubscriberAsync("154521", "154525", email);
                return q;
            }
            catch (MailChimpException me)
            {
                throw me;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<bool> isSubscriber(string listid, string emailid)
        {
            try
            {
                var subscriber = await _manager.Members.GetAsync(listid, emailid);
                return subscriber.Status == Status.Subscribed;
            }
            catch (Exception ex)
            {
                //throw ex;
            }
            return false;
        }
        public async Task<bool> isUnsubscribed(string listid, string emailid)
        {
            try
            {
                var subscriber = await _manager.Members.GetAsync(listid, emailid);
                return subscriber.Status == Status.Unsubscribed;
            }
            catch (Exception ex)
            {
                //throw ex;
            }
            return false;
        }
    }
}
