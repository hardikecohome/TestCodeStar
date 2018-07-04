using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DealnetPortal.Api.Common.Constants;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Common.Helpers;
using DealnetPortal.Api.Core.ApiClient;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Helpers;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Integration.ServiceAgents.ESignature.EOriginalTypes;
using DealnetPortal.Api.Integration.ServiceAgents.ESignature.EOriginalTypes.SsWeb;
using DealnetPortal.Api.Integration.ServiceAgents.ESignature.EOriginalTypes.Transformation;
using DealnetPortal.Api.Integration.Utility;
using DealnetPortal.Api.Models;
using DealnetPortal.Utilities;
using DealnetPortal.Utilities.Logging;
using Unity.Interception.Utilities;
using DocumentType = DealnetPortal.Api.Integration.ServiceAgents.ESignature.EOriginalTypes.SsWeb.DocumentType;
using textField = DealnetPortal.Api.Integration.ServiceAgents.ESignature.EOriginalTypes.Transformation.textField;

namespace DealnetPortal.Api.Integration.ServiceAgents.ESignature
{
    public class ESignatureServiceAgent : IESignatureServiceAgent
    {
        private IHttpApiClient Client { get; set; }
        private ILoggingService LoggingService { get; set; }
        private readonly string _fullUri;
        private readonly string _setupUri;
        public ESignatureServiceAgent(IHttpApiClient ecoreClient, ILoggingService loggingService)
        {
            Client = ecoreClient;
            LoggingService = loggingService;
            //AspireApiClient = aspireClient;
            _fullUri = string.Format("{0}/{1}", Client.Client.BaseAddress, "ecore");
            _setupUri = string.Format("{0}/{1}", Client.Client.BaseAddress, "ssweb/setup");
        }


        public async Task<IList<Alert>> Login(string userName, string organisation, string password)
        {
            IList<Alert> alerts = new List<Alert>();
            var data = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("loginUsername", userName),
                new KeyValuePair<string, string>("loginOrganization", organisation),
                new KeyValuePair<string, string>("loginPassword", password)
            });

            var response = await Client.Client.PostAsync(_fullUri + "/?action=eoLogin", data).ConfigureAwait(false);            
            response.EnsureSuccessStatusCode();

            if (response?.Content != null)
            {
                var eResponse = await response.Content.DeserializeFromStringAsync<response>();
                if (eResponse?.status != responseStatus.ok)
                {
                    alerts = GetAlertsFromResponse(eResponse);                    
                }
                else
                {
                    ReadCookies(response);
                }
            }
            else
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = ErrorConstants.EcoreConnectionFailed,
                    Message = "Can't connect to eCore service"
                });
            }            

            return alerts;
        }

        public async Task<bool> Logout()
        {
            var response = await Client.Client.PostAsync(_fullUri + "/?action=eoLogout", null).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

            return eResponse.status == responseStatus.ok;
        }

        public async Task<Tuple<transactionType, IList<Alert>>> CreateTransaction(string transactionName)
        {
            IList<Alert> alerts = new List<Alert>();
            var data = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("transactionName", transactionName),                
            });            

            var response = await Client.Client.PostAsync(_fullUri + "/?action=eoCreateTransaction", data).ConfigureAwait(false);

            var eResponse = await response.Content.DeserializeFromStringAsync<response>();

            if (eResponse?.status == responseStatus.ok)
            {
                if (eResponse.eventResponse.ItemsElementName.Contains(ItemsChoiceType15.transactionList))
                {
                    var transactionList = eResponse.eventResponse.Items[Array.IndexOf(eResponse.eventResponse.ItemsElementName, ItemsChoiceType15.transactionList)] as EOriginalTypes.transactionListType1;
                    return new Tuple<transactionType, IList<Alert>>(transactionList?.transaction?.FirstOrDefault(), alerts);
                }
            }
            else
            {
                alerts = GetAlertsFromResponse(eResponse);
            }

            return new Tuple<transactionType, IList<Alert>>(null, alerts);
        }

        public async Task<Tuple<documentProfileType, IList<Alert>>> CreateDocumentProfile(long transactionSid, string dptName, string dpName = null)
        {
            IList<Alert> alerts = new List<Alert>();
            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("transactionSid", transactionSid.ToString()),
                new KeyValuePair<string, string>("dptName", dptName)
            };            
            if (!string.IsNullOrEmpty(dpName))
            {
                values.Add(new KeyValuePair<string, string>("dpName", dpName));
            }
            var data = new FormUrlEncodedContent(values);

            var response = await Client.Client.PostAsync(_fullUri + "/?action=eoCreateDocumentProfile", data).ConfigureAwait(false);

            var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

            if (eResponse?.status == responseStatus.ok)
            {
                if (eResponse.eventResponse.ItemsElementName.Contains(ItemsChoiceType15.transactionList))
                {
                    var transactionList = eResponse.eventResponse.Items[Array.IndexOf(eResponse.eventResponse.ItemsElementName, ItemsChoiceType15.transactionList)] as EOriginalTypes.transactionListType1;
                    var documentProfileList = transactionList?.transaction?.FirstOrDefault()?.documentProfileList;
                    return new Tuple<documentProfileType, IList<Alert>>(documentProfileList?.FirstOrDefault(), alerts);
                }
            }
            else
            {
                alerts = GetAlertsFromResponse(eResponse);
            }

            return new Tuple<documentProfileType, IList<Alert>>(null, alerts);
        }

        public async Task<Tuple<documentVersionType, IList<Alert>>> UploadDocument(long dpSid, byte[] pdfDocumentData, string documentFileName)
        {
            IList<Alert> alerts = new List<Alert>();
            var fileContent = new ByteArrayContent(pdfDocumentData);
                fileContent.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "srcFile",
                        FileName = documentFileName
                    };

            var content = new MultipartFormDataContent();
            content.Add(new StringContent(dpSid.ToString()), "dpSid");
            content.Add(new StringContent(documentFileName), "documentFileName");
            content.Add(new StringContent("application/pdf"), "mimeType");
            content.Add(fileContent, "srcFile");

            var response = await Client.Client.PostAsync(_fullUri + "/?action=eoUploadDocument", content).ConfigureAwait(false);

            var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

            //??
            if (eResponse?.status == responseStatus.ok)
            {
                if (eResponse.eventResponse.ItemsElementName.Contains(ItemsChoiceType15.documentProfileList))
                {
                    var documentProfileList = eResponse.eventResponse.Items[Array.IndexOf(eResponse.eventResponse.ItemsElementName, ItemsChoiceType15.documentProfileList)] as EOriginalTypes.documentProfileListType;
                    var documentVersion = documentProfileList?.documentProfile?.FirstOrDefault()?.documentVersionList?.FirstOrDefault();
                    return new Tuple<documentVersionType, IList<Alert>>(documentVersion, alerts);
                }
            }
            else
            {
                alerts = GetAlertsFromResponse(eResponse);
            }

            return new Tuple<documentVersionType, IList<Alert>>(null, alerts);
        }

        public async Task<Tuple<documentVersionType, IList<Alert>>> InsertFormFields(long dpSid, textField[] textFields, TextData[] textData,
            SigBlock[] signBlocks)
        {
            try
            {            
                IList<Alert> alerts = new List<Alert>();

                var transformationInstructions = new List<TransformationInstructions>();

                var exportTypes = new List<Type>();

                if (signBlocks?.Any() ?? false)
                {
                    transformationInstructions.Add(new AddSigBlocks()
                    {
                        name = "addSigBlock",
                        sigBlockList = signBlocks
                    });
                    exportTypes.Add(typeof(AddSigBlocks));
                }

                if (textFields?.Any() ?? false)
                {
                    transformationInstructions.Add(new AddTextFields()
                    {
                        name = "addTextField",
                        textFieldList = textFields
                    });
                    exportTypes.Add(typeof(AddTextFields));
                    exportTypes.Add(typeof(textField));
                }

                if (textData?.Any() ?? false)
                {
                    transformationInstructions.Add(new AddTextData()
                    {
                        name = "addTextData",
                        textDataList = textData
                    });
                    exportTypes.Add(typeof(AddTextData));
                }

                var ts = new transformationInstructionSet()
                {                    
                    transformationInstructions = transformationInstructions.ToArray()
                };                

                var x = new XmlSerializer(ts.GetType(), exportTypes.ToArray());
                var settings = new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize};
                MemoryStream ms = new MemoryStream();
                var writer = XmlWriter.Create(ms, settings);
                x.Serialize(writer, ts);

                //var xmlWriter = XmlWriter.Create("testForm.xml", settings);
                //x.Serialize(xmlWriter, ts);
                //xmlWriter.Flush();

                ms.Position = 0;

                var fileContent = new ByteArrayContent(ms.GetBuffer());

                fileContent.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "formFieldsXML",
                        FileName = "formFieldsXML"
                    };

                var content = new MultipartFormDataContent();
                content.Add(new StringContent(dpSid.ToString()), "dpSid");          
                content.Add(fileContent, "formFieldsXML");

                var response = await Client.Client.PostAsync(_fullUri + "/?action=eoInsertFormFields", content).ConfigureAwait(false);

                var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

                if (eResponse?.status == responseStatus.ok)
                {
                    if (eResponse.eventResponse.ItemsElementName.Contains(ItemsChoiceType15.documentProfileList))
                    {
                        var documentProfileList = eResponse.eventResponse.Items[Array.IndexOf(eResponse.eventResponse.ItemsElementName, ItemsChoiceType15.documentProfileList)] as EOriginalTypes.documentProfileListType;
                        var documentVersion = documentProfileList?.documentProfile?.FirstOrDefault()?.documentVersionList?.FirstOrDefault();
                        return new Tuple<documentVersionType, IList<Alert>>(documentVersion, alerts);
                    }
                }
                else
                {
                    alerts = GetAlertsFromResponse(eResponse);
                }

                return new Tuple<documentVersionType, IList<Alert>>(null, alerts);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<Tuple<documentVersionType, IList<Alert>>> EditFormFields(long dpSid, FormField[] formFields)
        {
            try
            {
                IList<Alert> alerts = new List<Alert>();

                var transformationInstructions = new List<TransformationInstructions>();

                var exportTypes = new List<Type>();
                exportTypes.Add(typeof(FormFields));

                transformationInstructions.Add(new FormFields()
                {
                    name = "editFormField",
                    formFieldList = formFields.ToList()
                });                

                var ts = new transformationInstructionSet()
                {
                    transformationInstructions = transformationInstructions.ToArray()
                };

                var x = new XmlSerializer(ts.GetType(), exportTypes.ToArray());
                var settings = new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize };
                MemoryStream ms = new MemoryStream();
                var writer = XmlWriter.Create(ms, settings);
                x.Serialize(writer, ts);

                //var xmlWriter = XmlWriter.Create("testEdit.xml", settings);
                //x.Serialize(xmlWriter, ts);
                //xmlWriter.Flush();

                ms.Position = 0;

                var fileContent = new ByteArrayContent(ms.GetBuffer());

                fileContent.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "instructionsXML",
                        FileName = "instructionsXML"
                    };

                var content = new MultipartFormDataContent();
                content.Add(new StringContent(dpSid.ToString()), "dpSid");
                content.Add(fileContent, "instructionsXML");

                var response = await Client.Client.PostAsync(_fullUri + "/?action=eoEditFormFieldProperties", content).ConfigureAwait(false);

                var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

                if (eResponse?.status == responseStatus.ok)
                {
                    if (eResponse.eventResponse.ItemsElementName.Contains(ItemsChoiceType15.documentProfileList))
                    {
                        var documentProfileList = eResponse.eventResponse.Items[Array.IndexOf(eResponse.eventResponse.ItemsElementName, ItemsChoiceType15.documentProfileList)] as EOriginalTypes.documentProfileListType;
                        var documentVersion = documentProfileList?.documentProfile?.FirstOrDefault()?.documentVersionList?.FirstOrDefault();
                        return new Tuple<documentVersionType, IList<Alert>>(documentVersion, alerts);
                    }
                }
                else
                {
                    alerts = GetAlertsFromResponse(eResponse);
                }

                return new Tuple<documentVersionType, IList<Alert>>(null, alerts);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<Tuple<documentVersionType, IList<Alert>>> RemoveFormFields(long dpSid, FormField[] formFields)
        {
            try
            {
                IList<Alert> alerts = new List<Alert>();

                var transformationInstructions = new List<TransformationInstructions>();

                var exportTypes = new List<Type>();
                exportTypes.Add(typeof(FormFields));

                transformationInstructions.Add(new FormFields()
                {
                    name = "removeFormField",
                    formFieldList = formFields.ToList()
                });                

                var ts = new transformationInstructionSet()
                {
                    transformationInstructions = transformationInstructions.ToArray()
                };

                var x = new XmlSerializer(ts.GetType(), exportTypes.ToArray());
                var settings = new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize };
                MemoryStream ms = new MemoryStream();
                var writer = XmlWriter.Create(ms, settings);
                x.Serialize(writer, ts);

                ms.Position = 0;

                var fileContent = new ByteArrayContent(ms.GetBuffer());

                fileContent.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "blockListXMLFile",
                        FileName = "blockListXMLFile"
                    };

                var content = new MultipartFormDataContent();
                content.Add(new StringContent(dpSid.ToString()), "dpSid");
                content.Add(fileContent, "blockListXMLFile");

                var response = await Client.Client.PostAsync(_fullUri + "/?action=eoRemoveFormFields", content).ConfigureAwait(false);

                var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

                if (eResponse?.status == responseStatus.ok)
                {
                    if (eResponse.eventResponse.ItemsElementName.Contains(ItemsChoiceType15.documentProfileList))
                    {
                        var documentProfileList = eResponse.eventResponse.Items[Array.IndexOf(eResponse.eventResponse.ItemsElementName, ItemsChoiceType15.documentProfileList)] as EOriginalTypes.documentProfileListType;
                        var documentVersion = documentProfileList?.documentProfile?.FirstOrDefault()?.documentVersionList?.FirstOrDefault();
                        return new Tuple<documentVersionType, IList<Alert>>(documentVersion, alerts);
                    }
                }
                else
                {
                    alerts = GetAlertsFromResponse(eResponse);
                }

                return new Tuple<documentVersionType, IList<Alert>>(null, alerts);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<IList<Alert>> MergeData(long dpSid, TextData[] textData)
        {
            try
            {
                IList<Alert> alerts = new List<Alert>();

                var transformationInstructions = new List<TransformationInstructions>();

                var exportTypes = new List<Type>();                

                if (textData?.Any() ?? false)
                {
                    transformationInstructions.Add(new AddTextData()
                    {
                        name = "addTextData",
                        textDataList = textData
                    });
                    exportTypes.Add(typeof(AddTextData));
                }

                var ts = new transformationInstructionSet()
                {
                    transformationInstructions = transformationInstructions.ToArray()
                };

                //XmlSerializer x = new System.Xml.Serialization.XmlSerializer(ts.GetType(), exportTypes.ToArray());
                //MemoryStream ms = new MemoryStream();
                //x.Serialize(ms, ts);

                var x = new XmlSerializer(ts.GetType(), exportTypes.ToArray());
                var settings = new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize };
                MemoryStream ms = new MemoryStream();
                var writer = XmlWriter.Create(ms, settings);
                x.Serialize(writer, ts);
               
                ms.Position = 0;
                var fileContent = new ByteArrayContent(ms.GetBuffer());
                fileContent.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "formFieldDataXML",
                        FileName = "formFieldDataXML"
                    };

                var content = new MultipartFormDataContent();
                content.Add(new StringContent(dpSid.ToString()), "dpSid");
                content.Add(fileContent, "formFieldDataXML");

                var response = await Client.Client.PostAsync(_fullUri + "/?action=eoMergeData", content).ConfigureAwait(false);

                var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

                if (eResponse?.status == responseStatus.ok)
                {
                    return alerts;
                }
                else
                {
                    alerts = GetAlertsFromResponse(eResponse);
                }

                return alerts;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<IList<Alert>> ConfigureSortOrder(long transactionSid, long[] dpSids)
        {
            try
            {            
                IList<Alert> alerts = new List<Alert>();

                var sortOrder = new eoConfigureSortOrder()
                {
                    transactionSid = transactionSid.ToString(),                
                };
                if (dpSids?.Any() ?? false)
                {
                    List<ESignature.EOriginalTypes.SsWeb.DocumentType> docs = new List<DocumentType>();
                    dpSids.ForEach(dsid =>
                        docs.Add(new DocumentType()
                                {
                                    Value = dsid.ToString()
                                }));
                    sortOrder.documentList = docs.ToArray();
                }                

                XmlSerializer x = new System.Xml.Serialization.XmlSerializer(sortOrder.GetType());
                var settings = new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize };
                MemoryStream ms = new MemoryStream();
                var writer = XmlWriter.Create(ms, settings);
                //XmlWriter writer = new XmlTextWriter(ms, Encoding.UTF8);                
                x.Serialize(writer, sortOrder);
                writer.Flush();
                ms.Position = 0;

                //var fileContent = new ByteArrayContent(fileBytes);
                var fileContent = new ByteArrayContent(ms.ToArray());
                fileContent.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "instructionsXML",
                        FileName = "instructionsXML"
                    };

                var content = new MultipartFormDataContent();
                content.Add(fileContent, "instructionsXML");

                var response = await Client.Client.PostAsync(_fullUri + "/?action=eoConfigureSortOrder", content).ConfigureAwait(false);

                var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

                if (eResponse?.status != responseStatus.ok)
                {
                    alerts = GetAlertsFromResponse(eResponse);
                }

                return alerts;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<IList<Alert>> ConfigureRoles(long transactionSid, eoConfigureRolesRole[] roles)
        {
            try
            {
                IList<Alert> alerts = new List<Alert>();

                var configRoles = new eoConfigureRoles()
                {
                    transactionSid = transactionSid.ToString(),
                    rolesList = roles
                };

                XmlSerializer x = new System.Xml.Serialization.XmlSerializer(configRoles.GetType());
                var settings = new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize };
                MemoryStream ms = new MemoryStream();
                var writer = XmlWriter.Create(ms, settings);
                //XmlWriter writer = new XmlTextWriter(ms, Encoding.UTF8);
                x.Serialize(writer, configRoles);
                writer.Flush();
                ms.Position = 0;

                //var fileContent = new ByteArrayContent(fileBytes);
                var fileContent = new ByteArrayContent(ms.ToArray());
                fileContent.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "instructionsXML",
                        FileName = "instructionsXML"
                    };

                var content = new MultipartFormDataContent();
                content.Add(fileContent, "instructionsXML");

                var response = await Client.Client.PostAsync(_fullUri + "/?action=eoConfigureRoles", content).ConfigureAwait(false);

                var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

                if (eResponse?.status != responseStatus.ok)
                {
                    alerts = GetAlertsFromResponse(eResponse);
                }
                return alerts;
            }
            catch (Exception ex)
            {                
                throw;
            }
        }

        public async Task<IList<Alert>> ConfigureInvitation(long transactionSid, string roleName, string senderFirstName, string senderLastName, string senderEmail)
        {
            try
            {
                IList<Alert> alerts = new List<Alert>();

                var configInvitation = new eoConfigureInvitation()
                {                    
                    transactionSid = transactionSid.ToString(),
                    ItemsElementName = new ItemsChoiceTypeInvitation[] { ItemsChoiceTypeInvitation.role},
                    Items = new string[] { roleName},
                    sender = new eoConfigureInvitationSender()
                    {
                        firstName = senderFirstName,
                        lastName = senderLastName,
                        email = senderEmail
                    },                    
                };

                XmlSerializer x = new System.Xml.Serialization.XmlSerializer(configInvitation.GetType());
                var settings = new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize };
                MemoryStream ms = new MemoryStream();
                var writer = XmlWriter.Create(ms, settings);
                //MemoryStream ms = new MemoryStream();
                //XmlWriter writer = new XmlTextWriter(ms, Encoding.UTF8);
                x.Serialize(writer, configInvitation);
                writer.Flush();
                ms.Position = 0;

                //XmlWriter xmlWriter = new XmlTextWriter("testInvitation.xml", Encoding.UTF8);
                //x.Serialize(xmlWriter, configInvitation);
                //xmlWriter.Flush();

                //var fileContent = new ByteArrayContent(fileBytes);
                var fileContent = new ByteArrayContent(ms.ToArray());
                fileContent.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "instructionsXML",
                        FileName = "instructionsXML"
                    };

                var content = new MultipartFormDataContent();
                content.Add(fileContent, "instructionsXML");

                var response = await Client.Client.PostAsync(_fullUri + "/?action=eoConfigureInvitation", content).ConfigureAwait(false);

                var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

                if (eResponse?.status != responseStatus.ok)
                {
                    alerts = GetAlertsFromResponse(eResponse);
                }
                return alerts;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<Tuple<documentType, IList<Alert>>> GetCopy(long dpSid)
        {
            try
            {
                IList<Alert> alerts = new List<Alert>();

                var values = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("dpSid", dpSid.ToString()),
                };                
                var data = new FormUrlEncodedContent(values);

                var response = await Client.Client.PostAsync(_fullUri + "/?action=eoGetCopy", data).ConfigureAwait(false);

                var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

                if (eResponse?.status == responseStatus.ok)
                {
                    if (eResponse.eventResponse.ItemsElementName.Contains(ItemsChoiceType15.documentList))
                    {
                        var documentList = eResponse.eventResponse.Items[Array.IndexOf(eResponse.eventResponse.ItemsElementName, ItemsChoiceType15.documentList)] as EOriginalTypes.documentListType;
                        var document = documentList?.document?.FirstOrDefault();
                        return new Tuple<documentType, IList<Alert>>(document, alerts);
                    }
                }
                else
                {
                    alerts = GetAlertsFromResponse(eResponse);
                }
                return new Tuple<documentType, IList<Alert>>(null, alerts);
            }
            catch (Exception ex)
            {
                
                throw;
            }
        }

        public async Task<Tuple<signatureResultListTypeTransaction, IList<Alert>>> SearchSignatureResults(long transactionSid)
        {
            try
            {
                IList<Alert> alerts = new List<Alert>();

                var values = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("transactionSid", transactionSid.ToString()),
                };
                var data = new FormUrlEncodedContent(values);

                var response = await Client.Client.PostAsync(_fullUri + "/?action=eoSearchSignatureResults", data).ConfigureAwait(false);

                var eResponse = await response.Content.DeserializeFromStringAsync<EOriginalTypes.response>();

                if (eResponse?.status == responseStatus.ok)
                {
                    if (eResponse.eventResponse.ItemsElementName.Contains(ItemsChoiceType15.signatureResultTransactionList))
                    {
                        var signatureResultList = eResponse.eventResponse.Items[Array.IndexOf(eResponse.eventResponse.ItemsElementName, ItemsChoiceType15.signatureResultTransactionList)] as EOriginalTypes.signatureResultListType;
                        var signResult = signatureResultList?.transaction?.FirstOrDefault();
                        return new Tuple<signatureResultListTypeTransaction, IList<Alert>>(signResult, alerts);
                    }
                }
                else
                {
                    alerts = GetAlertsFromResponse(eResponse);
                }
                return new Tuple<signatureResultListTypeTransaction, IList<Alert>>(null, alerts);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        protected CookieContainer ReadCookies(HttpResponseMessage response)
        {
            var pageUri = response.RequestMessage.RequestUri;

            IEnumerable<string> cookies;
            //TODO: delete path
            if (response.Headers.TryGetValues("set-cookie", out cookies))
            {
                foreach (var c in cookies)
                {
                    var cookie = c;
                    var idx = c.IndexOf("Path", StringComparison.Ordinal);
                    if (idx > 0)
                    {
                        cookie = c.Substring(0, idx);
                    }
                    Client.Cookies.SetCookies(new Uri(_fullUri), cookie);
                }
            }
            //return null;
            return Client.Cookies;
        }

        private IList<Alert> GetAlertsFromResponse(response response)
        {
            var alerts = new List<Alert>();

            response?.errorList?.ForEach(e =>
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Message = e.ItemsElementName.Contains(ItemsChoiceType16.message) ? e.Items[Array.IndexOf(e.ItemsElementName, ItemsChoiceType16.message)] : string.Empty,
                    Header = e.ItemsElementName.Contains(ItemsChoiceType16.minorCode) ? e.Items[Array.IndexOf(e.ItemsElementName, ItemsChoiceType16.minorCode)] : string.Empty,                    
                })
                );

            return alerts;
        }
    }
}
