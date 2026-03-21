import { Component, inject, OnInit, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatDividerModule } from "@angular/material/divider";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { HttpClient } from "@angular/common/http";
import { AuthService } from "../../core/services/auth.service";
import { environment } from "../../../environments/environment";

interface StatCard {
  title:    string;
  value:    string | number;
  subtitle: string;
  icon:     string;
  color:    string;
  route:    string;
  roles:    string[];
}

@Component({
  selector: "app-dashboard",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <div class="dashboard">

      <!-- Welcome Header -->
      <div class="page-header">
        <div>
          <h1>
            Good {{ getTimeOfDay() }},
            {{ authService.currentUser()?.fullName }} ??
          </h1>
          <p>Here is your workspace overview</p>
        </div>
        <div class="header-badges">
          <span class="tenant-badge">
            <mat-icon>business</mat-icon>
            {{ getTenantDomain() }}
          </span>
          <span class="role-badge"
            [class]="'role-' + authService.currentUser()?.role?.toLowerCase()">
            {{ authService.currentUser()?.role }}
          </span>
        </div>
      </div>

      <!-- Stats Grid -->
      <div class="stats-grid">
        @for (card of getVisibleCards(); track card.title) {
          <mat-card class="stat-card"
            [routerLink]="card.route">
            <div class="stat-top">
              <div class="stat-icon-wrap"
                [style.background]="card.color + '15'">
                <mat-icon [style.color]="card.color">
                  {{ card.icon }}
                </mat-icon>
              </div>
              <span class="stat-value"
                [style.color]="card.color">
                {{ card.value }}
              </span>
            </div>
            <mat-card-content>
              <h3>{{ card.title }}</h3>
              <p>{{ card.subtitle }}</p>
            </mat-card-content>
            <mat-card-actions>
              <button mat-button [style.color]="card.color">
                Go to {{ card.title }}
                <mat-icon>arrow_forward</mat-icon>
              </button>
            </mat-card-actions>
          </mat-card>
        }
      </div>

      <!-- Quick Actions — Admin + Manager only -->
      @if (authService.isManager()) {
        <div class="quick-actions">
          <h2>Quick Actions</h2>
          <div class="action-grid">
            <button mat-raised-button color="primary"
              routerLink="/documents/upload">
              <mat-icon>upload_file</mat-icon>
              Upload Document
            </button>
            <button mat-stroked-button color="primary"
              routerLink="/workflow">
              <mat-icon>account_tree</mat-icon>
              View Approvals
            </button>
            <button mat-stroked-button
              routerLink="/notifications">
              <mat-icon>notifications</mat-icon>
              Notifications
            </button>
          </div>
        </div>
      }

      <!-- Viewer Message -->
      @if (!authService.isManager()) {
        <div class="viewer-info">
          <mat-icon>info_outline</mat-icon>
          <p>
            You have read-only access.
            Contact your admin to request additional permissions.
          </p>
        </div>
      }

    </div>
  `,
  styles: [`
    .dashboard {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 32px;
      flex-wrap: wrap;
      gap: 16px;
    }
    .page-header h1 {
      font-size: 26px;
      font-weight: 700;
      color: #1E293B;
      margin: 0 0 4px;
    }
    .page-header p {
      color: #64748B;
      margin: 0;
      font-size: 14px;
    }
    .header-badges {
      display: flex;
      gap: 8px;
      align-items: center;
      flex-wrap: wrap;
    }
    .tenant-badge {
      display: flex;
      align-items: center;
      gap: 4px;
      background: #EFF6FF;
      color: #2563EB;
      padding: 4px 10px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
    }
    .tenant-badge mat-icon {
      font-size: 14px;
      width: 14px;
      height: 14px;
    }
    .role-badge {
      padding: 4px 10px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
    }
    .role-admin   { background: #FEF3C7; color: #D97706; }
    .role-manager { background: #F0FDF4; color: #16A34A; }
    .role-viewer  { background: #F1F5F9; color: #64748B; }
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 16px;
      margin-bottom: 32px;
    }
    .stat-card {
      border-radius: 12px;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
    }
    .stat-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 25px rgba(0,0,0,0.1);
    }
    .stat-top {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 16px 0;
    }
    .stat-icon-wrap {
      width: 44px;
      height: 44px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .stat-icon-wrap mat-icon {
      font-size: 22px;
      width: 22px;
      height: 22px;
    }
    .stat-value {
      font-size: 28px;
      font-weight: 700;
    }
    mat-card-content h3 {
      font-size: 15px;
      font-weight: 600;
      color: #1E293B;
      margin: 12px 0 4px;
    }
    mat-card-content p {
      font-size: 12px;
      color: #64748B;
      margin: 0;
    }
    mat-card-actions {
      padding: 8px;
    }
    mat-card-actions button {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 12px;
    }
    .quick-actions {
      margin-bottom: 24px;
    }
    .quick-actions h2 {
      font-size: 18px;
      font-weight: 600;
      color: #1E293B;
      margin: 0 0 16px;
    }
    .action-grid {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
    }
    .action-grid button {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .viewer-info {
      display: flex;
      align-items: center;
      gap: 12px;
      background: #F1F5F9;
      border-radius: 10px;
      padding: 16px;
      color: #64748B;
    }
    .viewer-info p { margin: 0; font-size: 14px; }
  `],
})
export class DashboardComponent implements OnInit {
  authService = inject(AuthService);
  private http = inject(HttpClient);

  docCount      = signal(0);
  workflowCount = signal(0);
  unreadCount   = signal(0);

  ngOnInit(): void {
    this.loadCounts();
  }

  private loadCounts(): void {
    this.http.get<any>(
      `${environment.documentUrl}/api/documents?page=1&pageSize=1`)
      .subscribe({
        next: (res) => this.docCount.set(res.totalCount ?? 0),
        error: () => this.docCount.set(0),
      });

    this.http.get<any>(
      `${environment.workflowUrl}/api/workflow/my`)
      .subscribe({
        next: (res) => {
          const list = Array.isArray(res) ? res : [];
          this.workflowCount.set(list.length);
        },
        error: () => this.workflowCount.set(0),
      });

    this.http.get<any>(
      `${environment.notificationUrl}/api/notifications/unread-count`)
      .subscribe({
        next: (res) => this.unreadCount.set(res.unreadCount ?? 0),
        error: () => this.unreadCount.set(0),
      });
  }

  getVisibleCards(): StatCard[] {
    const role = this.authService.currentUser()?.role ?? "";
    return this.allCards.filter(c => c.roles.includes(role));
  }

  private allCards: StatCard[] = [
    {
      title:    "Documents",
      value:    this.docCount(),
      subtitle: "Total documents in your workspace",
      icon:     "description",
      color:    "#2563EB",
      route:    "/documents",
      roles:    ["Admin", "Manager", "Viewer"],
    },
    {
      title:    "Workflows",
      value:    this.workflowCount(),
      subtitle: "Active approval chains",
      icon:     "account_tree",
      color:    "#7C3AED",
      route:    "/workflow",
      roles:    ["Admin", "Manager"],
    },
    {
      title:    "Unread",
      value:    this.unreadCount(),
      subtitle: "Notifications awaiting your attention",
      icon:     "notifications",
      color:    "#EA580C",
      route:    "/notifications",
      roles:    ["Admin", "Manager", "Viewer"],
    },
    {
      title:    "Upload",
      value:    "+",
      subtitle: "Add new documents to workspace",
      icon:     "upload_file",
      color:    "#16A34A",
      route:    "/documents/upload",
      roles:    ["Admin", "Manager"],
    },
  ];

  getTimeOfDay(): string {
    const hour = new Date().getHours();
    if (hour < 12) return "morning";
    if (hour < 17) return "afternoon";
    return "evening";
  }

  getTenantDomain(): string {
    const email = this.authService.currentUser()?.email ?? "";
    const parts = email.split("@");
    return parts.length > 1 ? parts[1] : "";
  }
}
