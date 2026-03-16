using Stateless;
using Stateless.Graph;
using WorkflowService.Domain.Entities;
using WorkflowService.Domain.Enums;

namespace WorkflowService.Domain.StateMachines;

public sealed class WorkflowStateMachine
{
    private readonly StateMachine<WorkflowStatus, WorkflowTrigger>
        _machine;

    private readonly WorkflowInstance _instance;

    public WorkflowStateMachine(WorkflowInstance instance)
    {
        _instance = instance;

        _machine = new StateMachine<WorkflowStatus, WorkflowTrigger>(
            stateAccessor: () => instance.Status,
            stateMutator:  s  => SetStatus(instance, s));

        ConfigureTransitions();
    }

    private void ConfigureTransitions()
    {
        _machine.Configure(WorkflowStatus.NotStarted)
            .Permit(WorkflowTrigger.Start,
                WorkflowStatus.InProgress);

        _machine.Configure(WorkflowStatus.InProgress)
            .PermitReentry(WorkflowTrigger.Approve)
            .Permit(WorkflowTrigger.Reject,
                WorkflowStatus.Rejected)
            .Permit(WorkflowTrigger.Escalate,
                WorkflowStatus.Escalated)
            .Permit(WorkflowTrigger.Cancel,
                WorkflowStatus.Cancelled)
            .Permit(WorkflowTrigger.Complete,
                WorkflowStatus.Approved);

        _machine.Configure(WorkflowStatus.Escalated)
            .PermitReentry(WorkflowTrigger.Approve)
            .Permit(WorkflowTrigger.Reject,
                WorkflowStatus.Rejected)
            .Permit(WorkflowTrigger.Cancel,
                WorkflowStatus.Cancelled)
            .Permit(WorkflowTrigger.Complete,
                WorkflowStatus.Approved);

        _machine.Configure(WorkflowStatus.Approved)
            .Ignore(WorkflowTrigger.Complete);

        _machine.Configure(WorkflowStatus.Rejected)
            .Ignore(WorkflowTrigger.Cancel);

        _machine.Configure(WorkflowStatus.Cancelled)
            .Ignore(WorkflowTrigger.Cancel);
    }

    public void Start()
    {
        _machine.Fire(WorkflowTrigger.Start);
        _instance.Start();
    }

    public void Approve(Guid userId, string? comments = null)
    {
        var isComplete = _instance
            .ApproveCurrentStage(userId, comments);

        if (isComplete)
            _machine.Fire(WorkflowTrigger.Complete);
        else
            _machine.Fire(WorkflowTrigger.Approve);
    }

    public void Reject(Guid userId, string? comments = null)
    {
        _instance.RejectCurrentStage(userId, comments);
        _machine.Fire(WorkflowTrigger.Reject);
    }

    public void Escalate()
    {
        _instance.EscalateCurrentStage();
        _machine.Fire(WorkflowTrigger.Escalate);
    }

    public void Cancel()
    {
        _instance.Cancel();
        _machine.Fire(WorkflowTrigger.Cancel);
    }

    public bool CanFire(WorkflowTrigger trigger)
        => _machine.CanFire(trigger);

    public IEnumerable<WorkflowTrigger> PermittedTriggers
        => _machine.PermittedTriggers;

    public string ToDotGraph()
        => UmlDotGraph.Format(_machine.GetInfo());

    private static void SetStatus(
        WorkflowInstance instance,
        WorkflowStatus status)
    {
        var prop = typeof(WorkflowInstance)
            .GetProperty(nameof(WorkflowInstance.Status));
        prop?.SetValue(instance, status);
    }
}

public enum WorkflowTrigger
{
    Start    = 0,
    Approve  = 1,
    Reject   = 2,
    Escalate = 3,
    Cancel   = 4,
    Complete = 5
}
