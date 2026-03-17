using Shared.Domain.Primitives;

namespace WorkflowService.Domain.Entities;

// StageDefinitionEntity — persisted stage template
// Belongs to WorkflowDefinition
// Separate from WorkflowStage which is a running instance
public sealed class StageDefinitionEntity : BaseEntity<Guid>
{
    public Guid WorkflowDefinitionId { get; private set; }
    public int Order { get; private set; }
    public string StageName { get; private set; } = string.Empty;
    public string RoleRequired { get; private set; } = string.Empty;
    public int SlaDays { get; private set; }

    private StageDefinitionEntity(Guid id) : base(id) { }
    private StageDefinitionEntity() { }

    public static StageDefinitionEntity Create(
        Guid workflowDefinitionId,
        int order,
        string stageName,
        string roleRequired,
        int slaDays)
    {
        return new StageDefinitionEntity(Guid.NewGuid())
        {
            WorkflowDefinitionId = workflowDefinitionId,
            Order                = order,
            StageName            = stageName,
            RoleRequired         = roleRequired,
            SlaDays              = slaDays
        };
    }
}
