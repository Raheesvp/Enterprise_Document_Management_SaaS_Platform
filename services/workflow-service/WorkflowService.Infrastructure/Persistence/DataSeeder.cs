using Microsoft.EntityFrameworkCore;
using WorkflowService.Domain.Entities;
using WorkflowService.Infrastructure.Persistence;

namespace WorkflowService.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(WorkflowDbContext context)
    {
        // Check if we already have definitions
        if (await context.WorkflowDefinitions.AnyAsync())
            return;

        // Use a well-known dev TenantId if possible, but Guid.Empty works as a global fallback
        var tenantId = Guid.Empty; 

        var definitions = new List<WorkflowDefinition>
        {
            CreateDefinition(tenantId, "PDF Review", "Standard multi-stage review for PDF documents", new[]
            {
                ("Initial Review", "Manager", 2),
                ("Legal Proof", "Admin", 5),
                ("Final Approval", "Admin", 2)
            }),
            CreateDefinition(tenantId, "Word Edit", "Editorial workflow for Microsoft Word files", new[]
            {
                ("Editorial Review", "Manager", 3),
                ("Peer Review", "Manager", 2)
            }),
            CreateDefinition(tenantId, "Image Approval", "Quick creative review for images", new[]
            {
                ("Creative Review", "Manager", 1)
            }),
            CreateDefinition(tenantId, "Standard Review", "Default workflow for any document", new[]
            {
                ("Document Review", "Manager", 3)
            })
        };

        await context.WorkflowDefinitions.AddRangeAsync(definitions);
        await context.SaveChangesAsync();
    }

    private static WorkflowDefinition CreateDefinition(
        Guid tenantId, string name, string description, (string Name, string Role, int Sla)[] stages)
    {
        var definition = WorkflowDefinition.Create(tenantId, name, description);
        
        int order = 1;
        foreach (var s in stages)
        {
            definition.AddStage(order++, s.Name, s.Role, s.Sla);
        }

        return definition;
    }
}
