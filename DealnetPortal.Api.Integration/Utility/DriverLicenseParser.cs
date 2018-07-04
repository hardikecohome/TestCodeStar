using System;
using System.Linq;
using System.Xml.Linq;
using DealnetPortal.Api.Models.Scanning;

namespace DealnetPortal.Api.Integration.Utility
{
    class DriverLicenseParser
    {        
        public DriverLicenseParser()
        {            
        }

        public DriverLicenseParser(string xmlString)
        {
            Parse(xmlString);
        }

        public void Parse(string xmlString)
        {
            DriverLicense = new DriverLicenseData();
            
            XDocument xDoc = XDocument.Parse(xmlString);
            var elems = xDoc.Descendants("user").Descendants().ToList();
            if (elems.Any())
            {
                DriverLicense.Id = elems.FirstOrDefault(x => x.Name == "id")?.Value;
                DriverLicense.FirstName = elems.FirstOrDefault(x => x.Name == "first")?.Value;
                DriverLicense.LastName = elems.FirstOrDefault(x => x.Name == "last")?.Value;
                DriverLicense.MiddleName = elems.FirstOrDefault(x => x.Name == "middle")?.Value;
                DriverLicense.Suffix = elems.FirstOrDefault(x => x.Name == "suffix")?.Value;
                DriverLicense.Sex = elems.FirstOrDefault(x => x.Name == "sex")?.Value;
                DriverLicense.Street = elems.FirstOrDefault(x => x.Name == "street")?.Value?.Trim().Trim(',');
                DriverLicense.State = elems.FirstOrDefault(x => x.Name == "state")?.Value;
                DriverLicense.City = elems.FirstOrDefault(x => x.Name == "city")?.Value;
                DriverLicense.Country = elems.FirstOrDefault(x => x.Name == "country")?.Value;
                DriverLicense.PostalCode = elems.FirstOrDefault(x => x.Name == "postal")?.Value?.Replace(" ", "");
                DateTime date;
                DateTime.TryParse(elems.FirstOrDefault(x => x.Name == "dob")?.Value, out date);
                DriverLicense.DateOfBirth = date;
                DriverLicense.DateOfBirthStr = elems.FirstOrDefault(x => x.Name == "dob")?.Value;

                DateTime.TryParse(elems.FirstOrDefault(x => x.Name == "issued")?.Value, out date);
                DriverLicense.Issued = date;
                DateTime.TryParse(elems.FirstOrDefault(x => x.Name == "expires")?.Value, out date);
                DriverLicense.Expires = date;                
            }           
        }

        public DriverLicenseData DriverLicense { get; private set; }
    }
}
