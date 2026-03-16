using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkflowService.Domain.Enums;
using WorkflowService.Domain.StateMachines;
using WorkflowService.Infrastructure.Persistence;

namespace WorkflowService.Infrastructure.Jobs;

// SlaCheckerJob — runs every 5 minutes via Hangfire
//
// Checks all InProgress workflow stages
// If SLA deadline has passed ? escalates automatically
//
// Real world example:
// Manager has 2 days to approve document
// Day 3 arrives — this job fires — stage escalated
// Email sent to manager's supervisor
// Workflow marked as Escalated
public sealed class SlaCheckerJob
{
    private readonly WorkflowDbContext _context;
    private readonly ILogger<SlaCheckerJob> _logger;

    public SlaCheckerJob(
        WorkflowDbContext context,
        ILogger<SlaCheckerJob> logger)
    {
        _context = context;
        _logger  = logger;
    }

    // Called by Hangfire on schedule
    public async Task ExecuteAsync()
    {
        _logger.LogInformation(
            "SLA checker job started at {Time}",
            DateTime.UtcNow);

        // Find all InProgress workflows
        var overdueInstances = await _context.WorkflowInstances
            .Include(w => w.Stages)
            .Where(w => w.Status == WorkflowStatus.InProgress)
            .ToListAsync();

        var escalatedCount = 0;

        foreach (var instance in overdueInstances)
        {
            var currentStage = instance.GetCurrentStage();

            // Check if current stage SLA is breached
            if (currentStage is null || !currentStage.IsOverdue())
                continue;

            _logger.LogWarning(
                "SLA breached. WorkflowId: {WorkflowId} " +
                "Stage: {StageName} Deadline: {Deadline}",
                instance.Id,
                currentStage.StageName,
                currentStage.SlaDeadline);

            try
            {
                // Use state machine to escalate safely
                var stateMachine = new WorkflowStateMachine(instance);

                if (stateMachine.CanFire(WorkflowTrigger.Escalate))
                {
                    stateMachine.Escalate();
                    _context.WorkflowInstances.Update(instance);
                    escalatedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to escalate workflow. " +
                    "WorkflowId: {WorkflowId}", instance.Id);
            }
        }

        if (escalatedCount > 0)
            await _context.SaveChangesAsync();

        _logger.LogInformation(
            "SLA checker completed. Escalated: {Count}",
            escalatedCount);
    }
}
