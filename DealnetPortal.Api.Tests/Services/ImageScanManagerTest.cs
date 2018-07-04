using System.IO;
using System.Linq;
using DealnetPortal.Api.Integration.Services;
using DealnetPortal.Api.Models.Scanning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DealnetPortal.Api.Tests.Services
{
    [TestClass]
    public class ImageScanManagerTest
    {

        [TestMethod]
        public void TestReadDriverLicense()
        {
            var imgRaw = File.ReadAllBytes("Img//Barcode-Driver_License.CA.jpg");
            ScanningRequest scanningRequest = new ScanningRequest()
            {
                ImageForReadRaw = imgRaw
            };

            var imageManager = new ImageScanService();
            var result = imageManager.ReadDriverLicense(scanningRequest);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Item1);
            Assert.IsNotNull(result.Item2);
            Assert.IsFalse(result.Item2.Any());
            Assert.IsTrue(result.Item1.FirstName.Contains("First"));
            Assert.IsTrue(result.Item1.LastName.Contains("Last"));
        }

        [TestMethod]
        public void TestCanadaDriverLicense()
        {
            var imgRaw = File.ReadAllBytes("Img//ML_DriverLicence.jpg");
            ScanningRequest scanningRequest = new ScanningRequest()
            {
                ImageForReadRaw = imgRaw
            };

            var imageManager = new ImageScanService();
            var result = imageManager.ReadDriverLicense(scanningRequest);
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Item1);
            Assert.IsNotNull(result.Item2);
            Assert.IsFalse(result.Item2.Any());
            Assert.IsTrue(!string.IsNullOrEmpty(result.Item1.FirstName));
            Assert.IsTrue(!string.IsNullOrEmpty(result.Item1.LastName));
        }

        [TestMethod]
        public void TestScanVoidCheque()
        {
            var imgRaw = File.ReadAllBytes("Img//cheque.jpg");
            ScanningRequest scanningRequest = new ScanningRequest()
            {
                ImageForReadRaw = imgRaw
            };
            var imageManager = new ImageScanService();
            var result = imageManager.ReadVoidCheque(scanningRequest);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestExtractCheck()
        {
            var imgRaw = File.ReadAllBytes("Img//cheque.jpg");
            ScanningRequest scanningRequest = new ScanningRequest()
            {
                ImageForReadRaw = imgRaw
            };
            var imageManager = new ImageScanService();
            var result = imageManager.ExtractCheck(scanningRequest);
            Assert.IsNotNull(result);
        }
    }
}
