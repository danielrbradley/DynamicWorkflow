using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicWorkflow.Prototype.UnitTests
{
    [TestClass]
    public class TaskTests
    {
        public const string DefaultTaskName = "Test Task";
        private Workflow defaultWorkflow;
        private Queue defaultQueue;
        private Database database;

        public TaskTests()
        {
                this.database = new Database();
        }

        public TaskTests(Database database)
        {
            if (database == null)
                this.database = new Database();
            else
                this.database = database;
        }

        [TestInitialize]
        public void Initialise()
        {
            Workflow.Create(database, WorkflowTests.DefaultWorkflowName);
            Queue.Create(database, QueueTests.DefaultQueueName);
            defaultWorkflow = Workflow.Get(database, WorkflowTests.DefaultWorkflowName);
            defaultQueue = Queue.Get(database, QueueTests.DefaultQueueName);
        }

        [TestMethod]
        public void CreateTaskInSuspension()
        {
            Task.Create(database, WorkflowTests.DefaultWorkflowName, DefaultTaskName, QueueTests.DefaultQueueName);
            Assert.IsTrue(defaultWorkflow.TaskNames.ContainsKey(DefaultTaskName));
            Assert.IsTrue(defaultWorkflow.Tasks.ContainsKey(defaultWorkflow.TaskNames[DefaultTaskName]));
            Assert.AreEqual(0, defaultQueue.QueuedTasks.Count);
            Assert.AreEqual(defaultWorkflow.TaskNames[DefaultTaskName], defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].Id);
            Assert.AreEqual(DefaultTaskName, defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].Name);
            Assert.AreEqual(TaskState.Queued, defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].State);
            Assert.AreEqual(defaultQueue.Id, defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].QueueId);
            Assert.IsNotNull(defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].DependencyTo);
            Assert.IsNotNull(defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].DependantOn);
            Assert.IsNotNull(defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].OutstandingDependencies);
        }

        [TestMethod]
        public void CreateTask()
        {
            Workflow.Resume(database, WorkflowTests.DefaultWorkflowName);
            Task.Create(database, WorkflowTests.DefaultWorkflowName, DefaultTaskName, QueueTests.DefaultQueueName);
            Assert.IsTrue(defaultWorkflow.TaskNames.ContainsKey(DefaultTaskName));
            Assert.IsTrue(defaultWorkflow.Tasks.ContainsKey(defaultWorkflow.TaskNames[DefaultTaskName]));
            Assert.AreEqual(1, defaultQueue.QueuedTasks.Count);
            Assert.AreEqual(defaultWorkflow.TaskNames[DefaultTaskName], defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].Id);
            Assert.AreEqual(DefaultTaskName, defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].Name);
            Assert.AreEqual(TaskState.Queued, defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].State);
            Assert.AreEqual(defaultQueue.Id, defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].QueueId);
            Assert.IsNotNull(defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].DependencyTo);
            Assert.IsNotNull(defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].DependantOn);
            Assert.IsNotNull(defaultWorkflow.Tasks[defaultWorkflow.TaskNames[DefaultTaskName]].OutstandingDependencies);
        }

        [TestMethod]
        public void GetTask()
        {
            CreateTaskInSuspension();
            var task = Task.Get(database, WorkflowTests.DefaultWorkflowName, DefaultTaskName);
            Assert.AreEqual(DefaultTaskName, task.Name);
        }

        [TestMethod]
        public void AddDependencyInSuspension()
        {
            var dependancyTaskName = "Dependancy Task";
            CreateTaskInSuspension();
            Task.Create(database, WorkflowTests.DefaultWorkflowName, dependancyTaskName, QueueTests.DefaultQueueName);
            Task.AddDependency(database, WorkflowTests.DefaultWorkflowName, dependancyTaskName, DefaultTaskName);
            var defaultTask = Task.Get(database, WorkflowTests.DefaultWorkflowName, DefaultTaskName);
            var dependancyTask = Task.Get(database, WorkflowTests.DefaultWorkflowName, dependancyTaskName);

            Assert.AreEqual(1, defaultTask.DependantOn.Count);
            Assert.AreEqual(0, defaultTask.DependencyTo.Count);
            Assert.AreEqual(1, defaultTask.OutstandingDependencies.Count);

            Assert.AreEqual(0, dependancyTask.DependantOn.Count);
            Assert.AreEqual(1, dependancyTask.DependencyTo.Count);
            Assert.AreEqual(0, dependancyTask.OutstandingDependencies.Count);
        }

        [TestMethod]
        public void AddDependency()
        {
            var dependancyTaskName = "Dependancy Task";
            CreateTask();
            Task.Create(database, WorkflowTests.DefaultWorkflowName, dependancyTaskName, QueueTests.DefaultQueueName);
            Task.AddDependency(database, WorkflowTests.DefaultWorkflowName, dependancyTaskName, DefaultTaskName);
            var defaultTask = Task.Get(database, WorkflowTests.DefaultWorkflowName, DefaultTaskName);
            var dependancyTask = Task.Get(database, WorkflowTests.DefaultWorkflowName, dependancyTaskName);

            Assert.AreEqual(1, defaultTask.DependantOn.Count);
            Assert.AreEqual(0, defaultTask.DependencyTo.Count);
            Assert.AreEqual(1, defaultTask.OutstandingDependencies.Count);

            Assert.AreEqual(0, dependancyTask.DependantOn.Count);
            Assert.AreEqual(1, dependancyTask.DependencyTo.Count);
            Assert.AreEqual(0, dependancyTask.OutstandingDependencies.Count);

            Assert.AreEqual(1, Queue.Get(database, QueueTests.DefaultQueueName).QueuedTasks.Count);
        }
    }
}
