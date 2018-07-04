using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealnetPortal.Domain
{
    public class LoggedInUser
    {/// <summary>
     /// username
     /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// user id
        /// </summary>
        public string UserId { get; set; }
               
        /// <summary>
        /// Constructor, defines sames username and staff code
        /// </summary>
        /// <param name="userName">user name </param>
        /// <param name="staffCode">staff code </param>
        public LoggedInUser(string userName, string userId)
        {
            UserName = userName;
            UserId = userId;
        }        
    }
}
