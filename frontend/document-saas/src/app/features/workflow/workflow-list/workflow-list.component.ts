import {
  Component,
  OnInit,
  signal,
  inject,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";
import { HttpClient } from "@angular/common/http";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatChipsModule } from "@angular/material/chips";
import { MatTooltipModule } from "@angular/material/tooltip";
import { environment } from "../../../../environments/environment";
import {
  WorkflowInstance,
  WorkflowStatusColors,
  WorkflowStatus,
} from "../models/workflow.models";
import { WorkflowService } from "../services/workflow.service";
import { AuthService } from "../../../core/services/auth.service";

@Component({
  selector: "app-workflow-list",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatTooltipModule,
  ],
  template: `
    <div class="page-container">

      <div class="page-header">
        <div>
          <h1>Workflows</h1>
          <p>Approval chains for your documents</p>
        </div>
      </div>

      @if (isLoading()) {
        <div class="loading-container">
          <mat-spinner diameter="40"></mat-spinner>
        </div>
      }

      @if (!isLoading() && workflows().length === 0) {
        <div class="empty-state">
          <mat-icon class="empty-icon">account_tree</mat-icon>
          <h3>No workflows found</h3>
          <p>Upload a document to start an approval workflow</p>
          <button mat-raised-button color="primary"
            routerLink="/documents/upload">
            <mat-icon>upload_file</mat-icon>
            Upload Document
          </button>
        </div>
      }

      <div class="workflow-grid">
        @for (wf of workflows(); track wf.id) {
          <mat-card class="workflow-card"
            [routerLink]="['/workflow', wf.id]">

            <mat-card-header>
              <div class="wf-header">
                <div class="wf-icon">
                  <mat-icon>description</mat-icon>
                </div>
                <div class="wf-title-section">
                  <h3>{{ wf.documentTitle }}</h3>
                  <span class="status-chip"
                    [style.background]="getStatusColor(wf.status) + '20'"
                    [style.color]="getStatusColor(wf.status)">
                    {{ wf.status }}
                  </span>
                </div>
              </div>
            </mat-card-header>

            <mat-card-content>
              <p class="document-name">
                Uploaded Document: {{ wf.documentTitle }}
              </p>
              <div class="stage-progress">
                @for (stage of wf.stages; track stage.id) {
                  <div class="stage-dot"
                    [style.background]="getStageColor(stage.status)"
                    [matTooltip]="stage.stageName + ': ' + stage.status">
                  </div>
                }
              </div>
              <p class="stage-info">
                Stage {{ wf.currentStageOrder }}
                of {{ wf.stages.length }}
                — {{ getCurrentStageName(wf) }}
              </p>
              <p class="date-info">
                Started: {{ wf.startedAt | date: "dd MMM yyyy" }}
              </p>
            </mat-card-content>

            <mat-card-actions>
              <button mat-button color="primary"
                [routerLink]="['/workflow', wf.id]">
                <mat-icon>visibility</mat-icon>
                View Details
              </button>
              @if (canApprove(wf)) {
                <button mat-raised-button color="primary"
                  [routerLink]="['/workflow', wf.id]">
                  <mat-icon>check_circle</mat-icon>
                  Review
                </button>
              }
            </mat-card-actions>

          </mat-card>
        }
      </div>

    </div>
  `,
  styles: [`
    .page-container {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }
    .page-header {
      margin-bottom: 24px;
    }
    .page-header h1 {
      font-size: 24px;
      font-weight: 700;
      color: #1E293B;
      margin: 0 0 4px;
    }
    .page-header p {
      color: #64748B;
      margin: 0;
      font-size: 14px;
    }
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 60px;
    }
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 60px;
      gap: 12px;
      text-align: center;
    }
    .empty-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #CBD5E1;
    }
    .empty-state h3 {
      font-size: 18px;
      color: #1E293B;
      margin: 0;
    }
    .empty-state p {
      color: #64748B;
      margin: 0;
    }
    .empty-state button {
      margin-top: 8px;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .workflow-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 16px;
    }
    .workflow-card {
      border-radius: 12px;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
    }
    .workflow-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 25px rgba(0,0,0,0.1);
    }
    .wf-header {
      display: flex;
      align-items: center;
      gap: 12px;
      width: 100%;
      padding: 8px 0;
    }
    .wf-icon {
      width: 40px;
      height: 40px;
      min-width: 40px;
      border-radius: 10px;
      background: #EFF6FF;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .wf-icon mat-icon { color: #2563EB; }
    .wf-title-section {
      display: flex;
      flex-direction: column;
      gap: 4px;
      overflow: hidden;
    }
    .wf-title-section h3 {
      margin: 0;
      font-size: 15px;
      font-weight: 600;
      color: #1E293B;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .status-chip {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 600;
    }
    .stage-progress {
      display: flex;
      gap: 6px;
      margin-bottom: 8px;
    }
    .document-name {
      font-size: 13px;
      color: #334155;
      font-weight: 500;
      margin: 0 0 10px;
      word-break: break-word;
    }
    .stage-dot {
      width: 12px;
      height: 12px;
      border-radius: 50%;
    }
    .stage-info {
      font-size: 13px;
      color: #475569;
      margin: 0 0 4px;
    }
    .date-info {
      font-size: 12px;
      color: #94A3B8;
      margin: 0;
    }
    mat-card-actions {
      display: flex;
      gap: 8px;
      padding: 8px 16px 16px;
    }
    mat-card-actions button {
      display: flex;
      align-items: center;
      gap: 4px;
    }
  `],
})
export class WorkflowListComponent implements OnInit {
  private http        = inject(HttpClient);
  private authService = inject(AuthService);

  workflows = signal<WorkflowInstance[]>([]);
  isLoading = signal(true);

  ngOnInit(): void {
    this.loadWorkflows();
  }

  private loadWorkflows(): void {
    this.http
      .get<any>(`${environment.workflowUrl}/api/workflow/my`)
      .subscribe({
        next: (res) => {
          this.workflows.set(
            Array.isArray(res) ? res : res.items ?? []);
          this.isLoading.set(false);
        },
        error: () => {
          this.workflows.set([]);
          this.isLoading.set(false);
        },
      });
  }

  getStatusColor(status: string): string {
    return WorkflowStatusColors[status as WorkflowStatus]
      ?? "#64748B";
  }

  getStageColor(status: string): string {
    const colors: Record<string, string> = {
      Pending:    "#E2E8F0",
      InProgress: "#EA580C",
      Approved:   "#16A34A",
      Rejected:   "#DC2626",
      Escalated:  "#7C3AED",
    };
    return colors[status] ?? "#E2E8F0";
  }

  getCurrentStageName(wf: WorkflowInstance): string {
    const stage = wf.stages.find(
      s => s.stageOrder === wf.currentStageOrder);
    return stage?.stageName ?? "Complete";
  }

  canApprove(wf: WorkflowInstance): boolean {
    if (wf.status !== "InProgress") return false;
    const userId = this.authService.getUserId();
    const stage  = wf.stages.find(
      s => s.stageOrder === wf.currentStageOrder);
    return stage?.assignedToUserId === userId;
  }
}

