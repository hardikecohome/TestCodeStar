using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace DealnetPortal.Aspire.Integration.Models
{
    [Serializable]
    [XmlRoot(ElementName = "DocumentUploadXML")]
    public class DocumentUploadRequest : DealUploadRequest, IXmlSerializable
    {
        private DocumentUploadPayload _payload;
        public override Payload Payload
        {
            get { return _payload; }
            set { _payload = value as DocumentUploadPayload; }
        }
        public XmlSchema GetSchema()
        {
            throw new NotImplementedException();
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            if (Header != null)
            {
                writer.WriteStartElement("Header");
                writer.WriteElementString("UserId", Header.UserId);
                writer.WriteElementString("Password", Header.Password);
                writer.WriteEndElement();
            }
            if (Payload != null)
            {
                writer.WriteStartElement("Payload");
                writer.WriteElementString("TransactionId", _payload.TransactionId);
                writer.WriteElementString("Status", _payload.Status);
                _payload.Documents?.ForEach(doc =>
                {
                    writer.WriteStartElement("Document");
                    if (!string.IsNullOrEmpty(doc?.Name))
                    {
                        writer.WriteElementString("Name", doc.Name);
                    }
                    if (!string.IsNullOrEmpty(doc?.Data))
                    {
                        writer.WriteElementString("Data", doc.Data);
                    }
                    if (!string.IsNullOrEmpty(doc?.Ext))
                    {
                        writer.WriteElementString("Ext", doc.Ext);
                    }
                    writer.WriteEndElement();
                });
                writer.WriteEndElement();
            }
        }
    }

    [Serializable]
    public class DocumentUploadPayload : Payload
    {
        public string ContractId { get; set; }
        public string Status { get; set; }

        [XmlElement("Document")]
        public List<Document> Documents { get; set; }        
    }

    [Serializable]
    public class Document
    {
        public string Name { get; set; }
        public string Data { get; set; }
        public string Ext { get; set; }
    }
}