using Shared.Domain.Primitives;

namespace WorkflowService.Domain.Entities;

public sealed class WorkflowDefinition : BaseEntity<Guid>
{
    private readonly List<StageDefinitionEntity> _stages = new();

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<StageDefinitionEntity> Stages
        => _stages.AsReadOnly();

    private WorkflowDefinition(Guid id) : base(id) { }
    private WorkflowDefinition() { }

    public static WorkflowDefinition Create(
        Guid tenantId,
        string name,
        string description)
    {
        return new WorkflowDefinition(Guid.NewGuid())
        {
            TenantId    = tenantId,
            Name        = name,
            Description = description,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow
        };
    }

    public void AddStage(
        int order,
        string stageName,
        string roleRequired,
        int slaDays)
    {
        _stages.Add(StageDefinitionEntity.Create(
            Id, order, stageName, roleRequired, slaDays));
    }

    public void Deactivate() => IsActive = false;
}
