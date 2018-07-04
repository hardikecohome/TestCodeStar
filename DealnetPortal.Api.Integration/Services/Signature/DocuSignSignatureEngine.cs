using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Models;
using DealnetPortal.Api.Models.Signature;
using DealnetPortal.Api.Models.Storage;
using DealnetPortal.Domain;
using DealnetPortal.Utilities;
using DealnetPortal.Utilities.Configuration;
using DealnetPortal.Utilities.Logging;
using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using System.Xml.Linq;
using Unity.Interception.Utilities;

namespace DealnetPortal.Api.Integration.Services.Signature
{
    public class DocuSignSignatureEngine : ISignatureEngine
    {
        private readonly string _baseUrl;
        private readonly string _dsUser;
        private readonly string _dsPassword;
        private readonly string _dsIntegratorKey;
        private readonly string _dsDefaultBrandId;
        private readonly string _dsQuebecDefaultBrandId;
        private readonly string _notificationsEndpoint;
        private Document _document { get; set; }
        private Contract _contract;
        private string _templateId { get; set; }
        private bool _templateUsed { get; set; }
        private List<Text> _textTabs { get; set; }
        private List<Checkbox> _checkboxTabs { get; set; }
        private List<SignHere> _signHereTabs { get; set; }
        private List<Signer> _signers { get; set; }
        private List<CarbonCopy> _copyViewers { get; set; }
        private EnvelopeDefinition _envelopeDefinition { get; set; }
        private readonly string[] DocuSignResipientStatuses = { "Created", "Sent", "Delivered", "Signed", "Declined", "Completed", "Voided" };

        private readonly ILoggingService _loggingService;

        private string _accountId;

        public string AccountId
        {
            get
            {
                if (string.IsNullOrEmpty(_accountId))
                {
                    Login();
                }
                return _accountId;
            }
            private set { _accountId = value; }
        }

        public string TransactionId { get; set; }

        public string DocumentId { get; set; }        

        public DocuSignSignatureEngine(ILoggingService loggingService, IAppConfiguration configuration)
        {
            _loggingService = loggingService;
            _baseUrl = configuration.GetSetting(WebConfigKeys.DOCUSIGN_APIURL_CONFIG_KEY);
            _dsUser = configuration.GetSetting(WebConfigKeys.DOCUSIGN_USER_CONFIG_KEY);
            _dsPassword = configuration.GetSetting(WebConfigKeys.DOCUSIGN_PASSWORD_CONFIG_KEY);
            _dsIntegratorKey = configuration.GetSetting(WebConfigKeys.DOCUSIGN_INTEGRATORKEY_CONFIG_KEY);
            _dsDefaultBrandId = configuration.GetSetting(WebConfigKeys.DOCUSIGN_BRAND_ID);
            _notificationsEndpoint = configuration.GetSetting(WebConfigKeys.DOCUSIGN_NOTIFICATIONS_URL);
            _dsQuebecDefaultBrandId = configuration.GetSetting(WebConfigKeys.QUEBEC_DOCUDIGN_BRAND_ID);
            _signers = new List<Signer>();
            _copyViewers = new List<CarbonCopy>();

            Login();
        }

        public async Task<IList<Alert>> ServiceLogin()
        {
            List<Alert> alerts = new List<Alert>();

            var loginAlerts = await Task.Run(() => Login()).ConfigureAwait(false);

            if (loginAlerts.Any())
            {
                alerts.AddRange(loginAlerts);
            }

            return alerts;
        }

        public async Task<IList<Alert>> InitiateTransaction(Contract contract, AgreementTemplate agreementTemplate)
        {
            var alerts = new List<Alert>();

            await Task.Run(() =>
            {
                _contract = contract;
                if (contract != null & agreementTemplate?.TemplateDocument != null)
                {
                    if (!string.IsNullOrEmpty(agreementTemplate.TemplateDocument.ExternalTemplateId))
                    {
                        _templateId = agreementTemplate.TemplateDocument.ExternalTemplateId;
                        _templateUsed = true;
                    }
                    else
                    {
                        _templateUsed = false;
                        _document = new Document
                        {
                            DocumentBase64 = System.Convert.ToBase64String(agreementTemplate.TemplateDocument.TemplateBinary),
                            Name = agreementTemplate.TemplateDocument.TemplateName,
                            DocumentId = contract.Id.ToString(),
                            TransformPdfFields = "true"
                        };
                    }
                    _signers.Clear();
                    _copyViewers.Clear();
                    _envelopeDefinition = new EnvelopeDefinition();
                }
            });                      

            return alerts;
        }        

        public async Task<IList<Alert>> InsertDocumentFields(IList<FormField> formFields)
        {
            var alerts = new List<Alert>();
            _textTabs = new List<Text>();
            _checkboxTabs = new List<Checkbox>();

            await Task.Run(() =>
            {
                if (formFields?.Any() ?? false)
                {
                    formFields.ForEach(ff =>
                    {
                        if (ff.FieldType == FieldType.CheckBox)
                        {
                            _checkboxTabs.Add(new Checkbox()
                            {
                                TabLabel = "\\*" + ff.Name,
                                Selected = ff.Value
                            });
                        }
                        else //Text
                        {
                            _textTabs.Add(new Text()
                            {
                                TabLabel = "\\*" + ff.Name,
                                Value = ff.Value
                            });
                        }
                    });
                }
            });

            return alerts;
        }

        public async Task<IList<Alert>> InsertSignatures(IList<SignatureUser> signatureUsers)
        {
            var alerts = new List<Alert>();
            _signHereTabs = new List<SignHere>();

            await Task.Run(() =>
            {
                if (signatureUsers?.Any() ?? false)
                {
                    int signN = 1;
                    _signers?.Clear();
                    signatureUsers.Where(s => s.Role == SignatureRole.Signer).ForEach(s =>
                    {
                        _signers.Add(CreateSigner(s, signN++));
                    });
                    signatureUsers.Where(s => s.Role == SignatureRole.AdditionalApplicant).ForEach(s =>
                    {
                        _signers.Add(CreateSigner(s, signN++));
                    });
                    signatureUsers.Where(s => s.Role == SignatureRole.Dealer).ForEach(s =>
                    {
                        _signers.Add(CreateSigner(s, signN++, true));
                    });
                    _copyViewers?.Clear();
                    signatureUsers.Where(s => s.Role == SignatureRole.CopyViewer).ForEach(s =>
                    {
                        _copyViewers.Add(new CarbonCopy()
                        {
                            Email = s.EmailAddress,
                            Name = $"{s.FirstName} {s.LastName}",
                            RoutingOrder = signN.ToString(),
                            RecipientId = signN.ToString(),  
                            RoleName = "Viewer"
                        });
                        signN++;
                    });
                }

            });

            return alerts;
        }

        public async Task<IList<Alert>> UpdateSigners(IList<SignatureUser> signatureUsers)
        {
            string[] updateStatuses = new string[] { "Created", "Sent", "Delivered", "Correct"};

            var alerts = new List<Alert>();            
            if (!string.IsNullOrEmpty(TransactionId))
            {
                try
                {                
                    EnvelopesApi envelopesApi = new EnvelopesApi();
                    await InsertSignatures(signatureUsers);
                    var envelope = await envelopesApi.GetEnvelopeAsync(AccountId, TransactionId);
                    var reciepents = await envelopesApi.ListRecipientsAsync(AccountId, TransactionId);
                    if (envelope != null)
                    {
                        if (updateStatuses.Any(s =>
                            envelope.Status?.Equals(s, StringComparison.OrdinalIgnoreCase) == true))
                        {
                            var updateRecipients = !AreRecipientsEqual(reciepents);
                            if (updateRecipients)
                            {
                                reciepents = UpdateRecipientsMails(reciepents);
                            }
                            var uAlerts = await UpdateOrResendRecipients(updateRecipients ? reciepents : null, envelope);
                            if (uAlerts?.Any() == true)
                            {
                                alerts.AddRange(uAlerts);
                            }
                        }
                        else
                        {
                            var errorMsg =
                                $"Can't update eSignature reciepents in DocuSign for envelope {TransactionId} as current Envelope status is {envelope.Status}";
                            _loggingService.LogError(errorMsg);
                            alerts.Add(new Alert()
                            {
                                Type = AlertType.Error,
                                Header = "Can't update envelope",
                                Message = errorMsg
                            });
                        }
                    }
                    else
                    {
                        var errorMsg =
                            $"Can't find envelope {TransactionId} in DocuSign";
                        _loggingService.LogError(errorMsg);
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Error,
                            Header = "Can't find envelope",
                            Message = errorMsg
                        });
                    }
                }
                catch (Exception e)
                {
                    var errorMsg =
                        $"Can't update eSignature reciepents in DocuSign for envelope {TransactionId}";
                    _loggingService.LogError(errorMsg, e);
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Header = "eSignature error",
                        Message = $"{errorMsg}: {e}"
                    });
                }
            }

            return alerts;
        }

        public async Task<Tuple<bool, IList<Alert>>> ParseStatusEvent(string eventNotification, Contract contract)
        {
            var updated = false;
            var alerts = new List<Alert>();

            try
            {
                XDocument xDocument = XDocument.Parse(eventNotification);
                var xmlns = xDocument?.Root?.Attribute(XName.Get("xmlns"))?.Value ?? "http://www.docusign.net/API/3.0";

                var envelopeStatusSection = xDocument?.Root?.Element(XName.Get("EnvelopeStatus", xmlns));
                var envelopeId = envelopeStatusSection?.Element(XName.Get("EnvelopeID", xmlns))?.Value;

                if (contract != null && envelopeStatusSection != null)
                {
                    var envelopeStatus = envelopeStatusSection?.Element(XName.Get("Status", xmlns))?.Value;
                    var timeZone = xDocument.Root.Element(XName.Get("TimeZone", xmlns))?.Value;
                    TimeZoneInfo tzInfo = null;
                    if (!string.IsNullOrEmpty(timeZone))
                    {
                        tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                        _loggingService.LogInfo($"DocuSign envelope {envelopeId} timezone: {timeZone}");
                    }
                    if (!string.IsNullOrEmpty(envelopeStatus))
                    {
                        var envelopeStatusTimeValue = envelopeStatusSection.Element(XName.Get(envelopeStatus, xmlns))?.Value;
                        DateTime envelopeStatusTime;
                        if (!DateTime.TryParse(envelopeStatusTimeValue, out envelopeStatusTime))
                        {
                            envelopeStatusTime = DateTime.UtcNow;
                        }
                        else
                        {
                            if (tzInfo != null)
                            {
                                envelopeStatusTime = TimeZoneInfo.ConvertTimeToUtc(envelopeStatusTime, tzInfo);
                            }
                        }
                        _loggingService.LogInfo($"Recieved DocuSign {envelopeStatus} status for envelope {envelopeId}, time {envelopeStatusTime}");
                        updated |= ProcessSignatureStatus(contract, envelopeStatus, envelopeStatusTime);
                    }
                    
                    //proceed with recipients statuses
                    var recipientStatusesSection = envelopeStatusSection?.Element(XName.Get("RecipientStatuses", xmlns));
                    if (recipientStatusesSection != null)
                    {
                        var recipientStatuses = recipientStatusesSection.Descendants(XName.Get("RecipientStatus", xmlns));
                        recipientStatuses.ForEach(rs =>
                        {
                            var rsStatus = rs.Element(XName.Get("Status", xmlns))?.Value;
                            var rsLastStatusTime = rs.Elements().Where(rse =>
                                    DocuSignResipientStatuses.Any(ds => rse.Name.LocalName.Contains(ds)))
                                .Select(rse =>
                                {
                                    DateTime statusTime;
                                    if (!DateTime.TryParse(rse.Value, out statusTime))
                                    {
                                        statusTime = new DateTime();
                                    }
                                    return statusTime;
                                }).OrderByDescending(rst => rst).FirstOrDefault();
                            if (rsLastStatusTime == new DateTime())
                            {
                                rsLastStatusTime = DateTime.UtcNow;
                            }
                            else
                            {
                                if (tzInfo != null)
                                {
                                    rsLastStatusTime = TimeZoneInfo.ConvertTimeToUtc(rsLastStatusTime, tzInfo);
                                }
                            }
                            var rsName = rs.Element(XName.Get("UserName", xmlns))?.Value;
                            var rsEmail = rs.Element(XName.Get("Email", xmlns))?.Value;
                            var rsComment = rs.Element(XName.Get("DeclineReason", xmlns))?.Value;
                            _loggingService.LogInfo($"Recieved DocuSign recipient {rsName} status {rsStatus} status for envelope {envelopeId}, time {rsLastStatusTime}");
                            updated |= ProcessSignerStatus(contract, rsName, rsEmail, rsStatus, rsComment, rsLastStatusTime);
                        });
                    }                    
                }
                else
                {
                    alerts.Add(new Alert()
                    {
                        Type = AlertType.Error,
                        Code = ErrorCodes.CantGetContractFromDb,
                        Header = "Cannot find contract",
                        Message = $"Cannot find contract for signature envelopeId {envelopeId}"
                    });
                }
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Cannot parse DocuSign notification",
                    Message = $"Error occurred during parsing request from DocuSign: {ex.ToString()}"
                });                
            }           

            return await Task.FromResult(new Tuple<bool, IList<Alert>>(updated, alerts));
        }

        public async Task<Tuple<bool, IList<Alert>>> UpdateContractStatus(Contract contract)
        {
            var updated = false;
            var alerts = new List<Alert>();

            try
            {
                if (!string.IsNullOrEmpty(contract?.Details?.SignatureTransactionId))
                {
                    EnvelopesApi envelopesApi = new EnvelopesApi();
                    var envelope =
                        envelopesApi.GetEnvelope(AccountId, contract.Details.SignatureTransactionId);
                    var reciepents =
                        envelopesApi.ListRecipients(AccountId, contract.Details.SignatureTransactionId);

                    if (envelope != null)
                    {
                        DateTime envelopeStatusTime;
                        if (!DateTime.TryParse(envelope.StatusChangedDateTime, out envelopeStatusTime))
                        {
                            envelopeStatusTime = DateTime.UtcNow;
                        }
                        else
                        {
                            envelopeStatusTime = envelopeStatusTime.ToUniversalTime();
                        }
                        updated |= ProcessSignatureStatus(contract, envelope.Status, envelopeStatusTime);
                    }
                    if (reciepents != null)
                    {
                        reciepents.Signers?.ForEach(s =>
                        {
                            var updateTimes = new[]
                                {s.DeclinedDateTime, s.DeliveredDateTime, s.SentDateTime, s.SignedDateTime};

                            var rsLastStatusTime = updateTimes.Any(t => !string.IsNullOrEmpty(t))
                                ? updateTimes.Where(ut => !string.IsNullOrEmpty(ut))
                                    .Select(ut =>
                                    {
                                        DateTime statusTime;
                                        if (!DateTime.TryParse(ut, out statusTime))
                                        {
                                            statusTime = DateTime.Now;
                                        }
                                        statusTime = statusTime.ToUniversalTime();
                                        return statusTime;
                                    }).OrderByDescending(rst => rst).FirstOrDefault()
                                : DateTime.UtcNow;                                                                               

                            updated |= ProcessSignerStatus(contract, s.Name, s.Email, s.Status, s.DeclinedReason,
                                rsLastStatusTime);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "eSignature error",
                    Message = ex.ToString()
                });
            }

            return await Task.FromResult(new Tuple<bool, IList<Alert>>(updated, alerts));
        }

        public async Task<IList<Alert>> SubmitDocument(IList<SignatureUser> signatureUsers)
        {
            var alerts = new List<Alert>();

            EnvelopesApi envelopesApi = new EnvelopesApi();
            bool recreateEnvelope = string.IsNullOrEmpty(TransactionId);
            bool recreateRecipients = false;


            if (!string.IsNullOrEmpty(TransactionId))
            {
                var envelope = await envelopesApi.GetEnvelopeAsync(AccountId, TransactionId);
                var reciepents = await envelopesApi.ListRecipientsAsync(AccountId, TransactionId);
                if (envelope != null)
                {
                    DateTime sentTime;
                    bool contractUpdated = false;
                    if (DateTime.TryParse(envelope.SentDateTime, out sentTime) && _contract != null)
                    {
                        contractUpdated = _contract.LastUpdateTime.HasValue &&
                                          _contract.LastUpdateTime.Value > sentTime;
                    }

                    recreateRecipients = !AreRecipientsEqual(reciepents);

                    if (envelope.Status == "voided" || envelope.Status == "deleted" || envelope.Status == "declined" ||
                        envelope.Status == "completed" ||
                        contractUpdated || (envelope.Status == "sent" && recreateRecipients))
                    {
                        recreateEnvelope = true;
                    }
                }
                else
                {
                    recreateEnvelope = true;
                }

                if (!recreateEnvelope)
                {
                    try
                    {
                        var uAlerts = await UpdateOrResendRecipients(recreateRecipients ? reciepents : null, envelope);
                        if (uAlerts?.Any() == true)
                        {
                            alerts.AddRange(uAlerts);
                        }
                    }
                    catch (Exception ex)
                    {
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Error,
                            Header = "eSignature error",
                            Message = ex.ToString()
                        });
                    }
                }
            }

            if (recreateEnvelope)
            {
                if (_signers?.Any() != true && _copyViewers?.Any() != true)
                {
                    var tempSignatures = new List<SignatureUser>
                    {
                        new SignatureUser()
                        {
                            Role = SignatureRole.Signer
                        }
                    };
                    InsertSignatures(tempSignatures).GetAwaiter().GetResult();
                    _envelopeDefinition = PrepareEnvelope("created");
                }
                else
                {
                    _envelopeDefinition = PrepareEnvelope();
                }

                var envAlerts = SubmitEnvelope(_envelopeDefinition);
                if (envAlerts.Any())
                {
                    alerts.AddRange(envAlerts);
                }
            }
            
            return alerts;
        }

        public async Task<Tuple<IList<FormField>, IList<Alert>>> GetFormfFields()
        {
            var alerts = new List<Alert>();
            var formFields = new List<FormField>();

            try
            {
                TemplatesApi templatesApi = new TemplatesApi();
                var template = await templatesApi.GetAsync(AccountId, _templateId);
                var tabs = template?.Recipients?.Signers?.First()?.Tabs;
                var textTabs = tabs?.TextTabs?.Select(t => new FormField()
                {
                    Name = t.TabLabel,
                    FieldType = FieldType.Text
                }).ToList();
                if (textTabs?.Any() == true)
                {
                    formFields.AddRange(textTabs);
                }
                var chbTabs = tabs?.CheckboxTabs?.Select(t => new FormField()
                {
                    Name = t.TabLabel,
                    FieldType = FieldType.CheckBox
                }).ToList();
                if (chbTabs?.Any() == true)
                {
                    formFields.AddRange(chbTabs);
                }
            }                       
            catch (Exception e)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Warning,
                    Header = "Cannot get DocuSign template fields",
                    Message = e.ToString()
                });
            }

            return new Tuple<IList<FormField>, IList<Alert>>(formFields, alerts);
        }

        public async Task<Tuple<AgreementDocument, IList<Alert>>> GetDocument()
        {
            var alerts = new List<Alert>();
            AgreementDocument document = null;

            var tskAlerts = await Task.Run(async () =>
            {
                var sendAlerts = new List<Alert>();

                if (!string.IsNullOrEmpty(TransactionId))
                {
                    EnvelopesApi envelopesApi = new EnvelopesApi();
                    EnvelopeDocumentsResult docsList = envelopesApi.ListDocuments(AccountId, TransactionId);
                    if (docsList.EnvelopeDocuments.Any())
                    {
                        var doc = docsList.EnvelopeDocuments.First();
                        document = new AgreementDocument()
                        {
                            DocumentId = doc.DocumentId,
                            Type = doc.Type,
                            Name = doc.Name
                        };                        
                        var docStream = envelopesApi.GetDocument(AccountId, TransactionId, "combined");
                        document.DocumentRaw = new byte[docStream.Length];
                        await docStream.ReadAsync(document.DocumentRaw, 0, (int)docStream.Length);
                    }                    
                }                               
                return sendAlerts;
            });

            if (tskAlerts.Any())
            {
                alerts.AddRange(tskAlerts);
            }            

            return new Tuple<AgreementDocument, IList<Alert>>(document, alerts);
        }

        public async Task<IList<Alert>> CancelSignature(string cancelReason = null)
        {
            var alerts = new List<Alert>();
            try
            {
                EnvelopesApi envelopesApi = new EnvelopesApi();
                var envelope = await envelopesApi.GetEnvelopeAsync(AccountId, TransactionId);
                if (envelope.Status != "completed" && envelope.Status != "declined")
                {
                    envelope = new Envelope
                    {
                        Status = "voided",
                        VoidedReason = cancelReason ?? Resources.Resources.DealerCancelledEsign
                    };
                    var updateEnvelopeRes = await
                        envelopesApi.UpdateAsync(AccountId, TransactionId, envelope);
                    if (updateEnvelopeRes?.ErrorDetails?.ErrorCode != null)
                    {
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Error,
                            Header = "Cannot cancel signature",
                            Message = updateEnvelopeRes?.ErrorDetails?.Message
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "Cannot cancel contract",
                    Message = $"Cannot cancel eSignature {TransactionId} for contract: {ex.ToString()}",
                });
            }
            return alerts;
        }

        #region private

        private bool AreRecipientsEqual(Recipients recipients)
        {            
            bool areEqual = false;

            if (recipients != null && _signers != null)
            {
                areEqual = _signers.All(s => recipients.Signers.Any(r => r.Email == s.Email && r.Name == s.Name))
                    && (_copyViewers?.All(s => recipients.CarbonCopies?.Any(r => r.Email == s.Email && r.Name == s.Name) ?? true) ?? true);
            }

            return areEqual;
        }

        private Recipients UpdateRecipientsMails(Recipients recipients)
        {
            recipients.Signers?.ForEach(s =>
            {
                var signer = _signers.FirstOrDefault(us => us.Name == s.Name);
                if (!string.IsNullOrEmpty(signer?.Email))
                {
                    s.Email = signer.Email;
                }
            });            
            return recipients;
        }

        private async Task<IList<Alert>> UpdateOrResendRecipients(Recipients updateRecipients, Envelope envelope)
        {
            var alerts = new List<Alert>();
            EnvelopesApi envelopesApi = new EnvelopesApi();
            if (updateRecipients != null)
            {
                var updateRes =
                    await envelopesApi.UpdateRecipientsAsync(AccountId, TransactionId, updateRecipients);
                if (updateRes?.RecipientUpdateResults?.Any() == true)
                {
                    updateRes.RecipientUpdateResults.Where(rr => !string.IsNullOrEmpty(rr.ErrorDetails?.Message)).ForEach(rr =>
                    {
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Error,
                            Header = "DocuSign error",
                            Message = rr.ErrorDetails?.Message
                        });
                        _loggingService.LogError($"DocuSign error: {rr.ErrorDetails?.Message}");
                    });                    
                }
            }
            else
            {
                if (envelope.Status == "sent")
                {
                    envelope = new Envelope() { };
                    var updateOptions = new EnvelopesApi.UpdateOptions()
                    {
                        resendEnvelope = "true"
                    };
                    var updateEnvelopeRes =
                        await envelopesApi.UpdateAsync(AccountId, TransactionId, envelope,
                            updateOptions);
                    if (updateEnvelopeRes?.ErrorDetails?.Message != null)
                    {
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Error,
                            Header = "DocuSign error",
                            Message = updateEnvelopeRes.ErrorDetails.Message
                        });
                        _loggingService.LogError($"DocuSign error: {updateEnvelopeRes.ErrorDetails.Message}");
                    }
                }
                if (envelope.Status == "created")
                {
                    envelope = new Envelope()
                    {
                        Status = "sent"
                    };
                    var updateEnvelopeRes =
                       await envelopesApi.UpdateAsync(AccountId, TransactionId, envelope);
                    if (updateEnvelopeRes?.ErrorDetails?.Message != null)
                    {
                        alerts.Add(new Alert()
                        {
                            Type = AlertType.Error,
                            Header = "DocuSign error",
                            Message = updateEnvelopeRes.ErrorDetails.Message
                        });
                        _loggingService.LogError($"DocuSign error: {updateEnvelopeRes.ErrorDetails.Message}");
                    }
                }
            }
            return alerts;
        }

        private List<Alert> SubmitEnvelope(EnvelopeDefinition envelopeDefinition)
        {
            List<Alert> alerts = new List<Alert>();

            EnvelopesApi envelopesApi = new EnvelopesApi();
            EnvelopeSummary envelopeSummary = envelopesApi.CreateEnvelope(AccountId, envelopeDefinition);

            if (!string.IsNullOrEmpty(envelopeSummary?.EnvelopeId))
            {
                EnvelopeDocumentsResult docsList = envelopesApi.ListDocuments(AccountId, envelopeSummary.EnvelopeId);

                TransactionId = envelopeSummary.EnvelopeId;
                _loggingService.LogInfo($"DocuSign document was successfully created: envelope {TransactionId}, document {docsList?.EnvelopeDocuments?.FirstOrDefault()?.DocumentId}");
                DocumentId = docsList?.EnvelopeDocuments?.FirstOrDefault()?.DocumentId;
            }
            else
            {
                _loggingService.LogError("DocuSign document wasn't created");
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "DocuSign error",
                    Message = "DocuSign document wasn't created"
                });
            }

            return alerts;
        }

        private EnvelopeDefinition PrepareEnvelope(string statusOnCreation = "sent")
        {
            EnvelopeDefinition envelopeDefinition = new EnvelopeDefinition
            {
                EmailSubject = Resources.Resources.PleaseSignAgreement,
                BrandId = _contract.PrimaryCustomer.Locations?.FirstOrDefault(m=>m.AddressType == AddressType.MainAddress).State == "QC" ? _dsQuebecDefaultBrandId : _dsDefaultBrandId
            };

            if (_templateUsed)
            {
                FillEnvelopeForTemplate(envelopeDefinition);
            }
            else
            {
                envelopeDefinition.CompositeTemplates = new List<CompositeTemplate>()
                {
                    new CompositeTemplate
                    {
                        InlineTemplates = new List<InlineTemplate>()
                        {
                            new InlineTemplate()
                            {
                                Sequence = "1",                                
                                Recipients = new Recipients()
                                {
                                    Signers = _signers,
                                    CarbonCopies = _copyViewers
                                }
                            }
                        },
                        Document = _document
                    },
                };
            }
            envelopeDefinition.Status = statusOnCreation;
            envelopeDefinition.EventNotification = GetEventNotification();
            envelopeDefinition.CustomFields = GenerateEnvolveCustomFields();
            return envelopeDefinition;
        }

        private EventNotification GetEventNotification()
        {            
            if (!string.IsNullOrEmpty(_notificationsEndpoint))
            {
                string url = _notificationsEndpoint;
                _loggingService.LogInfo($"DocuSign notifications will send to {url}");

                List<EnvelopeEvent> envelope_events = new List<EnvelopeEvent>();

                EnvelopeEvent envelope_event1 = new EnvelopeEvent();
                envelope_event1.EnvelopeEventStatusCode = "sent";
                envelope_events.Add(envelope_event1);
                EnvelopeEvent envelope_event2 = new EnvelopeEvent();
                envelope_event2.EnvelopeEventStatusCode = "delivered";
                envelope_events.Add(envelope_event2);
                EnvelopeEvent envelope_event3 = new EnvelopeEvent();
                envelope_event3.EnvelopeEventStatusCode = "completed";
                envelope_events.Add(envelope_event3);
                EnvelopeEvent envelope_event4 = new EnvelopeEvent();
                envelope_event4.EnvelopeEventStatusCode = "declined";
                envelope_events.Add(envelope_event4);
                EnvelopeEvent envelope_event5 = new EnvelopeEvent();
                envelope_event5.EnvelopeEventStatusCode = "voided";
                envelope_events.Add(envelope_event5);

                List<RecipientEvent> recipient_events = new List<RecipientEvent>();
                RecipientEvent recipient_event1 = new RecipientEvent();
                recipient_event1.RecipientEventStatusCode = "Sent";
                recipient_events.Add(recipient_event1);
                RecipientEvent recipient_event2 = new RecipientEvent();
                recipient_event2.RecipientEventStatusCode = "Delivered";
                recipient_events.Add(recipient_event2);
                RecipientEvent recipient_event3 = new RecipientEvent();
                recipient_event3.RecipientEventStatusCode = "Completed";
                recipient_events.Add(recipient_event3);
                RecipientEvent recipient_event4 = new RecipientEvent();
                recipient_event4.RecipientEventStatusCode = "Declined";
                recipient_events.Add(recipient_event4);
                RecipientEvent recipient_event5 = new RecipientEvent();
                recipient_event5.RecipientEventStatusCode = "AuthenticationFailed";
                recipient_events.Add(recipient_event5);
                RecipientEvent recipient_event6 = new RecipientEvent();
                recipient_event6.RecipientEventStatusCode = "AutoResponded";
                recipient_events.Add(recipient_event6);

                EventNotification event_notification = new EventNotification();
                event_notification.Url = url;
                event_notification.LoggingEnabled = "true";
                event_notification.RequireAcknowledgment = "true";
                event_notification.UseSoapInterface = "false";
                event_notification.IncludeCertificateWithSoap = "false";
                event_notification.SignMessageWithX509Cert = "false";
                //event_notification.IncludeDocuments = "true";
                event_notification.IncludeEnvelopeVoidReason = "true";
                event_notification.IncludeTimeZone = "true";
                event_notification.IncludeSenderAccountAsCustomField = "true";
                //event_notification.IncludeDocumentFields = "true";
                //event_notification.IncludeCertificateOfCompletion = "true";
                event_notification.EnvelopeEvents = envelope_events;
                event_notification.RecipientEvents = recipient_events;

                return event_notification;
            }
            else
            {
                return null;
            }
        }

        private void FillEnvelopeForTemplate(EnvelopeDefinition envelopeDefinition)
        {
            List<TemplateRole> rolesList = new List<TemplateRole>();

            _signers.ForEach(signer =>
            {
                if (signer.RoleName == "Signer1")
                {
                    rolesList.Add(new TemplateRole()
                    {
                        Name = signer.Name,
                        Email = signer.Email,
                        //RoutingOrder = signer.RoutingOrder, //?
                        RoleName = signer.RoleName ?? $"Signer{signer.RoutingOrder}",
                        Tabs = signer.Tabs
                    });
                }
                else
                {
                    rolesList.Add(new TemplateRole()
                    {
                        Name = signer.Name,
                        Email = signer.Email,
                        RoleName = signer.RoleName ?? $"Signer{signer.RoutingOrder}"
                    });
                }
            });            

            _copyViewers.ForEach(viewer =>
            {
                rolesList.Add(new TemplateRole()
                {
                    Email = viewer.Email,
                    Name = viewer.Name,
                    RoutingOrder = viewer.RoutingOrder,
                    RoleName = viewer.RoleName//"Viewer",                    
                });
            });

            envelopeDefinition.TemplateId = _templateId;
            envelopeDefinition.TemplateRoles = rolesList;
        }

        private string CreateAuthHeader(string userName, string password, string integratorKey)
        {
            DocuSignCredentials dsCreds = new DocuSignCredentials()
            {
                Username = userName,
                Password = password,
                IntegratorKey = integratorKey
            };

            string authHeader = Newtonsoft.Json.JsonConvert.SerializeObject(dsCreds);
            return authHeader;
        }

        private CustomFields GenerateEnvolveCustomFields()
        {
            var customFields = new CustomFields
            {
                TextCustomFields = new List<TextCustomField>()                
            };
            if (!string.IsNullOrEmpty(_contract?.Details?.TransactionId))
            {
                customFields.TextCustomFields.Add(
                    new TextCustomField
                    {
                        Name = PdfFormFields.ApplicationID,
                        Required = "true",
                        Show = "true",
                        Value = _contract.Details.TransactionId
                    });
            }
            if (!string.IsNullOrEmpty(_contract?.Dealer?.UserName))
            {
                customFields.TextCustomFields.Add(
                    new TextCustomField
                    {
                        Name = PdfFormFields.DealerID,
                        Required = "true",
                        Show = "true",
                        Value = _contract.Dealer.UserName
                    });
            }
            return customFields;
        }

        private Signer CreateSigner(SignatureUser signatureUser, int routingOrder, bool isDealer = false)
        {
            var signer = new Signer()
            {
                Email = signatureUser.EmailAddress,
                Name = $"{signatureUser.FirstName} {signatureUser.LastName}".Trim(),
                RecipientId = routingOrder.ToString(),
                RoutingOrder = routingOrder.ToString(), //not sure, probably 1
                RoleName = !isDealer ? $"Signer{routingOrder}" : "SignerD",
                
                Tabs = new Tabs()
                {
                    SignHereTabs = new List<SignHere>()
                    {
                        new SignHere()
                        {
                            TabLabel = !isDealer ? $"Signature{routingOrder}" : "SignatureD"
                        },
                        //?? for 2nd signature
                        new SignHere()
                        {
                            TabLabel = !isDealer ? $"Signature{routingOrder}_2" : "SignatureD_2"
                        }
                    }
                }
            };

            {
                if (_textTabs?.Any() ?? false)
                {
                    if (signer.Tabs.TextTabs == null)
                    {
                        signer.Tabs.TextTabs = new List<Text>();
                    }
                    signer.Tabs.TextTabs.AddRange(_textTabs);
                }
                if (_checkboxTabs?.Any() ?? false)
                {
                    if (signer.Tabs.CheckboxTabs == null)
                    {
                        signer.Tabs.CheckboxTabs = new List<Checkbox>();
                    }
                    signer.Tabs.CheckboxTabs.AddRange(_checkboxTabs);
                }
            }

            return signer;
        }

        private SignatureStatus? ParseSignatureStatus(string status)
        {
            SignatureStatus? signatureStatus = null;
            switch (status?.ToLowerInvariant())
            {
                case "created":
                    signatureStatus = SignatureStatus.Created;
                    break;
                case "autoresponded":
                // [DEAL-3767] Esignature email sending is not working properly for previously submitted deals
                // It seems [autoresponded] status returned for not-existings mails. we should deal with this situation in the future
                case "sent":                
                    signatureStatus = SignatureStatus.Sent;
                    break;
                case "delivered":
                    signatureStatus = SignatureStatus.Delivered;
                    break;
                case "signed":
                    signatureStatus = SignatureStatus.Signed;
                    break;
                case "completed":
                    signatureStatus = SignatureStatus.Completed;
                    break;
                case "declined":
                    signatureStatus = SignatureStatus.Declined;
                    break;
                case "deleted":
                case "voided":
                    signatureStatus = SignatureStatus.Deleted;
                    break;
            }
            return signatureStatus;
        }

        private bool ProcessSignatureStatus(Contract contract, string signatureStatus, DateTime updateTime)
        {
            bool updated = false;
            var status = ParseSignatureStatus(signatureStatus);            

            if (status.HasValue && contract.Details.SignatureStatus != status)
            {
                contract.Details.SignatureStatus = status;
                updated = true;
            }
            if (contract.Details.SignatureStatusQualifier != signatureStatus)
            {
                contract.Details.SignatureStatusQualifier = signatureStatus;
                updated = true;
            }
            if (updated)
            {
                contract.Details.SignatureLastUpdateTime = updateTime;
            }

            return updated;
        }

        private bool ProcessSignerStatus(Contract contract, string userName, string email, string status, string comment, DateTime statusTime)
        {
            bool updated = false;

            var signer =
                contract.Signers?.FirstOrDefault(s => (string.IsNullOrEmpty(s.FirstName) || userName.Contains(s.FirstName))
                && (string.IsNullOrEmpty(s.LastName) || userName.Contains(s.LastName)));            
            if (signer != null)
            {
                var sStatus = ParseSignatureStatus(status);
                if (sStatus != null && signer.SignatureStatus != sStatus)
                {
                    signer.SignatureStatus = sStatus;
                    signer.SignatureStatusQualifier = status;
                    signer.StatusLastUpdateTime = statusTime;
                    if (!string.IsNullOrEmpty(comment))
                    {
                        comment = System.Web.HttpUtility.HtmlDecode(comment);
                    }
                    signer.Comment = comment;

                    if (string.IsNullOrEmpty(signer.EmailAddress))
                    {
                        signer.EmailAddress = email;
                    }
                    updated = true;
                }
            }

            return updated;
        }

        private IList<Alert> Login()
        {
            var logAlerts = new List<Alert>();
            ApiClient apiClient = new ApiClient(_baseUrl);
            string authHeader = CreateAuthHeader(_dsUser, _dsPassword, _dsIntegratorKey);
            Configuration.Default.ApiClient = apiClient;

            if (Configuration.Default.DefaultHeader.ContainsKey("X-DocuSign-Authentication"))
            {
                Configuration.Default.DefaultHeader.Remove("X-DocuSign-Authentication");
            }
            Configuration.Default.AddDefaultHeader("X-DocuSign-Authentication", authHeader);

            AuthenticationApi authApi = new AuthenticationApi();

            AuthenticationApi.LoginOptions options = new AuthenticationApi.LoginOptions();
            options.apiPassword = "true";
            options.includeAccountIdGuid = "true";
            LoginInformation loginInfo = authApi.Login(options);

            // find the default account for this user
            foreach (LoginAccount loginAcct in loginInfo.LoginAccounts)
            {
                if (loginAcct.IsDefault == "true")
                {
                    AccountId = loginAcct.AccountId;
                    break;
                }
            }

            if (string.IsNullOrEmpty(AccountId))
            {
                _loggingService.LogError("Can't login to DocuSign service");
                logAlerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "DocuSign error",
                    Message = "Can't login to DocuSign service"
                });
            }

            return logAlerts;
        }

        #endregion
    }

    public class DocuSignCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string IntegratorKey { get; set; }
    }
}

