using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace DynamicWorkflow.Prototype
{
    public class Queue
    {
        internal Queue(string name)
        {
            this.Id = Guid.NewGuid();
            this.Name = name;
            this.QueuedTasks = new LinkedList<Tuple<Guid, Guid>>();
            this.RunningTasks = new HashSet<Guid>();
            this.QueueLock = new ReaderWriterLockSlim();
        }

        internal readonly Guid Id;
        internal readonly string Name;

        /// <summary>
        /// Tuples of Workflow Id, TaskId
        /// </summary>
        internal LinkedList<Tuple<Guid, Guid>> QueuedTasks;
        internal HashSet<Guid> RunningTasks;
        internal readonly ReaderWriterLockSlim QueueLock;

        public static void Create(Database database, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");
            if (Exists(database, name))
                throw new ArgumentException(string.Format("Queue with the name \"{0}\" already exists.", name), "name");

            var queue = new Queue(name);
            queue.QueueLock.EnterWriteLock();
            try
            {
                database.QueuesLock.EnterWriteLock();
                try
                {
                    database.QueueNames.Add(queue.Name, queue.Id);
                    database.Queues.Add(queue.Id, queue);
                }
                finally
                {
                    database.QueuesLock.ExitWriteLock();
                }
            }
            finally
            {
                queue.QueueLock.ExitWriteLock();
            }
        }

        public static void Delete(Database database, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");
            if (!Queue.Exists(database, name))
                throw new ArgumentException(string.Format("Queue with the name \"{0}\" not found.", name), "name");
            if (!Queue.IsEmpty(database, name))
                throw new InvalidOperationException(string.Format("Failed deleting queue with name \"{0}\", queue is not empty.", name));

            var queue = Queue.Get(database, name);

            queue.QueueLock.EnterWriteLock();
            try
            {
                if (queue.QueuedTasks.Any())
                    throw new InvalidOperationException(string.Format("Failed deleting queue with name \"{0}\", queue is not empty.", name));

                database.QueuesLock.EnterWriteLock();
                try
                {
                    queue.QueuedTasks = null;
                    database.Queues.Remove(queue.Id);
                    database.QueueNames.Remove(queue.Name);
                }
                finally
                {
                    database.QueuesLock.ExitWriteLock();
                }
            }
            finally
            {
                queue.QueueLock.ExitWriteLock();
            }
        }

        public static bool Exists(Database database, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");

            database.QueuesLock.EnterReadLock();
            try
            {
                return database.QueueNames.ContainsKey(name);
            }
            finally
            {
                database.QueuesLock.ExitReadLock();
            }
        }

        public static bool IsEmpty(Database database, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");

            database.QueuesLock.EnterReadLock();
            try
            {
                if (!database.QueueNames.ContainsKey(name))
                    throw new ArgumentException(string.Format("Queue with the name \"{0}\" not found.", name), "name");

                var queue = database.Queues[database.QueueNames[name]];
                queue.QueueLock.EnterReadLock();
                try
                {
                    return !queue.QueuedTasks.Any();
                }
                finally
                {
                    queue.QueueLock.ExitReadLock();
                }
            }
            finally
            {
                database.QueuesLock.ExitReadLock();
            }
        }

        public static QueueTask Peek(Database database, string queueName)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (queueName == null)
                throw new ArgumentNullException("queueName", "queueName is null.");

            Guid workflowId, taskId;
            var queue = Queue.Get(database, queueName);

            queue.QueueLock.EnterReadLock();
            try
            {
                if (queue.QueuedTasks.Count == 0)
                    return null;

                var first = queue.QueuedTasks.First.Value;
                workflowId = first.Item1;
                taskId = first.Item2;
            }
            finally
            {
                queue.QueueLock.ExitReadLock();
            }

            database.WorkflowsLock.EnterReadLock();
            try
            {
                var workflow = database.Workflows[workflowId];
                workflow.WorkflowLock.EnterReadLock();
                try
                {
                    return new QueueTask()
                    {
                        WorkflowName = workflow.Name,
                        TaskName = workflow.Tasks[taskId].Name,
                    };
                }
                finally
                {
                    workflow.WorkflowLock.ExitReadLock();
                }
            }
            finally
            {
                database.WorkflowsLock.ExitReadLock();
            }
        }

        public static QueueTask Dequeue(Database database, string queueName)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (queueName == null)
                throw new ArgumentNullException("queueName", "queueName is null.");

            if (Queue.IsEmpty(database, queueName))
                return null;

            Workflow workflow;
            Task task;
            Queue queue;
            Guid workflowId, taskId;

            queue = Queue.Get(database, queueName);
            queue.QueueLock.EnterReadLock();
            try
            {
                var first = queue.QueuedTasks.First.Value;
                workflowId = first.Item1;
                taskId = first.Item2;
            }
            finally
            {
                queue.QueueLock.ExitReadLock();
            }

            database.WorkflowsLock.EnterReadLock();
            try
            {
                workflow = database.Workflows[workflowId];
                workflow.WorkflowLock.EnterReadLock();
                try
                {
                    task = workflow.Tasks[taskId];
                }
                finally
                {
                    workflow.WorkflowLock.ExitReadLock();
                }
            }
            finally
            {
                database.WorkflowsLock.ExitReadLock();
            }

            workflow.WorkflowLock.EnterWriteLock();
            try
            {
                queue.QueueLock.EnterWriteLock();
                try
                {
                    // Validate state;
                    if (queue.QueuedTasks.First().Item2 != taskId)
                        return null;
                    queue.QueuedTasks.RemoveFirst();
                    queue.RunningTasks.Add(taskId);
                    task.State = TaskState.Running;
                    return new QueueTask()
                    {
                        WorkflowName = workflow.Name,
                        TaskName = task.Name,
                    };
                }
                finally
                {
                    queue.QueueLock.ExitWriteLock();
                }
            }
            finally
            {
                workflow.WorkflowLock.ExitWriteLock();
            }
        }

        public static void Complete(Database database, string workflowName, string taskName)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (workflowName == null)
                throw new ArgumentNullException("workflowName", "workflowName is null.");
            if (taskName == null)
                throw new ArgumentNullException("taskName", "taskName is null.");

            Workflow workflow;
            Task task;
            Queue queue;

            database.WorkflowsLock.EnterUpgradeableReadLock();
            try
            {
                database.QueuesLock.EnterReadLock();
                try
                {
                    workflow = database.Workflows[database.WorkflowNames[workflowName]];
                    workflow.WorkflowLock.EnterWriteLock();
                    try
                    {
                        task = workflow.Tasks[workflow.TaskNames[taskName]];
                        queue = database.Queues[task.QueueId];
                        queue.QueueLock.EnterWriteLock();
                        try
                        {
                            queue.RunningTasks.Remove(task.Id);
                            task.State = TaskState.Completed;
                            workflow.CompletedTasks.Add(task.Id);
                            foreach (var nextTaskId in task.DependencyTo)
                            {
                                var nextTask = workflow.Tasks[nextTaskId];
                                nextTask.OutstandingDependencies.Remove(task.Id);
                                if (nextTask.OutstandingDependencies.Count == 0)
                                {
                                    nextTask.State = TaskState.Queued;
                                    var nextQueue = database.Queues[nextTask.QueueId];
                                    nextQueue.QueueLock.EnterWriteLock();
                                    try
                                    {
                                        nextQueue.QueuedTasks.AddLast(new LinkedListNode<Tuple<Guid, Guid>>(new Tuple<Guid, Guid>(workflow.Id, nextTask.Id)));
                                    }
                                    finally
                                    {
                                        nextQueue.QueueLock.ExitWriteLock();
                                    }
                                }
                            }

                            if (workflow.CompletedTasks.Count == workflow.Tasks.Count)
                            {
                                database.WorkflowsLock.EnterWriteLock();
                                try
                                {
                                    database.WorkflowNames.Remove(workflow.Name);
                                    database.Workflows.Remove(workflow.Id);
                                }
                                finally
                                {
                                    database.WorkflowsLock.ExitWriteLock();
                                }
                            }
                        }
                        finally
                        {
                            queue.QueueLock.ExitWriteLock();
                        }
                    }
                    finally
                    {
                        workflow.WorkflowLock.ExitWriteLock();
                    }
                }
                finally
                {
                    database.QueuesLock.ExitReadLock();
                }
            }
            finally
            {
                database.WorkflowsLock.ExitUpgradeableReadLock();
            }
        }

        internal static Queue Get(Database database, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");

            database.QueuesLock.EnterReadLock();
            try
            {
                if (!database.QueueNames.ContainsKey(name))
                    throw new Exception(string.Format("Queue named \"{0}\" not found.", name));
                return database.Queues[database.QueueNames[name]];
            }
            finally
            {
                database.QueuesLock.ExitReadLock();
            }
        }
    }
}
