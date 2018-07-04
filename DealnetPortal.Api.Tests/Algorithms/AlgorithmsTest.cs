using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DealnetPortal.Api.Tests.Algorithms
{
    [TestClass]
    public class AlgorithmsTest
    {
        [TestMethod]
        public void TestChangeDocumentName()
        {
            string documentNameReplacedSymbols = " -+=#@$%^!~&;:'`(){}.,|\"";//[](),.';:";
            string docName = "&()=';[]{}";

            var uploadName = Regex.Replace(docName.Replace('[','_').Replace(']', '_'), $"[{documentNameReplacedSymbols}]", "_");
            Assert.IsFalse(uploadName.Aggregate(false, (b, c) => b || documentNameReplacedSymbols.Contains(c)));
        }
    }
}
