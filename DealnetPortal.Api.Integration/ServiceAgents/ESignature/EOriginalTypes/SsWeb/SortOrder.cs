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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureSortOrder")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureSortOrder", IsNullable = false)]
    public partial class eoConfigureSortOrder
    {
        [XmlAttribute("schemaLocation", Namespace = XmlSchema.InstanceNamespace)]
        public string xsiSchemaLocation = "http://www.eoriginal.com/ssweb/ConfigureSortOrder http://schemas.eoriginal.com/releases/8.5/ssweb/configure-sort-order.xsd";

        private DocumentType[] documentListField;

        private UploadDocumentType[] attachDocumentListField;

        private System.DateTime expirationDateField;

        private bool expirationDateFieldSpecified;

        private eoConfigureSortOrderRole[] rolesListField;

        private string transactionSidField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("dpSid", IsNullable = false)]
        public DocumentType[] documentList
        {
            get
            {
                return this.documentListField;
            }
            set
            {
                this.documentListField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("dpSid", IsNullable = false)]
        public UploadDocumentType[] attachDocumentList
        {
            get
            {
                return this.attachDocumentListField;
            }
            set
            {
                this.attachDocumentListField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime expirationDate
        {
            get
            {
                return this.expirationDateField;
            }
            set
            {
                this.expirationDateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool expirationDateSpecified
        {
            get
            {
                return this.expirationDateFieldSpecified;
            }
            set
            {
                this.expirationDateFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("role", IsNullable = false)]
        public eoConfigureSortOrderRole[] rolesList
        {
            get
            {
                return this.rolesListField;
            }
            set
            {
                this.rolesListField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "nonNegativeInteger")]
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
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureSortOrder")]
    public partial class DocumentType
    {

        private bool requiredField;

        private bool requiredFieldSpecified;

        private DocumentTypeType typeField;

        private string valueField;

        public DocumentType()
        {
            this.typeField = DocumentTypeType.STANDARD;
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool required
        {
            get
            {
                return this.requiredField;
            }
            set
            {
                this.requiredField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool requiredSpecified
        {
            get
            {
                return this.requiredFieldSpecified;
            }
            set
            {
                this.requiredFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(DocumentTypeType.STANDARD)]
        public DocumentTypeType type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute(DataType = "nonNegativeInteger")]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureSortOrder")]
    public enum DocumentTypeType
    {

        /// <remarks/>
        STANDARD,

        /// <remarks/>
        WET_INK,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureSortOrder")]
    public partial class UploadDocumentType
    {

        private bool requiredField;

        private bool requiredFieldSpecified;

        private string typeField;

        private string valueField;

        public UploadDocumentType()
        {
            this.typeField = "UPLOAD";
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool required
        {
            get
            {
                return this.requiredField;
            }
            set
            {
                this.requiredField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool requiredSpecified
        {
            get
            {
                return this.requiredFieldSpecified;
            }
            set
            {
                this.requiredFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute(DataType = "nonNegativeInteger")]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureSortOrder")]
    public partial class eoConfigureSortOrderRole
    {

        private eoConfigureSortOrderRoleDpSid[] documentListField;

        private UploadDocumentType[] attachDocumentListField;

        private string nameField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("dpSid", IsNullable = false)]
        public eoConfigureSortOrderRoleDpSid[] documentList
        {
            get
            {
                return this.documentListField;
            }
            set
            {
                this.documentListField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("dpSid", IsNullable = false)]
        public UploadDocumentType[] attachDocumentList
        {
            get
            {
                return this.attachDocumentListField;
            }
            set
            {
                this.attachDocumentListField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureSortOrder")]
    public partial class eoConfigureSortOrderRoleDpSid
    {

        private bool requiredField;

        private bool requiredFieldSpecified;

        private eoConfigureSortOrderRoleDpSidType typeField;

        private bool typeFieldSpecified;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool required
        {
            get
            {
                return this.requiredField;
            }
            set
            {
                this.requiredField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool requiredSpecified
        {
            get
            {
                return this.requiredFieldSpecified;
            }
            set
            {
                this.requiredFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public eoConfigureSortOrderRoleDpSidType type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool typeSpecified
        {
            get
            {
                return this.typeFieldSpecified;
            }
            set
            {
                this.typeFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute(DataType = "nonNegativeInteger")]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureSortOrder")]
    public enum eoConfigureSortOrderRoleDpSidType
    {

        /// <remarks/>
        STANDARD,

        /// <remarks/>
        WET_INK,
    }
}
