using System;
using System.Net;
using Jirassic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace JirassicTests
{
    [TestClass]
    public class MainWindowTests
    {
        MainWindow sut;

        [TestInitialize]
        public void TestInitializer()
        {
            sut = new MainWindow();
        }

        [TestMethod]
        public void CheckAssociateIdFalse()
        {
            bool result = sut.CountIdLength("ps056572a");

            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void CheckAssociateIdTrue()
        {
            bool result = sut.CountIdLength("vk056646");

            Assert.AreEqual(true, result);
        } 
        
        [TestMethod]
        public void AssociateIdCaseCheck()
        {
            string id = "ar035595";

            string result = sut.GetAssociateID(id);
            string expected = id.Substring(0, 2).ToUpper();
            string actual = result.Substring(0, 2);

            Assert.AreEqual(expected, actual);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            sut.Close();
        }
    }
}
