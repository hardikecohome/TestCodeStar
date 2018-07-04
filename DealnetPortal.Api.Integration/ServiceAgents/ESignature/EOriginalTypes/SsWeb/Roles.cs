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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles", IsNullable = false)]
    public partial class eoConfigureRoles
    {        
        [XmlAttribute("schemaLocation", Namespace = XmlSchema.InstanceNamespace)]
        public string xsiSchemaLocation = "http://www.eoriginal.com/ssweb/ConfigureRoles http://schemas.eoriginal.com/releases/8.5/ssweb/configure-roles.xsd";

        private eoConfigureRolesRole[] rolesListField;

        private string transactionSidField;

        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("role", IsNullable = false)]
        public eoConfigureRolesRole[] rolesList
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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles")]
    public partial class eoConfigureRolesRole
    {

        private string firstNameField;

        private string middleNameField;

        private string lastNameField;

        private string suffixField;

        private string eMailField;

        private string[] itemsField;

        private ItemsChoiceType[] itemsElementNameField;

        private string notesField;

        private eoConfigureRolesRoleSignatureMode signatureModeField;

        private bool signatureModeFieldSpecified;

        private eoConfigureRolesRoleSignatureCaptureMethod signatureCaptureMethodField;

        private object itemField;

        private ItemChoiceType itemElementNameField;

        private string attachInstructionsField;

        private string wetInkInstructionsField;

        private string dataCollectionInstructionsField;

        private string smsVerificationField;

        private EVSParametersType item1Field;

        private string nameField;

        private string orderField;

        private bool requiredField;

        private bool collectBiometricVoiceVerificationField;

        private bool collectBiometricPictureVerificationField;

        private string reminderDaysField;

        private bool smsVerification1Field;

        public eoConfigureRolesRole()
        {
            this.requiredField = true;
            this.collectBiometricVoiceVerificationField = false;
            this.collectBiometricPictureVerificationField = false;
            this.reminderDaysField = "0";
            this.smsVerification1Field = false;
        }

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
        public string middleName
        {
            get
            {
                return this.middleNameField;
            }
            set
            {
                this.middleNameField = value;
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
        public string suffix
        {
            get
            {
                return this.suffixField;
            }
            set
            {
                this.suffixField = value;
            }
        }

        /// <remarks/>
        public string eMail
        {
            get
            {
                return this.eMailField;
            }
            set
            {
                this.eMailField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("securityCode", typeof(string))]
        [System.Xml.Serialization.XmlElementAttribute("securityCodeSentBySMS", typeof(string))]
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
        public ItemsChoiceType[] ItemsElementName
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
        public string notes
        {
            get
            {
                return this.notesField;
            }
            set
            {
                this.notesField = value;
            }
        }

        /// <remarks/>
        public eoConfigureRolesRoleSignatureMode signatureMode
        {
            get
            {
                return this.signatureModeField;
            }
            set
            {
                this.signatureModeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool signatureModeSpecified
        {
            get
            {
                return this.signatureModeFieldSpecified;
            }
            set
            {
                this.signatureModeFieldSpecified = value;
            }
        }

        /// <remarks/>
        public eoConfigureRolesRoleSignatureCaptureMethod signatureCaptureMethod
        {
            get
            {
                return this.signatureCaptureMethodField;
            }
            set
            {
                this.signatureCaptureMethodField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("exam", typeof(object))]
        [System.Xml.Serialization.XmlElementAttribute("ofac", typeof(object))]
        [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemElementName")]
        public object Item
        {
            get
            {
                return this.itemField;
            }
            set
            {
                this.itemField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public ItemChoiceType ItemElementName
        {
            get
            {
                return this.itemElementNameField;
            }
            set
            {
                this.itemElementNameField = value;
            }
        }

        /// <remarks/>
        public string attachInstructions
        {
            get
            {
                return this.attachInstructionsField;
            }
            set
            {
                this.attachInstructionsField = value;
            }
        }

        /// <remarks/>
        public string wetInkInstructions
        {
            get
            {
                return this.wetInkInstructionsField;
            }
            set
            {
                this.wetInkInstructionsField = value;
            }
        }

        /// <remarks/>
        public string dataCollectionInstructions
        {
            get
            {
                return this.dataCollectionInstructionsField;
            }
            set
            {
                this.dataCollectionInstructionsField = value;
            }
        }

        /// <remarks/>
        public string smsVerification
        {
            get
            {
                return this.smsVerificationField;
            }
            set
            {
                this.smsVerificationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("evsVerificationParameters")]
        public EVSParametersType Item1
        {
            get
            {
                return this.item1Field;
            }
            set
            {
                this.item1Field = value;
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

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "nonNegativeInteger")]
        public string order
        {
            get
            {
                return this.orderField;
            }
            set
            {
                this.orderField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(true)]
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
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool collectBiometricVoiceVerification
        {
            get
            {
                return this.collectBiometricVoiceVerificationField;
            }
            set
            {
                this.collectBiometricVoiceVerificationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool collectBiometricPictureVerification
        {
            get
            {
                return this.collectBiometricPictureVerificationField;
            }
            set
            {
                this.collectBiometricPictureVerificationField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "nonNegativeInteger")]
        [System.ComponentModel.DefaultValueAttribute("0")]
        public string reminderDays
        {
            get
            {
                return this.reminderDaysField;
            }
            set
            {
                this.reminderDaysField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("smsVerification")]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool smsVerification1
        {
            get
            {
                return this.smsVerification1Field;
            }
            set
            {
                this.smsVerification1Field = value;
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles", IncludeInSchema = false)]
    public enum ItemsChoiceType
    {

        /// <remarks/>
        securityCode,

        /// <remarks/>
        securityCodeSentBySMS,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles")]
    public enum eoConfigureRolesRoleSignatureMode
    {

        /// <remarks/>
        MANUAL,

        /// <remarks/>
        ACKNOWLEDGED,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles")]
    public partial class eoConfigureRolesRoleSignatureCaptureMethod
    {

        private bool lockField;

        private bool lockFieldSpecified;

        private signatureCaptureMethodType valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool @lock
        {
            get
            {
                return this.lockField;
            }
            set
            {
                this.lockField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool lockSpecified
        {
            get
            {
                return this.lockFieldSpecified;
            }
            set
            {
                this.lockFieldSpecified = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public signatureCaptureMethodType Value
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
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles")]
    public enum signatureCaptureMethodType
    {

        /// <remarks/>
        TYPE,

        /// <remarks/>
        DRAW,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("")]
        Item,
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(EVSParametersType))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles")]
    public partial class VerificationParametersType
    {
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles", IncludeInSchema = false)]
    public enum ItemChoiceType
    {

        /// <remarks/>
        exam,

        /// <remarks/>
        ofac,
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles")]
    [System.Xml.Serialization.XmlRootAttribute("evsVerificationParameters", Namespace = "http://www.eoriginal.com/ssweb/ConfigureRoles", IsNullable = false)]
    public partial class EVSParametersType : VerificationParametersType
    {

        private string firstNameField;

        private string lastNameField;

        private string address1Field;

        private string address2Field;

        private string cityField;

        private string stateField;

        private string zipField;

        private string socialSecurityNumberField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "token")]
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
        [System.Xml.Serialization.XmlElementAttribute(DataType = "token")]
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
        [System.Xml.Serialization.XmlElementAttribute(DataType = "token")]
        public string address1
        {
            get
            {
                return this.address1Field;
            }
            set
            {
                this.address1Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "token")]
        public string address2
        {
            get
            {
                return this.address2Field;
            }
            set
            {
                this.address2Field = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "token")]
        public string city
        {
            get
            {
                return this.cityField;
            }
            set
            {
                this.cityField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "token")]
        public string state
        {
            get
            {
                return this.stateField;
            }
            set
            {
                this.stateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "token")]
        public string zip
        {
            get
            {
                return this.zipField;
            }
            set
            {
                this.zipField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "token")]
        public string socialSecurityNumber
        {
            get
            {
                return this.socialSecurityNumberField;
            }
            set
            {
                this.socialSecurityNumberField = value;
            }
        }
    }
}
