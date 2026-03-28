import {
  Component,
  OnInit,
  signal,
  computed,
  inject,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatDividerModule } from "@angular/material/divider";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatDialogModule, MatDialog } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { WorkflowService } from "../services/workflow.service";
import { AuthService } from "../../../core/services/auth.service";
import {
  WorkflowInstance,
  WorkflowStage,
  WorkflowStatusColors,
  StageStatusColors,
  WorkflowStatus,
  StageStatus,
} from "../models/workflow.models";

@Component({
  selector: "app-workflow-detail",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    MatTooltipModule,
  ],
  template: `
    <div class="page-container">

      <button mat-button routerLink="/workflow" class="back-btn">
        <mat-icon>arrow_back</mat-icon>
        Back to Workflows
      </button>

      @if (isLoading()) {
        <div class="loading-container">
          <mat-spinner diameter="40"></mat-spinner>
        </div>
      }

      @if (!isLoading() && workflow()) {
        <div class="detail-layout">

          <!-- Header Card -->
          <mat-card class="header-card">
            <mat-card-content>
              <div class="wf-header">
                <div>
                  <h1>{{ workflow()!.documentTitle }}</h1>
                  <p class="document-name">
                    Uploaded Document: {{ workflow()!.documentTitle }}
                  </p>
                  <span class="status-badge"
                    [style.background]="getStatusColor(workflow()!.status) + '20'"
                    [style.color]="getStatusColor(workflow()!.status)">
                    {{ workflow()!.status }}
                  </span>
                </div>
                <div class="header-meta">
                  <p>Started: {{ workflow()!.startedAt | date: "dd MMM yyyy" }}</p>
                  @if (workflow()!.completedAt) {
                    <p>Completed: {{ workflow()!.completedAt | date: "dd MMM yyyy" }}</p>
                  }
                </div>
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Timeline Card -->
          <mat-card class="timeline-card">
            <mat-card-header>
              <h2>Approval Timeline</h2>
            </mat-card-header>
            <mat-divider></mat-divider>
            <mat-card-content>
              <div class="timeline">
                @for (stage of workflow()!.stages; track stage.id;
                  let last = $last) {
                  <div class="timeline-item">

                    <!-- Connector line -->
                    @if (!last) {
                      <div class="timeline-line"
                        [style.background]="stage.status === 'Approved'
                          ? '#16A34A' : '#E2E8F0'">
                      </div>
                    }

                    <!-- Stage dot -->
                    <div class="timeline-dot"
                      [style.background]="getStageStatusColor(stage.status)"
                      [style.border]="stage.stageOrder === workflow()!.currentStageOrder
                        ? '3px solid #2563EB' : 'none'">
                      @if (stage.status === "Approved") {
                        <mat-icon>check</mat-icon>
                      } @else if (stage.status === "Rejected") {
                        <mat-icon>close</mat-icon>
                      } @else if (stage.status === "InProgress") {
                        <mat-icon>hourglass_empty</mat-icon>
                      } @else {
                        <mat-icon>radio_button_unchecked</mat-icon>
                      }
                    </div>

                    <!-- Stage info -->
                    <div class="timeline-content">
                      <div class="stage-header">
                        <h3>{{ stage.stageName }}</h3>
                        <span class="stage-status"
                          [style.color]="getStageStatusColor(stage.status)">
                          {{ stage.status }}
                        </span>
                      </div>
                      <p class="stage-assignee">
                        <mat-icon>person</mat-icon>
                        {{ stage.assignedToEmail }}
                      </p>
                      <p class="stage-deadline"
                        [class.overdue]="wfService.isOverdue(stage.slaDeadline)
                          && stage.status === 'InProgress'">
                        <mat-icon>schedule</mat-icon>
                        Deadline: {{ stage.slaDeadline | date: "dd MMM yyyy" }}
                        @if (wfService.isOverdue(stage.slaDeadline)
                          && stage.status === "InProgress") {
                          <span class="overdue-badge">OVERDUE</span>
                        }
                      </p>
                      @if (stage.comments) {
                        <p class="stage-comments">
                          <mat-icon>comment</mat-icon>
                          {{ stage.comments }}
                        </p>
                      }
                      @if (stage.processedAt) {
                        <p class="stage-processed">
                          Processed: {{ stage.processedAt | date: "dd MMM yyyy HH:mm" }}
                        </p>
                      }
                    </div>

                  </div>
                }
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Action Card ï¿½ only shown if current user can act -->
          @if (canApprove()) {
            <mat-card class="action-card">
              <mat-card-header>
                <h2>Your Decision</h2>
                <p>Stage: {{ getCurrentStage()?.stageName }}</p>
              </mat-card-header>
              <mat-divider></mat-divider>
              <mat-card-content>
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Comments (optional)</mat-label>
                  <textarea matInput
                    [(ngModel)]="comments"
                    rows="3"
                    placeholder="Add your review comments...">
                  </textarea>
                </mat-form-field>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary"
                  [disabled]="isActing()"
                  (click)="onApprove()">
                  <mat-icon>check_circle</mat-icon>
                  Approve
                </button>
                <button mat-stroked-button color="warn"
                  [disabled]="isActing()"
                  (click)="onReject()">
                  <mat-icon>cancel</mat-icon>
                  Reject
                </button>
              </mat-card-actions>
            </mat-card>
          }

        </div>
      }

    </div>
  `,
  styles: [`
    .page-container {
      padding: 24px;
      max-width: 900px;
      margin: 0 auto;
    }
    .back-btn {
      display: flex;
      align-items: center;
      gap: 4px;
      color: #64748B;
      margin-bottom: 16px;
    }
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 60px;
    }
    .detail-layout {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }
    .header-card, .timeline-card, .action-card {
      border-radius: 12px;
    }
    .wf-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      flex-wrap: wrap;
      gap: 16px;
      padding: 8px 0;
    }
    .wf-header h1 {
      font-size: 22px;
      font-weight: 700;
      color: #1E293B;
      margin: 0 0 8px;
    }
    .document-name {
      margin: 0 0 8px;
      font-size: 13px;
      color: #475569;
      font-weight: 500;
    }
    .status-badge {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 20px;
      font-size: 13px;
      font-weight: 600;
    }
    .header-meta p {
      margin: 0 0 4px;
      font-size: 13px;
      color: #64748B;
    }
    .timeline-card h2 {
      font-size: 16px;
      font-weight: 600;
      color: #1E293B;
      margin: 0;
      padding: 16px;
    }
    .timeline {
      padding: 16px 8px;
      position: relative;
    }
    .timeline-item {
      display: flex;
      gap: 16px;
      padding-bottom: 24px;
      position: relative;
    }
    .timeline-line {
      position: absolute;
      left: 19px;
      top: 40px;
      width: 2px;
      height: calc(100% - 16px);
    }
    .timeline-dot {
      width: 40px;
      height: 40px;
      min-width: 40px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1;
    }
    .timeline-dot mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
      color: white;
    }
    .timeline-content {
      flex: 1;
      padding-top: 6px;
    }
    .stage-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 6px;
    }
    .stage-header h3 {
      margin: 0;
      font-size: 15px;
      font-weight: 600;
      color: #1E293B;
    }
    .stage-status {
      font-size: 12px;
      font-weight: 600;
    }
    .stage-assignee, .stage-deadline, .stage-comments {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 13px;
      color: #64748B;
      margin: 0 0 4px;
    }
    .stage-assignee mat-icon,
    .stage-deadline mat-icon,
    .stage-comments mat-icon {
      font-size: 14px;
      width: 14px;
      height: 14px;
    }
    .stage-deadline.overdue {
      color: #DC2626;
    }
    .overdue-badge {
      background: #DC2626;
      color: white;
      padding: 1px 6px;
      border-radius: 4px;
      font-size: 10px;
      font-weight: 700;
    }
    .stage-processed {
      font-size: 11px;
      color: #94A3B8;
      margin: 2px 0 0;
    }
    .stage-comments {
      font-style: italic;
    }
    .action-card h2 {
      font-size: 16px;
      font-weight: 600;
      color: #1E293B;
      margin: 0 0 4px;
      padding: 16px 16px 0;
    }
    .action-card p {
      font-size: 13px;
      color: #64748B;
      margin: 0;
      padding: 0 16px 16px;
    }
    .full-width { width: 100%; }
    mat-card-actions {
      display: flex;
      gap: 12px;
      padding: 8px 16px 16px;
    }
    mat-card-actions button {
      display: flex;
      align-items: center;
      gap: 6px;
    }
  `],
})
export class WorkflowDetailComponent implements OnInit {
  wfService   = inject(WorkflowService);
  authService = inject(AuthService);

  private route    = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);

  workflow  = signal<WorkflowInstance | null>(null);
  isLoading = signal(true);
  isActing  = signal(false);
  comments  = "";

  currentStage = computed(() => {
    const wf = this.workflow();
    if (!wf) return undefined;
    return wf.stages.find(s => s.stageOrder === wf.currentStageOrder);
  });



  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (!id) { this.isLoading.set(false); return; }
    this.loadWorkflow(id);
  }

  private loadWorkflow(id: string): void {
    this.wfService.getWorkflow(id).subscribe({
      next:  (wf) => { this.workflow.set(wf); this.isLoading.set(false); },
      error: ()   => this.isLoading.set(false),
    });
  }

  canApprove(): boolean {
    const wf = this.workflow();
    if (!wf || wf.status !== "InProgress") return false;
    const userId = this.authService.getUserId();
    const stage  = this.getCurrentStage();
    return stage?.assignedToUserId === userId;
  }

  getCurrentStage(): WorkflowStage | undefined {
    const wf = this.workflow();
    if (!wf) return undefined;
    return wf.stages.find(s => s.stageOrder === wf.currentStageOrder);
  }

  onApprove(): void {
    const wf = this.workflow();
    if (!wf) return;
    this.isActing.set(true);

    this.wfService.approveStage(wf.id, this.comments).subscribe({
      next: (updated) => {
        this.workflow.set(updated);
        this.isActing.set(false);
        this.comments = "";
        this.snackBar.open("Stage approved successfully", "Close",
          { duration: 3000 });
      },
      error: () => {
        this.isActing.set(false);
        this.snackBar.open("Failed to approve stage", "Close",
          { duration: 3000 });
      },
    });
  }

  onReject(): void {
    const wf = this.workflow();
    if (!wf) return;
    this.isActing.set(true);

    this.wfService.rejectStage(wf.id, this.comments).subscribe({
      next: (updated) => {
        this.workflow.set(updated);
        this.isActing.set(false);
        this.comments = "";
        this.snackBar.open("Stage rejected", "Close",
          { duration: 3000 });
      },
      error: () => {
        this.isActing.set(false);
        this.snackBar.open("Failed to reject stage", "Close",
          { duration: 3000 });
      },
    });
  }

  getStatusColor(status: string): string {
    return WorkflowStatusColors[status as WorkflowStatus] ?? "#64748B";
  }

  getStageStatusColor(status: string): string {
    return StageStatusColors[status as StageStatus] ?? "#94A3B8";
  }
}

