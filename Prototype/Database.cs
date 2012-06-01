using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace DynamicWorkflow.Prototype
{
    public sealed class Database
    {
        public Database()
        {
            this.Workflows = new Dictionary<Guid, Workflow>();
            this.Queues = new Dictionary<Guid, Queue>();
            this.QueueNames = new Dictionary<string, Guid>();
            this.WorkflowNames = new Dictionary<string, Guid>();
            this.WorkflowsLock = new ReaderWriterLockSlim();
            this.QueuesLock = new ReaderWriterLockSlim();
        }

        // Databases
        internal readonly Dictionary<Guid, Workflow> Workflows;
        internal readonly Dictionary<Guid, Queue> Queues;
        // Name indexes
        internal readonly Dictionary<string, Guid> QueueNames;
        internal readonly Dictionary<string, Guid> WorkflowNames;
        // Locks
        internal readonly ReaderWriterLockSlim WorkflowsLock;
        internal readonly ReaderWriterLockSlim QueuesLock;

    }
}
