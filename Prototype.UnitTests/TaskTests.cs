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
        public const string DefaultWorkflowName = "Test Workflow";
        public const string DefaultTaskName = "Test Task";
        public const string DefaultQueueName = "Test Queue";
        private Workflow defaultWorkflow;
        private Queue defaultQueue;
        private Database database;

        [TestInitialize]
        public void Initialise()
        {
            this.database = new Database();
            Workflow.Create(database, DefaultWorkflowName);
            Queue.Create(database, DefaultQueueName);
            defaultWorkflow = Workflow.Get(database, DefaultWorkflowName);
            defaultQueue = Queue.Get(database, DefaultQueueName);
        }

        [TestMethod]
        public void CreateTaskInSuspension()
        {
            Task.Create(database, DefaultWorkflowName, DefaultTaskName, DefaultQueueName);
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
            Workflow.Resume(database, DefaultWorkflowName);
            Task.Create(database, DefaultWorkflowName, DefaultTaskName, DefaultQueueName);
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
            var task = Task.Get(database, DefaultWorkflowName, DefaultTaskName);
            Assert.AreEqual(DefaultTaskName, task.Name);
        }

        [TestMethod]
        public void AddDependency()
        {
            var dependancyTaskName = "Dependancy Task";
            CreateTaskInSuspension();
            Task.Create(database, DefaultWorkflowName, dependancyTaskName, DefaultQueueName);
            Task.AddDependency(database, DefaultWorkflowName, dependancyTaskName, DefaultTaskName);
            var defaultTask = Task.Get(database, DefaultWorkflowName, DefaultTaskName);
            var dependancyTask = Task.Get(database, DefaultWorkflowName, dependancyTaskName);

            Assert.AreEqual(1, defaultTask.DependantOn.Count);
            Assert.AreEqual(0, defaultTask.DependencyTo.Count);
            Assert.AreEqual(1, defaultTask.OutstandingDependencies.Count);

            Assert.AreEqual(0, dependancyTask.DependantOn.Count);
            Assert.AreEqual(1, dependancyTask.DependencyTo.Count);
            Assert.AreEqual(0, dependancyTask.OutstandingDependencies.Count);
        }
    }
}
