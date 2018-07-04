using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Api.Models.Notification
{
    public class MandrillRequest
    {
        public string key { get; set; }
        public string template_name { get; set; }
        public List<templatecontent> template_content { get; set; }
        public MandrillMessage message { get; set; }
        
    }

    public class MandrillMessage
    {
        public string html { get; set; }
        public string text { get; set; }
        public string subject { get; set; }
        public string from_email { get; set; }
        public string from_name { get; set; }
        public List<MandrillTo> to { get; set; }
        public List<MergeVariable> merge_vars { get; set; }

        public DateTime send_at = DateTime.Now;
    }

    public class MergeVariable
    {
        public string rcpt { get; set; }
        public List<Variable> vars { get; set; }
    }

    public class Variable
    {
        public string name { get; set; }
        public string content { get; set; }
    }

    public class MandrillTo
    {
        public string email { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }

    public class templatecontent
    {
        public string name { get; set; }
        public string content { get; set; }
    }
}
