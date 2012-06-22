# Project Aim
Provide a highly efficient database for managing complex, distributed task
workflows. This is taking the concept of a system bus through message queueing
and extending that to also model the concept of one action initiating 
subsequent actions.

# High level goals
1. Minimise global locking/use lock-free implementations where possible.
2. Ensure transactionability of all actions to guarentee data integrity.
3. Workflows can be manipulated while running.
4. The structure of workflows is dynamic

#Example Workflow
            ┌──> Task B ─┐
    Task A ─┤            ├──> Task D
            └──> Task C ─┘

##Steps
1. Task A is queued to run
2. Task A is completed which queues Task B and Task C
3. Task B and Task C are run in parallel
4. When the last of Task B and Task C is completed, Task D is queued
5. Task D is completed and completes the workflow
