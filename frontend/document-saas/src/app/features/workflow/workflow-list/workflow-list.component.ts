import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";

@Component({
  selector: "app-workflow-list",
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="padding:24px">
      <h2>Workflows</h2>
      <p style="color:#64748B">
        Workflow UI coming in Day 32.
      </p>
    </div>
  `,
})
export class WorkflowListComponent {}
