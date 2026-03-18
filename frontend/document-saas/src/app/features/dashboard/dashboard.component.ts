import { Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { RouterLink } from "@angular/router";
import { AuthService } from "../../core/services/auth.service";

@Component({
  selector: "app-dashboard",
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    RouterLink,
  ],
  template: `
    <div class="dashboard-container">
      <div class="welcome-section">
        <h1>Welcome, {{ authService.currentUser()?.fullName }}</h1>
        <p>{{ authService.currentUser()?.role }} —
           {{ authService.currentUser()?.email }}</p>
      </div>

      <div class="stats-grid">

        <mat-card class="stat-card">
          <mat-icon class="stat-icon documents">description</mat-icon>
          <div class="stat-info">
            <h3>Documents</h3>
            <p>Manage your files</p>
          </div>
          <button mat-stroked-button routerLink="/documents">
            View All
          </button>
        </mat-card>

        <mat-card class="stat-card">
          <mat-icon class="stat-icon workflow">account_tree</mat-icon>
          <div class="stat-info">
            <h3>Workflows</h3>
            <p>Pending approvals</p>
          </div>
          <button mat-stroked-button routerLink="/workflow">
            View All
          </button>
        </mat-card>

        <mat-card class="stat-card">
          <mat-icon class="stat-icon notifications">
            notifications
          </mat-icon>
          <div class="stat-info">
            <h3>Notifications</h3>
            <p>Stay updated</p>
          </div>
          <button mat-stroked-button routerLink="/notifications">
            View All
          </button>
        </mat-card>

      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }
    .welcome-section {
      margin-bottom: 32px;
    }
    .welcome-section h1 {
      font-size: 28px;
      font-weight: 700;
      color: #1E293B;
      margin: 0 0 4px;
    }
    .welcome-section p {
      color: #64748B;
      margin: 0;
    }
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 20px;
    }
    .stat-card {
      padding: 24px;
      border-radius: 12px;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }
    .stat-icon {
      font-size: 40px;
      width: 40px;
      height: 40px;
    }
    .stat-icon.documents    { color: #2563EB; }
    .stat-icon.workflow     { color: #7C3AED; }
    .stat-icon.notifications { color: #EA580C; }
    .stat-info h3 {
      margin: 0 0 4px;
      font-size: 18px;
      font-weight: 600;
    }
    .stat-info p {
      margin: 0;
      color: #64748B;
      font-size: 14px;
    }
  `],
})
export class DashboardComponent {
  authService = inject(AuthService);
}
