namespace DealnetPortal.Api.Models.Signature
{

    public enum FieldType
    {
        Text = 0,
        CheckBox = 1
    };
    public class FormField
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public FieldType FieldType { get; set; }
    }
}
