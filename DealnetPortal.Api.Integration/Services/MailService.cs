using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.DataAccess.Repositories;
using DealnetPortal.Domain;
using System.Globalization;
using System.Text.RegularExpressions;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Utilities.Logging;
using DealnetPortal.Utilities.Messaging;
using DealnetPortal.Api.Models.Notification;
using DealnetPortal.Api.Models.Notify;
using DealnetPortal.Domain.Repositories;

namespace DealnetPortal.Api.Integration.Services
{
    public class MailService : IMailService
    {
        private readonly IEmailService _emailService;
        private readonly ILoggingService _loggingService;
        private readonly IContractRepository _contractRepository;
        private readonly ISmsSubscriptionService _smsSubscriptionServive;
        private readonly IPersonalizedMessageService _personalizedMessageService;
        private readonly IMailChimpService _mailChimpService;
        private readonly IMandrillService _mandrillService;

        public MailService(IEmailService emailService, IContractRepository contractRepository, ILoggingService loggingService, IPersonalizedMessageService personalizedMessageService, IMailChimpService mailChimpService, IMandrillService mandrillService, ISmsSubscriptionService smsSubscriptionServive)
        {
            _emailService = emailService;
            _contractRepository = contractRepository;
            _loggingService = loggingService;
            _personalizedMessageService = personalizedMessageService;
            _mailChimpService = mailChimpService;
            _mandrillService = mandrillService;
            _smsSubscriptionServive = smsSubscriptionServive;
        }

        #region DP
        public async Task<IList<Alert>> SendContractSubmitNotification(ContractDTO contract, string dealerEmail, bool success = true)
        {
            var alerts = new List<Alert>();
            var id = contract.Details?.TransactionId ?? contract.Id.ToString();
            var subject = string.Format(success ? Resources.Resources.ContractWasSuccessfullySubmitted : Resources.Resources.ContractWasDeclined, id);
            var body = new StringBuilder();
            body.AppendLine(subject);
            body.AppendLine($"{Resources.Resources.TypeOfApplication} {contract.Equipment.AgreementType.GetEnumDescription()}");
            body.AppendLine($"{Resources.Resources.HomeOwnersName} {contract.PrimaryCustomer?.FirstName} {contract.PrimaryCustomer?.LastName}");
            await SendNotification(body.ToString(), subject, contract, dealerEmail, alerts);

            if (alerts.All(a => a.Type != AlertType.Error))
            {
                _loggingService.LogInfo($"Email notifications for contract [{contract.Id}] was sent");
            }

            return alerts;
        }

        public async Task<IList<Alert>> SendContractChangeNotification(ContractDTO contract, string dealerEmail)
        {
            var alerts = new List<Alert>();

            var id = contract.Details?.TransactionId ?? contract.Id.ToString();
            var subject = string.Format(Resources.Resources.ContractWasSuccessfullyChanged, id);
            var body = new StringBuilder();
            body.AppendLine(subject);
            body.AppendLine($"{Resources.Resources.TypeOfApplication} {contract.Equipment.AgreementType.GetEnumDescription()}");
            body.AppendLine(
                $"{Resources.Resources.HomeOwnersName} {contract.PrimaryCustomer?.FirstName} {contract.PrimaryCustomer?.LastName}");
            await SendNotification(body.ToString(), subject, contract, dealerEmail, alerts);

            if (alerts.All(a => a.Type != AlertType.Error))
            {
                _loggingService.LogInfo($"Email notifications for contract [{contract.Id}] was sent");
            }

            return alerts;
        }

        public async Task SendDealerLoanFormContractCreationNotification(CustomerFormDTO customerFormData, CustomerContractInfoDTO contractData, string dealerProvince)
        {
            try
            {
                await _mandrillService.SendDealerCustomerLinkFormSubmittedNotification(customerFormData, contractData, dealerProvince);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email", ex);
            }
        }

        public async Task SendCustomerLoanFormContractCreationNotification(string customerEmail, CustomerContractInfoDTO contractData, string dealerColor, byte[] dealerLogo)
        {
            var location = contractData.DealerAdress;
            var html = File.ReadAllText(HostingEnvironment.MapPath(@"~\Content\emails\customer-notification-email.html"));
            var bodyBuilder = new StringBuilder(html, html.Length * 2);
            bodyBuilder.Replace("{headerColor}", dealerColor ?? "#2FAE00");
            bodyBuilder.Replace("{thankYouForApplying}", Resources.Resources.ThankYouForApplyingForFinancing);
            bodyBuilder.Replace("{youHaveBeenPreapprovedFor}", contractData.CreditAmount != 0 ? Resources.Resources.YouHaveBeenPreapprovedFor.Replace("{0}", contractData.CreditAmount.ToString("N0", CultureInfo.InvariantCulture)) : string.Empty);
            bodyBuilder.Replace("{yourApplicationWasSubmitted}", Resources.Resources.YourFinancingApplicationWasSubmitted);
            bodyBuilder.Replace("{willContactYouSoon}", Resources.Resources.WillContactYouSoon.Replace("{0}", contractData.DealerName ?? Resources.Resources.Dealer));
            LinkedResource inlineLogo = null;
            var inlineSuccess = new LinkedResource(HostingEnvironment.MapPath(@"~\Content\emails\images\icon-success.png"));
            inlineSuccess.ContentId = Guid.NewGuid().ToString();
            inlineSuccess.ContentType.MediaType = "image/png";
            bodyBuilder.Replace("{successIcon}", "cid:" + inlineSuccess.ContentId);

            var body = bodyBuilder.ToString();
            if (contractData.DealerEmail == null && contractData.DealerPhone == null &&
                contractData.DealerAdress == null && contractData.DealerName == null)
            {
                var contactSectionPattern = @"{ContactSectionStart}(.*?){ContactSectionEnd}";
                body = Regex.Replace(body, contactSectionPattern, "", RegexOptions.Singleline);
            }
            else
            {
                var contactSectionTagsPattern = @"{ContactSection(.*?)}";
                body = Regex.Replace(body, contactSectionTagsPattern, "", RegexOptions.Singleline);
                body = body.Replace("{ifYouHavePleaseContact}", Resources.Resources.IfYouHaveQuestionsPleaseContact);
                body = body.Replace("{dealerName}", contractData.DealerName ?? "");
                body = body.Replace("{dealerAddress}", location != null ? $"{location?.Street}, {location?.City}, {location?.State}, {location?.PostalCode}" : "");
                if (contractData.DealerPhone == null)
                {
                    var phoneSectionPattern = @"{PhoneSectionStart}(.*?){PhoneSectionEnd}";
                    body = Regex.Replace(body, phoneSectionPattern, "", RegexOptions.Singleline);
                }
                else
                {
                    var phoneSectionTagsPattern = @"{PhoneSection(.*?)}";
                    body = Regex.Replace(body, phoneSectionTagsPattern, "", RegexOptions.Singleline);
                    body = body.Replace("{phone}", Resources.Resources.Phone);
                    body = body.Replace("{dealerPhone}", contractData.DealerPhone);
                }
                if (contractData.DealerEmail == null)
                {
                    var mailSectionPattern = @"{MailSectionStart}(.*?){MailSectionEnd}";
                    body = Regex.Replace(body, mailSectionPattern, "", RegexOptions.Singleline);
                }
                else
                {
                    var mailSectionTagsPattern = @"{MailSection(.*?)}";
                    body = Regex.Replace(body, mailSectionTagsPattern, "", RegexOptions.Singleline);
                    body = body.Replace("{mail}", Resources.Resources.Email);
                    body = body.Replace("{dealerMail}", contractData.DealerEmail);
                }
            }

            if (dealerLogo == null)
            {
                var logoPattern = @"{LogoStart}(.*?){LogoEnd}";
                body = Regex.Replace(body, logoPattern, "", RegexOptions.Singleline);
            }
            else
            {
                var logoTagsPattern = @"{Logo(.*?)}";
                body = Regex.Replace(body, logoTagsPattern, "", RegexOptions.Singleline);
                inlineLogo = new LinkedResource(new MemoryStream(dealerLogo));
                inlineLogo.ContentId = Guid.NewGuid().ToString();
                inlineLogo.ContentType.MediaType = "image/png";
                body = body.Replace("{dealerLogo}", "cid:" + inlineLogo.ContentId);
            }
            var alternateView = AlternateView.CreateAlternateViewFromString(body, null,
                    MediaTypeNames.Text.Html);
            alternateView.LinkedResources.Add(inlineSuccess);
            if (inlineLogo != null)
            {
                alternateView.LinkedResources.Add(inlineLogo);
            }

            var mail = new MailMessage();
            mail.IsBodyHtml = true;
            mail.AlternateViews.Add(alternateView);
            mail.From = new MailAddress(contractData.DealerEmail);
            mail.To.Add(customerEmail);
            mail.Subject = Resources.Resources.ThankYouForApplyingForFinancing;
            try
            {
                await _emailService.SendAsync(mail);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email", ex);
            }
        }
        #endregion

        #region Public MB
        public async Task SendInviteLinkToCustomer(Contract customerFormData, string password)
        {
            string customerEmail = customerFormData.PrimaryCustomer.Emails.FirstOrDefault(m => m.EmailType == EmailType.Main)?.EmailAddress ?? string.Empty;
            string domain = ConfigurationManager.AppSettings["CustomerWalletClient"];
            string hashLogin = SecurityUtils.Hash(customerEmail);
           
            MailChimpMember member = new MailChimpMember()
            {
                Email = customerFormData.PrimaryCustomer.Emails.FirstOrDefault().EmailAddress,
                FirstName = customerFormData.PrimaryCustomer.FirstName,
                LastName = customerFormData.PrimaryCustomer.LastName,
                address = new MemberAddress()
                {
                    Street = customerFormData.PrimaryCustomer.Locations.FirstOrDefault().Street,
                    Unit = customerFormData.PrimaryCustomer.Locations.FirstOrDefault().Unit,
                    City = customerFormData.PrimaryCustomer.Locations.FirstOrDefault().City,
                    State = customerFormData.PrimaryCustomer.Locations.FirstOrDefault().State,
                    PostalCode = customerFormData.PrimaryCustomer.Locations.FirstOrDefault().PostalCode
                },
                CreditAmount = (decimal)customerFormData.Details.CreditAmount,
                ApplicationStatus = customerFormData.ContractState.ToString(),
                TemporaryPassword = password,
                EquipmentInfoRequired = "Required",
                OneTimeLink = domain + "/invite/" + hashLogin
            };
            try
            {
                if (customerFormData.PrimaryCustomer.Phones.Any(c => c.PhoneType == PhoneType.Cell))
                {
                    var result = await _smsSubscriptionServive.SetStartSubscription(customerFormData.PrimaryCustomer.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Cell).PhoneNum,
                                                                                    customerFormData.PrimaryCustomer.Id.ToString(),
                                                                                  ConfigurationManager.AppSettings["SmsAffiliateCode"],
                                                                                ConfigurationManager.AppSettings["SubscriptionRef"]);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send Sms subscription request from SendInviteLinkToCustomer", ex);
            }
            try
            {
                // Hardik MailChimp trigger for subscription request
                await _mailChimpService.AddNewSubscriberAsync(ConfigurationManager.AppSettings["ListID"], member);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email from SendInviteLinkToCustomer", ex);
            }
        }

        public async Task SendHomeImprovementMailToCustomer(IList<Contract> succededContracts)
        {
            var contract = succededContracts.First();
            string services = string.Join(",", succededContracts.Select(i => (i.Equipment.NewEquipment.First()?.Description ??
                _contractRepository.GetEquipmentTypeInfo(i.Equipment.NewEquipment.First()?.Type)?.Description)?.ToLower()));

            var subject = $"{Resources.Resources.WeAreLookingForTheBestProfessionalForYourHomeImprovementProject}";
           
            try
            {
                if (await _mailChimpService.isSubscriber(ConfigurationManager.AppSettings["ListID"], contract.PrimaryCustomer.Emails.FirstOrDefault().EmailAddress) || await _mailChimpService.isSubscriber(ConfigurationManager.AppSettings["RegistrationListID"], contract.PrimaryCustomer.Emails.FirstOrDefault().EmailAddress))
                {
                    await _mandrillService.SendHomeImprovementTypeUpdatedConfirmation(contract.PrimaryCustomer.Emails.FirstOrDefault().EmailAddress,
                                                                                        contract.PrimaryCustomer.FirstName,
                                                                                        contract.PrimaryCustomer.LastName,
                                                                                        services);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email from SendHomeImprovementMailToCustomer", ex);
            }
            try
            {
                if (contract.PrimaryCustomer.Phones.Any(c => c.PhoneType == PhoneType.Cell))
                {
                    var result = await _personalizedMessageService.SendMessage(contract.PrimaryCustomer.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Cell).PhoneNum, subject);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send Sms from SendHomeImprovementMailToCustomer", ex);
            }
        }

        public async Task SendDeclinedConfirmation(string emailid, string firstName, string lastName)
        {
            try
            {
                await _mandrillService.SendDeclineNotificationConfirmation(emailid, firstName, lastName);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email SendDeclinedConfirmation", ex);
            }

        }

        public async Task SendApprovedMailToCustomer(Contract customerFormData)
        {
            var subject = $"{Resources.Resources.Congratulations}, {Resources.Resources.YouHaveBeen} {Resources.Resources.PreApproved.ToLower()} {Resources.Resources.For} ${customerFormData.Details.CreditAmount.Value.ToString("N0", CultureInfo.InvariantCulture)}";
            MailChimpMember member = new MailChimpMember()
            {
                Email = customerFormData.PrimaryCustomer.Emails.FirstOrDefault().EmailAddress,
                FirstName = customerFormData.PrimaryCustomer.FirstName,
                LastName = customerFormData.PrimaryCustomer.LastName,
                address = new MemberAddress()
                {
                    Street = customerFormData.PrimaryCustomer.Locations.FirstOrDefault().Street,
                    Unit = customerFormData.PrimaryCustomer.Locations.FirstOrDefault().Unit,
                    City = customerFormData.PrimaryCustomer.Locations.FirstOrDefault().City,
                    State = customerFormData.PrimaryCustomer.Locations.FirstOrDefault().State,
                    PostalCode = customerFormData.PrimaryCustomer.Locations.FirstOrDefault().PostalCode
                },
                CreditAmount = (decimal)customerFormData.Details.CreditAmount,
                ApplicationStatus = customerFormData.ContractState.ToString(),
            };

            try
            {
                //Hardik MailChimp Trigger to update CreditAmount
                await _mailChimpService.AddNewSubscriberAsync(ConfigurationManager.AppSettings["ListID"], member);

            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email from SendApprovedMailToCustomer", ex);
            }
            try
            {
                if (customerFormData.PrimaryCustomer.Phones.Any(c => c.PhoneType == PhoneType.Cell))
                {
                    var result = await _personalizedMessageService.SendMessage(customerFormData.PrimaryCustomer.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Cell).PhoneNum, subject);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send Sms from SendApprovedMailToCustomer", ex);
            }
        }

        public async Task SendCustomerDealerAcceptLead(Contract contract, DealerDTO dealer)
        {
            string services = contract.Equipment.NewEquipment != null ? string.Join(",", contract.Equipment.NewEquipment.Select(i => (i.Description ?? _contractRepository.GetEquipmentTypeInfo(i?.Type)?.Description)?.ToLower())) : string.Empty;
            var subject = $"{Resources.Resources.WeFoundHomeProfessionalForYourHomeImprovementProject}";
            
            try
            {
                if (await _mailChimpService.isSubscriber(ConfigurationManager.AppSettings["ListID"], contract.PrimaryCustomer.Emails?.FirstOrDefault()?.EmailAddress) || await _mailChimpService.isSubscriber(ConfigurationManager.AppSettings["RegistrationListID"], contract.PrimaryCustomer.Emails?.FirstOrDefault()?.EmailAddress))
                {

                    await _mandrillService.SendDealerLeadAccepted(contract, dealer, services);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send Dealer Accept Lead email from SendCustomerDealerAcceptLead", ex);
            }
            try
            {
                if (contract.PrimaryCustomer.Phones.Any(c => c.PhoneType == PhoneType.Cell))
                {
                    var result = await _personalizedMessageService.SendMessage(contract.PrimaryCustomer.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Cell).PhoneNum, subject);
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send Dealer Accept Lead SMS from SendCustomerDealerAcceptLead", ex);
            }
        }
        #endregion

        #region Public DealNet mails

        public async Task SendNotifyMailNoDealerAcceptLead(Contract contract)
        {
            string equipment = contract.Equipment.NewEquipment?.FirstOrDefault()?.EquipmentType?.Description?.ToLower() ?? string.Empty;
            var location = contract.PrimaryCustomer.Locations?.FirstOrDefault(l => l.AddressType == AddressType.InstallationAddress);
            string customerEmail = contract.PrimaryCustomer.Emails?.FirstOrDefault(m => m.EmailType == EmailType.Main)?.EmailAddress ?? string.Empty;
            string mailTo = ConfigurationManager.AppSettings["DealNetEmail"];
            var homePhone = contract?.PrimaryCustomer?.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Home)?.PhoneNum ?? string.Empty;
            var businessPhone = contract?.PrimaryCustomer?.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Business)?.PhoneNum ?? string.Empty;
            var mobilePhone = contract?.PrimaryCustomer?.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Cell)?.PhoneNum ?? string.Empty;

            var body = new StringBuilder();
            body.AppendLine("<div>");
            body.AppendLine($"<u>{Resources.Resources.ThereAreNoDealersMatchingFollowingLead}.</u>");
            body.AppendLine($"<p>{Resources.Resources.TransactionId}: {contract.Details?.TransactionId ?? contract.Id.ToString()}</p>");
            body.AppendLine($"<p><b>{Resources.Resources.Client}: {contract.PrimaryCustomer.FirstName} {contract.PrimaryCustomer.LastName}</b></p>");
            body.AppendLine($"<p><b>{Resources.Resources.PreApproved}: ${contract.Details?.CreditAmount?.ToString("N0", CultureInfo.InvariantCulture)}</b></p>");
            body.AppendLine($"<p><b>{Resources.Resources.HomeImprovementType}: {equipment}</b></p>");
            if (!string.IsNullOrEmpty(contract.Details?.Notes))
            {
                body.AppendLine($"<p>{Resources.Resources.ClientsComment}: <i>{contract.Details.Notes}</i></p>");
            }
            body.AppendLine("<br />");
            body.AppendLine($"<p><b>{Resources.Resources.InstallationAddress}:</b></p>");
            body.AppendLine($"<p>{location?.Street ?? string.Empty}</p>");
            body.AppendLine($"<p>{location?.City ?? string.Empty}, {location?.State ?? string.Empty} {location?.PostalCode ?? string.Empty}</p>");
            body.AppendLine("<br />");
            body.AppendLine($"<p><b>{Resources.Resources.ContactInformation}:</b></p>");
            body.AppendLine("<ul>");
            if (!string.IsNullOrEmpty(homePhone))
            {
                body.AppendLine($"<li>{Resources.Resources.HomePhone}: {homePhone}</li>");
            }
            if (!string.IsNullOrEmpty(mobilePhone))
            {
                body.AppendLine($"<li>{Resources.Resources.MobilePhone}: {mobilePhone}</li>");
            }
            if (!string.IsNullOrEmpty(businessPhone))
            {
                body.AppendLine($"<li>{Resources.Resources.BusinessPhone}: {businessPhone}</li>");
            }
            if (!string.IsNullOrEmpty(customerEmail))
            {
                body.AppendLine($"<li>{Resources.Resources.EmailAddress}: {customerEmail}</li>");
            }
            if (contract.PrimaryCustomer.PreferredContactMethod.HasValue)
            {
                body.AppendLine($"<li>{Resources.Resources.PreferredContactMethod}: {contract.PrimaryCustomer.PreferredContactMethod.Value}</li>");
            }
            body.AppendLine("</ul>");
            body.AppendLine("</div>");

            var subject = string.Format(Resources.Resources.NoDealersMatchingCustomerLead, equipment, location?.PostalCode ?? string.Empty);
            try
            {
                await _emailService.SendAsync(new List<string> { mailTo }, string.Empty, subject, body.ToString());
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email", ex);
            }
        }

        public void SendNotifyMailNoDealerAcceptedLead12H(Contract contract)
        {
            string equipment = contract.Equipment.NewEquipment?.First().Description.ToLower() ?? string.Empty;
            var location = contract.PrimaryCustomer.Locations?.FirstOrDefault(l => l.AddressType == AddressType.InstallationAddress);
            string customerEmail = contract.PrimaryCustomer.Emails.FirstOrDefault(m => m.EmailType == EmailType.Main)?.EmailAddress ?? string.Empty;
            string mailTo = ConfigurationManager.AppSettings["DealNetEmail"];
            var homePhone = contract?.PrimaryCustomer?.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Home)?.PhoneNum ?? string.Empty;
            var businessPhone = contract?.PrimaryCustomer?.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Business)?.PhoneNum ?? string.Empty;
            var mobilePhone = contract?.PrimaryCustomer?.Phones?.FirstOrDefault(p => p.PhoneType == PhoneType.Cell)?.PhoneNum ?? string.Empty;
            var expireperiod = int.Parse(ConfigurationManager.AppSettings["LeadExpiredMinutes"]) / 60;

            var body = new StringBuilder();
            body.AppendLine("<div>");
            body.AppendLine($"<u>{string.Format(Resources.Resources.FollowingLeadHasNotBeenAcceptedByAnyDealerFor12h, expireperiod)}.</u>");
            body.AppendLine($"<p>{Resources.Resources.TransactionId}: {contract.Details?.TransactionId ?? contract.Id.ToString()}</p>");
            body.AppendLine($"<p><b>{Resources.Resources.Client}: {contract.PrimaryCustomer.FirstName} {contract.PrimaryCustomer.LastName}</b></p>");
            body.AppendLine($"<p><b>{Resources.Resources.PreApproved}: ${contract.Details.CreditAmount.Value.ToString("N0", CultureInfo.InvariantCulture)}</b></p>");
            body.AppendLine($"<p><b>{Resources.Resources.HomeImprovementType}: {equipment}</b></p>");
            if (!string.IsNullOrEmpty(contract.Details?.Notes))
            {
                body.AppendLine($"<p>{Resources.Resources.ClientsComment}: <i>{contract.Details.Notes}</i></p>");
            }
            body.AppendLine("<br />");
            body.AppendLine($"<p><b>{Resources.Resources.InstallationAddress}:</b></p>");
            body.AppendLine($"<p>{location?.Street ?? string.Empty}</p>");
            body.AppendLine($"<p>{location?.City ?? string.Empty}, {location?.State ?? string.Empty} {location?.PostalCode ?? string.Empty}</p>");
            body.AppendLine("<br />");
            body.AppendLine($"<p><b>{Resources.Resources.ContactInformation}:</b></p>");
            body.AppendLine("<ul>");
            if (!string.IsNullOrEmpty(homePhone))
            {
                body.AppendLine($"<li>{Resources.Resources.HomePhone}: {homePhone}</li>");
            }
            if (!string.IsNullOrEmpty(mobilePhone))
            {
                body.AppendLine($"<li>{Resources.Resources.MobilePhone}: {mobilePhone}</li>");
            }
            if (!string.IsNullOrEmpty(businessPhone))
            {
                body.AppendLine($"<li>{Resources.Resources.BusinessPhone}: {businessPhone}</li>");
            }
            if (!string.IsNullOrEmpty(customerEmail))
            {
                body.AppendLine($"<li>{Resources.Resources.EmailAddress}: {customerEmail}</li>");
            }
            if (contract.PrimaryCustomer.PreferredContactMethod.HasValue)
            {
                body.AppendLine($"<li>{Resources.Resources.PreferredContactMethod}: {contract.PrimaryCustomer.PreferredContactMethod.Value}</li>");
            }
            body.AppendLine("</ul>");
            body.AppendLine("</div>");



            var subject = string.Format(Resources.Resources.CustomerLeadHasNotBeenAcceptedByAnyDealerFor, expireperiod, equipment, location?.PostalCode ?? string.Empty);
            try
            {
                _emailService.SendAsync(new List<string> { mailTo }, string.Empty, subject, body.ToString());
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email", ex);
            }
        }

        public async Task SendProblemsWithSubmittingOnboarding(string errorMsg, int dealerInfoId, string accessKey)
        {
            try
            {
                await _mandrillService.SendProblemsWithSubmittingOnboarding(errorMsg, dealerInfoId, accessKey);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email SendProblemsWithSubmittingOnboarding", ex);
            }

        }

        public async Task SendDraftLinkMail(string accessKey, string email)
        {
            try
            {
                await _mandrillService.SendDraftLinkMail(accessKey, email);
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email SendProblemsWithSubmittingOnboarding", ex);
            }

        }

        public async Task SendSupportRequiredEmail(SupportRequestDTO SupportDetails, string dealerProvince)
        {
            string mailTo = ""; /*ConfigurationManager.AppSettings["DealNetEmail"];*/
            switch (SupportDetails.SupportType)
            {
                case SupportTypeEnum.creditDecision:

                    mailTo = dealerProvince == "QC" ? ConfigurationManager.AppSettings["QuebecCreditDecisionDealNetEmail"] : ConfigurationManager.AppSettings["CreditDecisionDealNetEmail"];
                    break;
                case SupportTypeEnum.dealerProfileUpdate:
                case SupportTypeEnum.portalInquiries:
                case SupportTypeEnum.programInquiries:
                    mailTo = dealerProvince == "QC" ? ConfigurationManager.AppSettings["QuebecCreditDocsDealNetEmail"] : ConfigurationManager.AppSettings["CreditDocsDealNetEmail"];
                    break;
                case SupportTypeEnum.pendingDeals:
                case SupportTypeEnum.fundedDeals:
                    mailTo = dealerProvince == "QC" ? ConfigurationManager.AppSettings["QuebecFundingDocsDealNetEmail"] : ConfigurationManager.AppSettings["FundingDocsDealNetEmail"];
                    break;
                default:
                    mailTo = dealerProvince == "QC" ? ConfigurationManager.AppSettings["QuebecOtherDealNetEmail"] : ConfigurationManager.AppSettings["OtherDealNetEmail"];
                    break;
            }
            try
            {
                await _mandrillService.SendSupportRequiredEmail(SupportDetails, mailTo);
                //await _emailService.SendAsync(new List<string> { mailTo }, string.Empty, subject, body.ToString());
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email", ex);
            }
        }

        public async Task SendDeclineToSign(Contract contract, string dealerProvince)
        {
            var declinedSigner = contract.Signers.SingleOrDefault(s => s.SignatureStatus == SignatureStatus.Declined && s.SignerType != SignatureRole.Dealer);
            
            try
            {
                if (declinedSigner != null)
                {
                    var declinedCustomer = _contractRepository.GetCustomer(declinedSigner.CustomerId.Value);
                    if (declinedCustomer != null)
                    {
                        var phone =
                            declinedCustomer.Phones.SingleOrDefault(x => x.PhoneType == PhoneType.Cell)?.PhoneNum ??
                            declinedCustomer.Phones.SingleOrDefault(x => x.PhoneType == PhoneType.Business)?.PhoneNum ??
                            declinedCustomer.Phones.SingleOrDefault(x => x.PhoneType == PhoneType.Home)?.PhoneNum;
                        var agreementType = contract.Equipment.AgreementType == AgreementType.LoanApplication
                            ? "loan"
                            : "rental";
                        var email = declinedCustomer.Emails.SingleOrDefault(x => x.EmailType == EmailType.Main)?.EmailAddress ?? string.Empty;

                        await _mandrillService.SendDeclineToSignDealerNotification(contract.Dealer.Email,
                            contract.Dealer.AspireLogin, contract.Details.TransactionId,
                            declinedCustomer.FirstName + " " + declinedCustomer.LastName,
                            email,
                            phone, agreementType, dealerProvince);
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Cannot send email SendDeclineToSign", ex);
            }

        }
        #endregion

        #region Private
        private async Task SendNotification(string body, string subject, ContractDTO contract, string dealerEmail, List<Alert> alerts)
        {
            if (contract != null)
            {
                var recipients = GetContractRecipients(contract, dealerEmail);

                if (recipients.Any())
                {
                    try
                    {
                        foreach (var recipient in recipients)
                        {
                            await _emailService.SendAsync(new List<string>() { recipient }, string.Empty, subject, body);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = "Can't send notification email";
                        _loggingService.LogError("Can't send notification email", ex);
                        alerts.Add(new Alert()
                        {
                            Header = errorMsg,
                            Message = ex.ToString(),
                            Type = AlertType.Error
                        });
                    }
                }
                else
                {
                    var errorMsg = $"Can't get recipients list for contract [{contract.Id}]";
                    _loggingService.LogError(errorMsg);
                    alerts.Add(new Alert()
                    {
                        Header = "Can't get recipients list",
                        Message = errorMsg,
                        Type = AlertType.Error
                    });
                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Header = "Can't get contract",
                    Message = $"Can't get contract with id {contract.Id}",
                    Type = AlertType.Error
                });
                _loggingService.LogError($"Can't get contract with id {contract.Id}");
            }

            if (alerts.All(a => a.Type != AlertType.Error))
            {
                _loggingService.LogInfo($"Email notifications for contract [{contract.Id}] was sent");
            }
        }
        
        private IList<string> GetContractRecipients(ContractDTO contract, string dealerEmail)
        {
            var recipients = new List<string>();

            if (contract.PrimaryCustomer?.Emails?.Any() ?? false)
            {
                recipients.Add(contract.PrimaryCustomer.Emails.FirstOrDefault(e => e.EmailType == EmailType.Main)?.EmailAddress ??
                    contract.PrimaryCustomer.Emails.First().EmailAddress);
            }

            if (contract?.SecondaryCustomers?.Any() ?? false)
            {
                contract.SecondaryCustomers.ForEach(c =>
                {
                    if (c.Emails?.Any() ?? false)
                    {
                        recipients.Add(c.Emails.FirstOrDefault(e => e.EmailType == EmailType.Main)?.EmailAddress ??
                            c.Emails.First().EmailAddress);
                    }
                });
            }

            //TODO: dealer and ODI/Ecohome team
            if (!string.IsNullOrEmpty(dealerEmail))
            {
                recipients.Add(dealerEmail);
            }

            return recipients;
        }
        #endregion
    }
}
