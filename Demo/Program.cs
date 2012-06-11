using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DynamicWorkflow.Prototype;
using System.Diagnostics;

namespace DynamicWorkflow.Demo
{
    class Program
    {
        private static IList<string> queueNames;
        private static int WorkflowsCreated = 0;
        private static int TasksCompleted = 0;
        const int QueueProcessors = 10;
        const int QueueCount = 5;
        const int WorkflowTaskCount = 5;
        const int WorkflowCreators = 6;
        private static TimeSpan QueueCheckFrequency = TimeSpan.FromMilliseconds(1);
        private static TimeSpan QueueCheckFrequencyVariance = TimeSpan.FromMilliseconds(1);
        private static TimeSpan TaskCompletionTime = TimeSpan.FromMilliseconds(1);
        private static TimeSpan TaskCompletionTimeVariance = TimeSpan.FromMilliseconds(1);
        private static TimeSpan WorkflowCreationFrequency = TimeSpan.FromMilliseconds(30);
        private static TimeSpan WorkflowCreationFrequencyVariance = TimeSpan.FromMilliseconds(1);
        private static Database database;
        private static bool stopping = false;

        static void Main(string[] args)
        {
            queueNames = Enumerable.Range(1, QueueCount).Select(i => string.Format("Queue {0}", i)).ToList();
            database = new Database();

            foreach (var name in queueNames)
            {
                Queue.Create(database, name);
            }

            for (int i = 0; i < QueueProcessors; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(QueueProcessor), queueNames[i % QueueCount]);
            }

            for (int i = 0; i < WorkflowCreators; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(TaskCreator));
            }

            while (!stopping)
            {
                PrintOverview();

                if (Console.KeyAvailable)
                {
                    var keyPress = Console.ReadKey(true);
                    switch (keyPress.Key)
                    {
                        case ConsoleKey.DownArrow:
                            // Decrease process time.
                            if (TaskCompletionTime.TotalMilliseconds >= 1)
                                TaskCompletionTime = TaskCompletionTime.Add(TimeSpan.FromMilliseconds(-1));
                            break;
                        case ConsoleKey.UpArrow:
                            // Increase process time.
                            TaskCompletionTime = TaskCompletionTime.Add(TimeSpan.FromMilliseconds(1));
                            break;
                        case ConsoleKey.LeftArrow:
                            // Decrease rate of queuing.
                            WorkflowCreationFrequency = WorkflowCreationFrequency.Add(TimeSpan.FromMilliseconds(1));
                            break;
                        case ConsoleKey.RightArrow:
                            // Increase rate of queuing.
                            if (WorkflowCreationFrequency.TotalMilliseconds >= 1)
                                WorkflowCreationFrequency = WorkflowCreationFrequency.Add(TimeSpan.FromMilliseconds(-1));
                            break;
                        case ConsoleKey.OemComma:
                            // < - Decrease check frequency.
                            QueueCheckFrequency = QueueCheckFrequency.Add(TimeSpan.FromMilliseconds(1));
                            break;
                        case ConsoleKey.OemPeriod:
                            // > - Increase check frequency.
                            if (QueueCheckFrequency.TotalMilliseconds >= 1)
                                QueueCheckFrequency = QueueCheckFrequency.Add(TimeSpan.FromMilliseconds(-1));
                            break;
                        case ConsoleKey.X:
                            stopping = true;
                            break;
                        default:
                            break;
                    }
                }

                Thread.Sleep(200);
            }
        }

        private static void PrintOverview()
        {
            Console.Clear();
            Console.WriteLine("{0} Workflows created, {1} of {2} tasks processed", WorkflowsCreated, TasksCompleted, WorkflowsCreated * WorkflowTaskCount);
            foreach (var queueName in queueNames)
            {
                Console.WriteLine("{0}: running: {1}; queued: {2}", queueName, Queue.RunningCount(database, queueName), Queue.QueuedCount(database, queueName));
            }
            Console.WriteLine();
            Console.WriteLine("Press x to exit.");
            Console.WriteLine();
            Console.WriteLine("Process time: {0} \tDown = decrease \tUp = Increase", TaskCompletionTime);
            Console.WriteLine("Queuing interval: {0} \tLeft = Decrease rate \tRight = Increase rate", WorkflowCreationFrequency);
            Console.WriteLine("Queue poll interval: {0} \t< = Decrease check frequency \t> = Increase check frequency", QueueCheckFrequency);
        }

        private static void TaskCreator(object o)
        {
            var rand = new Random();
            var timer = new Stopwatch();

            while (!stopping)
            {
                var checkFrequency = WorkflowCreationFrequency.Add(TimeSpan.FromMilliseconds(((((double)rand.Next(1000) * 2) - 1000) / 1000) * WorkflowCreationFrequencyVariance.Milliseconds));
                if (checkFrequency > timer.Elapsed)
                    Thread.Sleep(checkFrequency - timer.Elapsed);

                timer.Restart();

                var workflowName = string.Format("Workflow {0}", Interlocked.Increment(ref WorkflowsCreated));
                Workflow.Create(database, workflowName);
                for (int i = 0; i < WorkflowTaskCount; i++)
                {
                    Task.Create(database, workflowName, string.Format("Task {0}", i + 1), queueNames[i % QueueCount]);
                }

                var order = Enumerable.Range(1, WorkflowTaskCount).OrderBy(i => rand.Next()).ToList();

                for (int i = 0; i < WorkflowTaskCount - 1; i++)
                {
                    Task.AddDependency(database, workflowName, string.Format("Task {0}", order[i]), string.Format("Task {0}", order[i + 1]));
                }

                Workflow.Resume(database, workflowName);

                timer.Stop();
            }
        }

        private static void QueueProcessor(object name)
        {
            var queueName = (string)name;
            var rand = new Random();

            while (!stopping)
            {
                var queueTask = Queue.Dequeue(database, queueName);
                if (queueTask != null)
                {
                    var completionTime = TaskCompletionTime.Add(TimeSpan.FromMilliseconds(((((double)rand.Next(1000) * 2) - 1000) / 1000) * TaskCompletionTimeVariance.Milliseconds));
                    if (completionTime > TimeSpan.Zero)
                        Thread.Sleep(completionTime);
                    Queue.Complete(database, queueTask.WorkflowName, queueTask.TaskName);
                    Interlocked.Increment(ref TasksCompleted);
                }
                else
                {
                    var checkFrequency = QueueCheckFrequency.Add(TimeSpan.FromMilliseconds(((((double)rand.Next(1000) * 2) - 1000) / 1000) * QueueCheckFrequencyVariance.Milliseconds));
                    if (checkFrequency > TimeSpan.Zero)
                        Thread.Sleep(checkFrequency);
                }
            }
        }
    }
}
