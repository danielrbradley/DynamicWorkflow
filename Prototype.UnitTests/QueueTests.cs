using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DynamicWorkflow.Prototype;

namespace DynamicWorkflow.Prototype.UnitTests
{
    [TestClass]
    public class QueueTests
    {
        public const string DefaultQueueName = "Test Queue";
        private Database database;

        [TestInitialize]
        public void Initialise()
        {
            this.database = new Database();
        }

        [TestMethod]
        public void CreateQueue()
        {
            Queue.Create(database, DefaultQueueName);
            Assert.IsTrue(database.QueueNames.ContainsKey(DefaultQueueName));
            Assert.IsTrue(database.Queues.ContainsKey(database.QueueNames[DefaultQueueName]));
            Assert.AreEqual(database.QueueNames[DefaultQueueName], database.Queues[database.QueueNames[DefaultQueueName]].Id);
            Assert.AreEqual(DefaultQueueName, database.Queues[database.QueueNames[DefaultQueueName]].Name);
            Assert.IsNotNull(database.Queues[database.QueueNames[DefaultQueueName]].QueuedTasks);
            Assert.IsNotNull(database.Queues[database.QueueNames[DefaultQueueName]].QueueLock);
            Assert.IsNotNull(database.Queues[database.QueueNames[DefaultQueueName]].RunningTasks);
        }

        [TestMethod]
        public void GetQueue()
        {
            CreateQueue();
            var workflow = Queue.Get(database, DefaultQueueName);
            Assert.AreEqual(DefaultQueueName, workflow.Name);
        }

        [TestMethod]
        public void QueueExists()
        {
            CreateQueue();
            Assert.IsTrue(Queue.Exists(database, DefaultQueueName));
        }

        [TestMethod]
        public void QueueNotExists()
        {
            // Don't create default first.
            Assert.IsFalse(Queue.Exists(database, DefaultQueueName));
        }

        [TestMethod]
        public void PeekEmpty()
        {
            CreateQueue();
            Assert.IsNull(Queue.Peek(database, DefaultQueueName));
        }

        [TestMethod]
        public void PeekTask()
        {
            QueueDefaultTask();
            var nextTask = Queue.Peek(database, DefaultQueueName);
            Assert.IsNotNull(nextTask);
            Assert.AreEqual(WorkflowTests.DefaultWorkflowName, nextTask.WorkflowName);
            Assert.AreEqual(TaskTests.DefaultTaskName, nextTask.TaskName);
        }

        [TestMethod]
        public void DequeueTask()
        {
            QueueDefaultTask();
            var nextTask = Queue.Dequeue(database, DefaultQueueName);
            Assert.IsNotNull(nextTask);
            Assert.AreEqual(WorkflowTests.DefaultWorkflowName, nextTask.WorkflowName);
            Assert.AreEqual(TaskTests.DefaultTaskName, nextTask.TaskName);
            Assert.IsTrue(Queue.IsEmpty(database, DefaultQueueName));
            Assert.AreEqual(1, Queue.Get(database, DefaultQueueName).RunningTasks.Count);
            Assert.AreEqual(TaskState.Running, Task.Get(database, WorkflowTests.DefaultWorkflowName, TaskTests.DefaultTaskName).State);
        }

        [TestMethod]
        public void CompleteTask()
        {
            QueueDefaultTask();
            var dequeuedTask = Queue.Dequeue(database, DefaultQueueName);
            Queue.Complete(database, dequeuedTask.WorkflowName, dequeuedTask.TaskName);
            var queue = Queue.Get(database, DefaultQueueName);
            Assert.AreEqual(0, queue.RunningTasks.Count);
            Assert.AreEqual(0, queue.QueuedTasks.Count);
            Assert.IsFalse(Workflow.Exists(database, WorkflowTests.DefaultWorkflowName));
        }

        private void QueueDefaultTask()
        {
            var taskTests = new TaskTests(database);
            taskTests.Initialise();
            taskTests.CreateTask();
        }
    }
}
