using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace DynamicWorkflow.Prototype
{
    public class Task
    {
        internal Task(string name, Guid queueId)
        {
            this.Id = Guid.NewGuid();
            this.QueueId = queueId;
            this.Name = name;
            this.State = TaskState.Queued;
            this.OutstandingDependencies = new HashSet<Guid>();
            this.DependantOn = new HashSet<Guid>();
            this.DependencyTo = new HashSet<Guid>();
        }

        internal readonly Guid Id;
        internal readonly Guid QueueId;
        internal TaskState State;
        internal readonly string Name;
        internal HashSet<Guid> OutstandingDependencies;
        internal HashSet<Guid> DependantOn;
        internal HashSet<Guid> DependencyTo;

        public static void Create(Database database, string workflowName, string name, string queueName)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (workflowName == null)
                throw new ArgumentNullException("workflowName", "workflowName is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");
            if (queueName == null)
                throw new ArgumentNullException("queueName", "queueName is null.");

            Task task;
            var workflow = Workflow.Get(database, workflowName);
            var queue = Queue.Get(database, queueName);
            queue.QueueLock.EnterReadLock();
            try
            {
                task = new Task(name, queue.Id);
            }
            finally
            {
                queue.QueueLock.ExitReadLock();
            }

            database.WorkflowsLock.EnterReadLock();
            try
            {
                workflow = database.Workflows[database.WorkflowNames[workflowName]];
                if (workflow.Suspended)
                {
                    workflow.WorkflowLock.EnterUpgradeableReadLock();
                    try
                    {
                        workflow.Tasks.Add(task.Id, task);
                        workflow.TaskNames.Add(task.Name, task.Id);
                    }
                    finally
                    {
                        workflow.WorkflowLock.ExitUpgradeableReadLock();
                    }
                }
                else
                {
                    database.QueuesLock.EnterReadLock();
                    try
                    {
                        workflow.WorkflowLock.EnterUpgradeableReadLock();
                        try
                        {
                            queue.QueueLock.EnterWriteLock();
                            try
                            {
                                workflow.Tasks.Add(task.Id, task);
                                workflow.TaskNames.Add(task.Name, task.Id);
                                queue.QueuedTasks.AddLast(new LinkedListNode<Tuple<Guid, Guid>>(new Tuple<Guid, Guid>(workflow.Id, task.Id)));
                            }
                            finally
                            {
                                queue.QueueLock.ExitWriteLock();
                            }
                        }
                        finally
                        {
                            workflow.WorkflowLock.ExitUpgradeableReadLock();
                        }
                    }
                    finally
                    {
                        database.QueuesLock.ExitReadLock();
                    }
                }
            }
            finally
            {
                database.WorkflowsLock.ExitReadLock();
            }
        }

        internal static Task Get(Database database, string workflowName, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (workflowName == null)
                throw new ArgumentNullException("workflowName", "workflowName is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");

            Workflow workflow = Workflow.Get(database, workflowName);
            workflow.WorkflowLock.EnterReadLock();
            try
            {
                if (!workflow.TaskNames.ContainsKey(name))
                    throw new Exception(string.Format("Task named \"{0}\" not found in the workflow named \"{1}\".", name, workflowName));
                return workflow.Tasks[workflow.TaskNames[name]];
            }
            finally
            {
                workflow.WorkflowLock.ExitReadLock();
            }
        }

        public static void AddDependency(Database database, string workflowName, string nameDependantOn, string nameDependancyTo)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (workflowName == null)
                throw new ArgumentNullException("workflowName", "workflowName is null.");
            if (nameDependantOn == null)
                throw new ArgumentNullException("nameDependantOn", "nameDependantOn is null.");
            if (workflowName == null)
                throw new ArgumentNullException("nameDependancyTo", "nameDependancyTo is null.");

            var workflow = Workflow.Get(database, workflowName);
            var dependantOn = Task.Get(database, workflowName, nameDependantOn);
            var dependancyTo = Task.Get(database, workflowName, nameDependancyTo);
            var dependancyToQueue = Queue.Get(database, dependancyTo.QueueId);

            workflow.WorkflowLock.EnterWriteLock();
            try
            {
                switch (dependancyTo.State)
                {
                    case TaskState.AwaitDependence:
                        break;
                    case TaskState.Queued:
                        if (!workflow.Suspended)
                        {
                            dependancyToQueue.QueueLock.EnterWriteLock();
                            try
                            {
                                dependancyToQueue.QueuedTasks.Remove(dependancyToQueue.QueuedTasks.First(t => t.Item2 == dependancyTo.Id));
                            }
                            finally
                            {
                                dependancyToQueue.QueueLock.ExitWriteLock();
                            }
                        }
                        break;
                    case TaskState.Running:
                    case TaskState.Completed:
                    case TaskState.Failed:
                    default:
                        throw new InvalidOperationException("Can only add dependancy to a task which has not yet been started.");
                }

                dependancyTo.State = TaskState.AwaitDependence;
                dependantOn.DependencyTo.Add(dependancyTo.Id);
                dependancyTo.DependantOn.Add(dependantOn.Id);

                switch (dependantOn.State)
                {
                    case TaskState.AwaitDependence:
                    case TaskState.Queued:
                    case TaskState.Running:
                        dependancyTo.OutstandingDependencies.Add(dependantOn.Id);
                        break;
                }
            }
            finally
            {
                workflow.WorkflowLock.ExitWriteLock();
            }
        }
    }
}
