using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace DealnetPortal.Api.Integration.ServiceAgents.ESignature.EOriginalTypes.SsWeb
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureInvitation")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureInvitation", IsNullable = false)]
    public partial class eoConfigureInvitation
    {
        [XmlAttribute("schemaLocation", Namespace = XmlSchema.InstanceNamespace)]
        public string xsiSchemaLocation = "http://www.eoriginal.com/ssweb/ConfigureInvitation http://schemas.eoriginal.com/releases/8.5/ssweb/configure-invitation.xsd";

        private string[] itemsField;

        private ItemsChoiceTypeInvitation[] itemsElementNameField;

        private string subjectField;

        private string bodyField;

        private eoConfigureInvitationSender senderField;

        private string transactionSidField;

        private string emailTemplateNameField;

        private bool includeStandardLanguageField;

        public eoConfigureInvitation()
        {
            this.includeStandardLanguageField = true;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("email", typeof(string))]
        [System.Xml.Serialization.XmlElementAttribute("role", typeof(string))]
        [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
        public string[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public ItemsChoiceTypeInvitation[] ItemsElementName
        {
            get
            {
                return this.itemsElementNameField;
            }
            set
            {
                this.itemsElementNameField = value;
            }
        }

        /// <remarks/>
        public string subject
        {
            get
            {
                return this.subjectField;
            }
            set
            {
                this.subjectField = value;
            }
        }

        /// <remarks/>
        public string body
        {
            get
            {
                return this.bodyField;
            }
            set
            {
                this.bodyField = value;
            }
        }

        /// <remarks/>
        public eoConfigureInvitationSender sender
        {
            get
            {
                return this.senderField;
            }
            set
            {
                this.senderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "integer")]
        public string transactionSid
        {
            get
            {
                return this.transactionSidField;
            }
            set
            {
                this.transactionSidField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string emailTemplateName
        {
            get
            {
                return this.emailTemplateNameField;
            }
            set
            {
                this.emailTemplateNameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool includeStandardLanguage
        {
            get
            {
                return this.includeStandardLanguageField;
            }
            set
            {
                this.includeStandardLanguageField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureInvitation", IncludeInSchema = false)]
    public enum ItemsChoiceTypeInvitation
    {

        /// <remarks/>
        email,

        /// <remarks/>
        role,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureInvitation")]
    public partial class eoConfigureInvitationSender
    {

        private string firstNameField;

        private string lastNameField;

        private string emailField;

        private string phoneField;

        /// <remarks/>
        public string firstName
        {
            get
            {
                return this.firstNameField;
            }
            set
            {
                this.firstNameField = value;
            }
        }

        /// <remarks/>
        public string lastName
        {
            get
            {
                return this.lastNameField;
            }
            set
            {
                this.lastNameField = value;
            }
        }

        /// <remarks/>
        public string email
        {
            get
            {
                return this.emailField;
            }
            set
            {
                this.emailField = value;
            }
        }

        /// <remarks/>
        public string phone
        {
            get
            {
                return this.phoneField;
            }
            set
            {
                this.phoneField = value;
            }
        }
    }
}
