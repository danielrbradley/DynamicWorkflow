using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace DynamicWorkflow.Prototype
{
    public class Workflow
    {
        internal Workflow(string name)
        {
            this.Id = Guid.NewGuid();
            this.Name = name;
            this.Suspended = true;
            this.Tasks = new Dictionary<Guid, Task>();
            this.TaskNames = new Dictionary<string, Guid>();
            this.CompletedTasks = new HashSet<Guid>();
            this.WorkflowLock = new ReaderWriterLockSlim();
        }

        internal readonly Guid Id;
        internal string Name;
        internal bool Suspended;
        internal Dictionary<Guid, Task> Tasks;
        internal Dictionary<string, Guid> TaskNames;
        internal HashSet<Guid> CompletedTasks;
        internal ReaderWriterLockSlim WorkflowLock;

        public static void Create(Database database, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");
            if (Exists(database, name))
                throw new ArgumentException(string.Format("Workflow with the name \"{0}\" already exists.", name), "name");

            var workflow = new Workflow(name);
            database.WorkflowsLock.EnterWriteLock();
            try
            {
                workflow.WorkflowLock.EnterWriteLock();
                try
                {
                    database.WorkflowNames.Add(workflow.Name, workflow.Id);
                    database.Workflows.Add(workflow.Id, workflow);
                }
                finally
                {
                    workflow.WorkflowLock.ExitWriteLock();
                }
            }
            finally
            {
                database.WorkflowsLock.ExitWriteLock();
            }
        }

        public static bool Exists(Database database, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");

            database.WorkflowsLock.EnterReadLock();
            try
            {
                return database.WorkflowNames.ContainsKey(name);
            }
            finally
            {
                database.WorkflowsLock.ExitReadLock();
            }
        }

        public static bool IsSuspended(Database database, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");

            var workflow = Get(database, name);
            workflow.WorkflowLock.EnterReadLock();
            try
            {
                return workflow.Suspended;
            }
            finally
            {
                workflow.WorkflowLock.ExitReadLock();
            }
        }

        internal static Workflow Get(Database database, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");

            database.WorkflowsLock.EnterReadLock();
            try
            {
                if (!database.WorkflowNames.ContainsKey(name))
                    throw new Exception(string.Format("Workflow named \"{0}\" not found.", name));
                return database.Workflows[database.WorkflowNames[name]];
            }
            finally
            {
                database.WorkflowsLock.ExitReadLock();
            }
        }

        internal static Workflow Get(Database database, Guid id)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (id == Guid.Empty)
                throw new ArgumentException("id", "id is empty.");

            database.WorkflowsLock.EnterReadLock();
            try
            {
                if (!database.Workflows.ContainsKey(id))
                    return null;
                return database.Workflows[id];
            }
            finally
            {
                database.WorkflowsLock.ExitReadLock();
            }
        }

        public static void Resume(Database database, string name)
        {
            if (database == null)
                throw new ArgumentNullException("database", "database is null.");
            if (name == null)
                throw new ArgumentNullException("name", "name is null.");

            var workflow = Get(database, name);
            workflow.WorkflowLock.EnterWriteLock();
            try
            {
                if (!workflow.Suspended)
                    return;

                database.QueuesLock.EnterReadLock();
                try
                {
                    var tasksToQueue = workflow.Tasks.Values.Where(task => task.State == TaskState.Queued).ToList();
                    var queues = tasksToQueue.Select(task => task.QueueId).OrderBy(id => id).Distinct().Select(id => database.Queues[id]).ToList();
                    int queueLocksHeld = 0;
                    try
                    {
                        foreach (var queue in queues)
                        {
                            queue.QueueLock.EnterWriteLock();
                            queueLocksHeld++;
                        }

                        foreach (var task in tasksToQueue)
                        {
                            database.Queues[task.QueueId].QueuedTasks.AddLast(new LinkedListNode<Tuple<Guid, Guid>>(new Tuple<Guid, Guid>(workflow.Id, task.Id)));
                        }

                        workflow.Suspended = false;
                    }
                    finally
                    {
                        // Release write locks for any that were taken.
                        for (int i = 0; i < queueLocksHeld; i++)
                        {
                            queues[i].QueueLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    database.QueuesLock.ExitReadLock();
                }
            }
            finally
            {
                workflow.WorkflowLock.ExitWriteLock();
            }
        }

        public static void Suspend(Database database, string name)
        {
            throw new NotImplementedException();
        }
    }
}
