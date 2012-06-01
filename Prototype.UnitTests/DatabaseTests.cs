using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicWorkflow.Prototype.UnitTests
{
    [TestClass]
    public class DatabaseTests
    {
        [TestMethod]
        public void InitialiseDatabase()
        {
            // Just test this doesn't throw an exception!
            var database = new Database();
        }
    }
}
