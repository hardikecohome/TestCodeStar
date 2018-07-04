using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealnetPortal.Api.Common.Enumeration;
using DealnetPortal.Api.Core.Enums;
using DealnetPortal.Api.Core.Types;
using DealnetPortal.Api.Models;
using DealnetPortal.Api.Models.Signature;
using DocuSign.eSign.Model;
using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.IO;
using Unity.Interception.Utilities;

namespace DealnetPortal.Api.Integration.Services.Signature
{
    public class PdfSharpEngine : IPdfEngine
    {
        public Tuple<Stream, IList<Alert>> InsertFormFields(Stream inFormStream, IList<FormField> formFields)
        {
            var alerts = new List<Alert>();
            MemoryStream outStream = null;
            try
            {
                PdfDocument document = PdfReader.Open(inFormStream, PdfDocumentOpenMode.Modify);
                // Get the root object of all interactive form fields
                PdfAcroForm form = document.AcroForm;
                if (form.Elements.ContainsKey("/NeedAppearances"))
                {
                    form.Elements["/NeedAppearances"] = new PdfBoolean(true);
                }
                else
                {
                    form.Elements.Add("/NeedAppearances", new PdfBoolean(true));
                }
                // Get all form fields of the whole document
                PdfAcroField.PdfAcroFieldCollection fields = form.Fields;

                if (formFields?.Any() ?? false)
                {
                    formFields.ForEach(ff =>
                    {
                        PdfAcroField field = fields[ff.Name];
                        if (field != null)
                        {
                            if (ff.FieldType == FieldType.CheckBox && (field is PdfCheckBoxField))
                            {
                                (field as PdfCheckBoxField).ReadOnly = false;
                                var bVal = false;
                                bool.TryParse(ff.Value, out bVal);
                                (field as PdfCheckBoxField).Checked = bVal;
                                (field as PdfCheckBoxField).ReadOnly = true;
                            }
                            else //Text
                            {
                                if (field is PdfTextField)
                                {
                                    (field as PdfTextField).ReadOnly = false;
                                    (field as PdfTextField).Value = new PdfString(ff.Value);
                                    (field as PdfTextField).ReadOnly = true;
                                }
                            }
                        }
                        //field.ReadOnly = true;
                    });
                }
                outStream = new MemoryStream();
                document.Save(outStream);               
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "PDF Sharp error",
                    Message = ex.ToString()
                });
            }
            return new Tuple<Stream, IList<Alert>>(outStream, alerts);
        }

        public Tuple<IList<FormField>, IList<Alert>> GetFormfFields(Stream inFormStream)
        {
            var alerts = new List<Alert>();
            IList<FormField> ffields = null;
            try
            {
                PdfDocument document = PdfReader.Open(inFormStream, PdfDocumentOpenMode.Modify);
                // Get the root object of all interactive form fields
                PdfAcroForm form = document.AcroForm;                
                // Get all form fields of the whole document
                PdfAcroField.PdfAcroFieldCollection fields = form.Fields;
                ffields = fields.Names?.Select(f => new FormField()
                {
                    Name = f,
                }).ToList();                
            }
            catch (Exception ex)
            {
                alerts.Add(new Alert()
                {
                    Type = AlertType.Error,
                    Header = "PDF Sharp error",
                    Message = ex.ToString()
                });
            }
            return new Tuple<IList<FormField>, IList<Alert>>(ffields, alerts);
        }
    }
}
