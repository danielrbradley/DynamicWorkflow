using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicWorkflow.Prototype
{
    enum TaskState
    {
        AwaitDependence,
        Queued,
        Running,
        Completed,
        Failed,
    }
}
