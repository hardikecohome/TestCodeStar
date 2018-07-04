using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Integration.Interfaces;
using DealnetPortal.Api.Models.Notification;
using DealnetPortal.Domain;
using DealnetPortal.Api.Models.Contract;
using DealnetPortal.Api.Models.Notify;
using DealnetPortal.Api.Core.Helpers;
using DealnetPortal.Api.Common.Enumeration;
using System.Globalization;
using DealnetPortal.Api.Common.Helpers;

namespace DealnetPortal.Api.Integration.Services
{
    public class MandrillService : IMandrillService
    {
        public static string _endPoint { get; set; }
        public static string _apiKey { get; set; }

        public MandrillService()
        {
            _endPoint = ConfigurationManager.AppSettings["MandrillEndPoint"];
            _apiKey = ConfigurationManager.AppSettings["MandrillApiKey"];
        }

        public async Task<HttpResponseMessage> SendEmail(MandrillRequest request)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(ConfigurationManager.AppSettings["MandrillEndPoint"]);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                try
                {
                    
                    return await client.PostAsJsonAsync("/api/1.0/messages/send-template.json", request);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public async Task SendDealerLeadAccepted(Contract contract, DealerDTO dealer, string services)
        {
            string emailid = contract.PrimaryCustomer.Emails.FirstOrDefault().EmailAddress;

            MandrillRequest request = new MandrillRequest();
            List<Variable> myVariables = new List<Variable>();
            myVariables.Add(new Variable() { name = "FNAME", content = contract.PrimaryCustomer.FirstName });
            myVariables.Add(new Variable() { name = "LNAME", content = contract.PrimaryCustomer.LastName });
            myVariables.Add(new Variable() { name = "EQUIPINFO", content = services });
            request.key = _apiKey;
            request.template_name = ConfigurationManager.AppSettings["DealerLeadAcceptedTemplate"];
            request.template_content = new List<templatecontent>() {
                new templatecontent(){
                    name="Dealer Accepted Lead",
                    content = "Dealer Accepted Your Lead"
                }
            };


            request.message = new MandrillMessage()
            {
                from_email = ConfigurationManager.AppSettings["FromEmail"],
                from_name = "Myhome Wallet by EcoHome Financial",
                html = null,
                merge_vars = new List<MergeVariable>() {
                    new MergeVariable(){
                        rcpt = emailid,
                        vars = myVariables
                             
                        
                    }
                },
                send_at = DateTime.Now,
                subject = "A contractor has accepted your project request",
                text = "A contractor has accepted your project request",
                to = new List<MandrillTo>() {
                    new MandrillTo(){
                        email =emailid,
                        name = contract.PrimaryCustomer.FirstName+" "+contract.PrimaryCustomer.LastName,
                        type = "to"
                    }
                }
            };
            try
            {
                var result = await SendEmail(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task SendHomeImprovementTypeUpdatedConfirmation(string emailid, string firstName, string lastName, string services)
        {
            MandrillRequest request = new MandrillRequest();
            List<Variable> myVariables = new List<Variable>();
            myVariables.Add(new Variable() { name = "FNAME", content = firstName });
            myVariables.Add(new Variable() { name = "LNAME", content = lastName });
            myVariables.Add(new Variable() { name = "EQUIPINFO", content = services });
            request.key = _apiKey;
            request.template_name = ConfigurationManager.AppSettings["HomeImprovementTypeUpdatedTemplate"];
            request.template_content = new List<templatecontent>() {
                    new templatecontent(){
                        name="We’re looking for the best professional for your home improvement project",
                        content = "We’re looking for the best professional for your home improvement project"
                    }
                };


            request.message = new MandrillMessage()
            {
                from_email = ConfigurationManager.AppSettings["FromEmail"],
                from_name = "Myhome Wallet by EcoHome Financial",
                html = null,
                merge_vars = new List<MergeVariable>() {
                        new MergeVariable(){
                            rcpt = emailid,
                            vars = myVariables


                        }
                    },
                send_at = DateTime.Now,
                subject = "We’re looking for the best professional for your home improvement project",
                text = "We’re looking for the best professional for your home improvement project",
                to = new List<MandrillTo>() {
                        new MandrillTo(){
                            email =emailid,
                            name = firstName+" "+lastName,
                            type = "to"
                        }
                    }
            };
            try
            {
                var result = await SendEmail(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task SendDeclineNotificationConfirmation(string emailid, string firstName, string lastName)
        {
            MandrillRequest request = new MandrillRequest();
            List<Variable> myVariables = new List<Variable>();
            myVariables.Add(new Variable() { name = "FNAME", content = firstName });
            myVariables.Add(new Variable() { name = "LNAME", content = lastName });
            request.key = _apiKey;
            request.template_name = ConfigurationManager.AppSettings["DeclinedOrCreditReviewTemplate"];
            request.template_content = new List<templatecontent>() {
                    new templatecontent(){
                        name="Declined",
                        content = "Declined"
                    }
                };


            request.message = new MandrillMessage()
            {
                from_email = ConfigurationManager.AppSettings["FromEmail"],
                from_name = "Myhome Wallet by EcoHome Financial",
                html = null,
                merge_vars = new List<MergeVariable>() {
                        new MergeVariable(){
                            rcpt = emailid,
                            vars = myVariables


                        }
                    },
                send_at = DateTime.Now,
                subject = "Unfortunately, we’re unable to process this application automatically",
                text = "Unfortunately, we’re unable to process this application automatically",
                to = new List<MandrillTo>() {
                        new MandrillTo(){
                            email = emailid,
                            name = firstName+" "+lastName,
                            type = "to"
                        }
                    }
            };
            try
            {
                var result = await SendEmail(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task SendProblemsWithSubmittingOnboarding(string errorMsg, int dealerInfoId, string accessKey)
        {
            var draftLink = ConfigurationManager.AppSettings[WebConfigKeys.DEALER_PORTAL_DRAFTURL_KEY] + accessKey;
            MandrillRequest request = new MandrillRequest();
            List<Variable> myVariables = new List<Variable>();
            myVariables.Add(new Variable() { name = "DealerInfoID", content = dealerInfoId.ToString() });
            myVariables.Add(new Variable() { name = "DealerUniqueLink", content = draftLink });
            myVariables.Add(new Variable() { name = "DealerErrorLog", content = errorMsg });
            request.key = _apiKey;
            request.template_name = ConfigurationManager.AppSettings["AspireServiceErrorTemplate"];
            request.template_content = new List<templatecontent>() {
                    new templatecontent(){
                        name= $"Exception while submitting onboarding application to Aspire (DealerInfoID = {dealerInfoId})",
                        content = $"Exception while submitting onboarding application to Aspire (DealerInfoID = {dealerInfoId})"
                    }
                };


            request.message = new MandrillMessage()
            {
                from_email = ConfigurationManager.AppSettings["FromEmail"],
                from_name = "EcoHome Financial",
                html = null,
                merge_vars = new List<MergeVariable>() {
                        new MergeVariable(){
                            rcpt = ConfigurationManager.AppSettings["DealNetErrorLogsEmail"],
                            vars = myVariables


                        }
                    },
                send_at = DateTime.Now,
                subject = $"Exception while submitting onboarding application to Aspire (DealerInfoID = {dealerInfoId})",
                text = $"Exception while submitting onboarding application to Aspire (DealerInfoID = {dealerInfoId})",
                to = new List<MandrillTo>() {
                        new MandrillTo(){
                            email = ConfigurationManager.AppSettings["DealNetErrorLogsEmail"],
                            name = " ",
                            type = "to"
                        }
                    }
            };
            try
            {
                var result = await SendEmail(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task SendDraftLinkMail(string accessKey, string email)
        {
            var draftLink = ConfigurationManager.AppSettings["DealerPortalDraftUrl"] + accessKey;
            MandrillRequest request = new MandrillRequest();
            List<Variable> myVariables = new List<Variable>();
            myVariables.Add(new Variable() { name = "DealerUniqueLink", content = draftLink });
            request.key = _apiKey;
            request.template_name = CultureHelper.CurrentCultureType == CultureType.French ? ConfigurationManager.AppSettings["DraftLinkTemplateFrench"] : ConfigurationManager.AppSettings["DraftLinkTemplate"];
            request.template_content = new List<templatecontent>() {
                    new templatecontent(){
                        name="Your EcoHome Financial dealer application link",
                        content = "Your EcoHome Financial dealer application link"
                    }
                };


            request.message = new MandrillMessage()
            {
                from_email = ConfigurationManager.AppSettings["FromEmail"],
                from_name = CultureHelper.CurrentCultureType == CultureType.French ? "Services financiers Ecohome" : "EcoHome Financial",
                html = null,
                merge_vars = new List<MergeVariable>() {
                        new MergeVariable(){
                            rcpt = email,
                            vars = myVariables


                        }
                    },
                send_at = DateTime.Now,
                subject = CultureHelper.CurrentCultureType == CultureType.French ? "Votre lien d'application de concessionnaire Services financiers Ecohome" : "Your EcoHome Financial dealer application link",
                text = "Your EcoHome Financial dealer application link",
                to = new List<MandrillTo>() {
                        new MandrillTo(){
                            email = email,
                            name = " ",
                            type = "to"
                        }
                    }
            };
            try
            {
                var result = await SendEmail(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task SendSupportRequiredEmail(SupportRequestDTO SupportDetails, string email)
        {
            string BestWay = "";
            BestWay = $"<strong> { SupportDetails.BestWay.GetEnumDescription() } : </strong> { SupportDetails.ContactDetails ?? string.Empty}";
            var supportTypeDescription = SupportDetails.SupportType.GetEnumDescription();
            
            MandrillRequest request = new MandrillRequest();
            List<Variable> myVariables = new List<Variable>();
            myVariables.Add(new Variable() { name = "RequestType", content = supportTypeDescription ?? "Not provided" });
            myVariables.Add(new Variable() { name = "DealerName", content = SupportDetails.DealerName });
            myVariables.Add(new Variable() { name = "YourName", content = SupportDetails.YourName });
            myVariables.Add(new Variable() { name = "LoanNumber", content = SupportDetails.LoanNumber ?? "Not provided" });
            myVariables.Add(new Variable() { name = "HelpRequested", content = SupportDetails.HelpRequested ?? "Not provided"});
            myVariables.Add(new Variable() { name = "BestWay", content = BestWay ?? "Not Provided"});
            request.key = _apiKey;
            request.template_name = ConfigurationManager.AppSettings["SupportRequestTemplate"];
            request.template_content = new List<templatecontent>() {
                    new templatecontent(){
                        name="Your EcoHome Financial dealer application link",
                        content = "Your EcoHome Financial dealer application link"
                    }
                };


            request.message = new MandrillMessage()
            {
                from_email = ConfigurationManager.AppSettings["FromEmail"],
                from_name = "EcoHome Financial",
                html = null,
                merge_vars = new List<MergeVariable>() {
                        new MergeVariable(){
                            rcpt = email,
                            vars = myVariables


                        }
                    },
                send_at = DateTime.Now,
                subject = $"Support Request - { supportTypeDescription }",
                text = $"Support Request - { supportTypeDescription }",
                to = new List<MandrillTo>() {
                        new MandrillTo(){
                            email = email,
                            name = " ",
                            type = "to"
                        }
                    }
            };
            try
            {
                var result = await SendEmail(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task SendDeclineToSignDealerNotification(string dealerEmail, string dealerName, string contractId, string customerName, string customerEmail, string customerPhone, string agreementType, string dealerProvince)
        {
            MandrillRequest request = new MandrillRequest();
            List<Variable> myVariables = new List<Variable>();
            myVariables.Add(new Variable() { name = "ContractNumber", content = contractId });
            myVariables.Add(new Variable() { name = "CustomerName", content = customerName });
            myVariables.Add(new Variable() { name = "CustomerEmail", content = customerEmail });
            myVariables.Add(new Variable() { name = "CustomerNumber", content = customerPhone });
            myVariables.Add(new Variable() { name = "AgreementType", content = agreementType });
            request.key = _apiKey;
            request.template_name = dealerProvince == "QC" ? ConfigurationManager.AppSettings["QuebecSignatureDeclineNotification"] : ConfigurationManager.AppSettings["SignatureDeclineNotification"];
            request.template_content = new List<templatecontent>() {
                    new templatecontent(){
                        name="Declined to sign",
                        content = "Declined to sign"
                    }
                };

            request.message = new MandrillMessage()
            {
                from_email = ConfigurationManager.AppSettings["FromEmail"],
                from_name = dealerProvince == "QC" ? "Services financiers Ecohome" : "EcoHome Financial",
                html = null,
                merge_vars = new List<MergeVariable>() {
                        new MergeVariable(){
                            rcpt = dealerEmail,
                            vars = myVariables


                        }
                    },
                send_at = DateTime.Now,
                subject = dealerProvince == "QC" ? $"Vous avez récemment soumis une {agreementType} a Services financiers Ecohome qui n'a pas été signé par le client / Action required: customer declined to sign a recently submitted {agreementType} agreement" : $"Action required: customer declined to sign a recently submitted {agreementType} agreement",
                text = $"Action required: customer declined to sign a recently submitted {agreementType} agreement",
                to = new List<MandrillTo>() {
                        new MandrillTo(){
                            email = dealerEmail,
                            name = dealerName,
                            type = "to"
                        }
                    }
            };
            try
            {
                var result = await SendEmail(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public async Task SendDealerCustomerLinkFormSubmittedNotification(CustomerFormDTO customerFormData, CustomerContractInfoDTO contractData, string dealerProvince)
        {
            var address = string.Empty;
            var approvalStatus = string.Empty;
            var addresItem = customerFormData.PrimaryCustomer.Locations.FirstOrDefault(ad => ad.AddressType == AddressType.MainAddress);
            if (addresItem != null)
            {
                address = $"{addresItem.Street}, {addresItem.City}, {addresItem.State}, {addresItem.PostalCode}";
            }
            if (contractData.CreditAmount > 0 && contractData.IsPreApproved)
            {
                approvalStatus = $"{Resources.Resources.PreApproved}: ${contractData.CreditAmount.ToString("N0", CultureInfo.InvariantCulture)}";
            }
            
            MandrillRequest request = new MandrillRequest();
            List<Variable> myVariables = new List<Variable>();
            myVariables.Add(new Variable() { name = "TransactionID", content = contractData.TransactionId });
            myVariables.Add(new Variable() { name = "CustomerName", content = $"{customerFormData.PrimaryCustomer.FirstName} {customerFormData.PrimaryCustomer.LastName}" });
            myVariables.Add(new Variable() { name = "CreditCheckStatus", content = approvalStatus });
            myVariables.Add(new Variable() { name = "SelectedTypeOfService", content = customerFormData.SelectedService ?? string.Empty });
            myVariables.Add(new Variable() { name = "Comment", content = customerFormData.CustomerComment ?? string.Empty });
            myVariables.Add(new Variable() { name = "InstallationAddress", content = address });
            myVariables.Add(new Variable() { name = "HomePhone", content = customerFormData.PrimaryCustomer.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Home)?.PhoneNum ?? string.Empty });
            myVariables.Add(new Variable() { name = "CellPhone", content = customerFormData.PrimaryCustomer.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Cell)?.PhoneNum ?? string.Empty });
            myVariables.Add(new Variable() { name = "BusinessPhone", content = customerFormData.PrimaryCustomer.Phones.FirstOrDefault(p => p.PhoneType == PhoneType.Business)?.PhoneNum ?? string.Empty });
            myVariables.Add(new Variable() { name = "CustomerEmail", content = customerFormData.PrimaryCustomer.Emails.FirstOrDefault(m => m.EmailType == EmailType.Main)?.EmailAddress ?? string.Empty });
            myVariables.Add(new Variable() { name = "LinkToDeal", content = $"{customerFormData.DealUri}/{contractData.ContractId}" });
            request.key = _apiKey;
            request.template_name = dealerProvince == "QC" ? ConfigurationManager.AppSettings["QuebecCustomerLinkFormNotification"] : ConfigurationManager.AppSettings["CustomerLinkFormNotification"];
            request.template_content = new List<templatecontent>() {
                    new templatecontent(){
                        name="Customer Link Form",
                        content = "Customer Link Form"
                    }
                };

            request.message = new MandrillMessage()
            {
                from_email = ConfigurationManager.AppSettings["FromEmail"],
                from_name = dealerProvince == "QC" ? "Services financiers Ecohome" : "EcoHome Financial",
                html = null,
                merge_vars = new List<MergeVariable>() {
                        new MergeVariable(){
                            rcpt = contractData.DealerEmail,
                            vars = myVariables
                        }
                    },
                send_at = DateTime.Now,
                subject = dealerProvince == "QC" ? "Merci d’avoir effectué une demande de financement / Thank you for applying for financing" : "Thank you for applying for financing",
                text = "Thank you for applying for financing",
                to = new List<MandrillTo>() {
                        new MandrillTo(){
                            email = contractData.DealerEmail,
                            name = " ",
                            type = "to"
                        }
                    }
            };
            try
            {
                var result = await SendEmail(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
