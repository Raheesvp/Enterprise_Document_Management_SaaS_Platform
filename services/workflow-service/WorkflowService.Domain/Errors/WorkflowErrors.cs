using Shared.Domain.Common;

namespace WorkflowService.Domain.Errors;

public static class WorkflowErrors
{
    public static class Definition
    {
        public static Error NotFound(Guid id)
            => new("WorkflowDefinition.NotFound",
                $"Workflow definition {id} not found");

        public static Error NoStages
            => new("WorkflowDefinition.NoStages",
                "Workflow definition must have at least one stage");

        public static Error AlreadyExists
            => new("WorkflowDefinition.AlreadyExists",
                "Workflow definition already exists for this tenant");
    }

    public static class Instance
    {
        public static Error NotFound(Guid id)
            => new("WorkflowInstance.NotFound",
                $"Workflow instance {id} not found");

        public static Error AlreadyStarted
            => new("WorkflowInstance.AlreadyStarted",
                "Workflow already started for this document");

        public static Error NotInProgress
            => new("WorkflowInstance.NotInProgress",
                "Workflow is not in progress");

        public static Error InvalidTransition
            => new("WorkflowInstance.InvalidTransition",
                "Invalid workflow state transition");
    }

    public static class Stage
    {
        public static Error NotFound(Guid id)
            => new("WorkflowStage.NotFound",
                $"Stage {id} not found");

        public static Error NotAssignedToUser
            => new("WorkflowStage.NotAssignedToUser",
                "Stage is not assigned to this user");

        public static Error AlreadyProcessed
            => new("WorkflowStage.AlreadyProcessed",
                "Stage has already been processed");
    }
}
