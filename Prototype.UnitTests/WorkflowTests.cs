using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicWorkflow.Prototype.UnitTests
{
    [TestClass]
    public class WorkflowTests
    {
        public const string DefaultWorkflowName = "Test Workflow";
        private Database database;

        [TestInitialize]
        public void Initialise()
        {
            this.database = new Database();
        }

        [TestMethod]
        public void CreateWorkflow()
        {
            Workflow.Create(database, DefaultWorkflowName);
            Assert.IsTrue(database.WorkflowNames.ContainsKey(DefaultWorkflowName));
            Assert.IsTrue(database.Workflows.ContainsKey(database.WorkflowNames[DefaultWorkflowName]));
            Assert.AreEqual(database.WorkflowNames[DefaultWorkflowName], database.Workflows[database.WorkflowNames[DefaultWorkflowName]].Id);
            Assert.AreEqual(DefaultWorkflowName, database.Workflows[database.WorkflowNames[DefaultWorkflowName]].Name);
            Assert.IsTrue(database.Workflows[database.WorkflowNames[DefaultWorkflowName]].IsSuspended);
            Assert.IsNotNull(database.Workflows[database.WorkflowNames[DefaultWorkflowName]].CompletedTasks);
            Assert.IsNotNull(database.Workflows[database.WorkflowNames[DefaultWorkflowName]].TaskNames);
            Assert.IsNotNull(database.Workflows[database.WorkflowNames[DefaultWorkflowName]].Tasks);
            Assert.IsNotNull(database.Workflows[database.WorkflowNames[DefaultWorkflowName]].WorkflowLock);
        }

        [TestMethod]
        public void GetWorkflow()
        {
            CreateWorkflow();
            var workflow = Workflow.Get(database, DefaultWorkflowName);
            Assert.AreEqual(DefaultWorkflowName, workflow.Name);
        }

        [TestMethod]
        public void WorkflowExists()
        {
            CreateWorkflow();
            Assert.IsTrue(Workflow.Exists(database, DefaultWorkflowName));
        }

        [TestMethod]
        public void WorkflowNotExists()
        {
            // Don't create default first.
            Assert.IsFalse(Workflow.Exists(database, DefaultWorkflowName));
        }
    }
}
