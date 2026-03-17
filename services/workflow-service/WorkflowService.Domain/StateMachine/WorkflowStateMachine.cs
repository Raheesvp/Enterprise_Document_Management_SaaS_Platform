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

        // Terminal states — ignore all triggers
        _machine.Configure(WorkflowStatus.Approved)
            .Ignore(WorkflowTrigger.Complete)
            .Ignore(WorkflowTrigger.Approve)
            .Ignore(WorkflowTrigger.Cancel);

        _machine.Configure(WorkflowStatus.Rejected)
            .Ignore(WorkflowTrigger.Reject)
            .Ignore(WorkflowTrigger.Cancel);

        _machine.Configure(WorkflowStatus.Cancelled)
            .Ignore(WorkflowTrigger.Cancel)
            .Ignore(WorkflowTrigger.Reject);
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
        // Fire state machine BEFORE updating instance
        // to ensure state is still InProgress when trigger fires
        _machine.Fire(WorkflowTrigger.Reject);
        _instance.RejectCurrentStage(userId, comments);
    }

    public void Escalate()
    {
        _machine.Fire(WorkflowTrigger.Escalate);
        _instance.EscalateCurrentStage();
    }

    public void Cancel()
    {
        _machine.Fire(WorkflowTrigger.Cancel);
        _instance.Cancel();
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
