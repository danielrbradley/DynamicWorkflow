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
            this.IsSuspended = true;
            this.Tasks = new Dictionary<Guid, Task>();
            this.TaskNames = new Dictionary<string, Guid>();
            this.CompletedTasks = new HashSet<Guid>();
            this.WorkflowLock = new ReaderWriterLockSlim();
        }

        internal readonly Guid Id;
        internal string Name;
        internal bool IsSuspended;
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
                database.WorkflowNames.Add(workflow.Name, workflow.Id);
                database.Workflows.Add(workflow.Id, workflow);
            }
            finally
            {
                database.WorkflowsLock.ExitReadLock();
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

        public static void Resume(Database database, string name)
        {
            throw new NotImplementedException();
        }

        public static void Suspend(Database database, string name)
        {
            throw new NotImplementedException();
        }
    }
}
