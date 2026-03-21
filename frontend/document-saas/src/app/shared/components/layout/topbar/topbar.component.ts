import {
  Component,
  Output,
  EventEmitter,
  inject,
  OnInit,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatToolbarModule } from "@angular/material/toolbar";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatMenuModule } from "@angular/material/menu";
import { MatBadgeModule } from "@angular/material/badge";
import { MatDividerModule } from "@angular/material/divider";
import { AuthService } from "../../../../core/services/auth.service";
import { NotificationService } from "../../../../features/notifications/services/notification.service";

@Component({
  selector: "app-topbar",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatBadgeModule,
    MatDividerModule,
  ],
  template: `
    <mat-toolbar class="topbar">
      <button mat-icon-button (click)="toggleSidenav.emit()"
        class="menu-btn">
        <mat-icon>menu</mat-icon>
      </button>

      <span class="app-title">Document SaaS</span>
      <span class="spacer"></span>

      <!-- Notification Bell with Badge -->
      <button mat-icon-button routerLink="/notifications"
        class="notif-btn">
        <mat-icon
          [matBadge]="notifService.unreadCount() > 0
            ? notifService.unreadCount() : null"
          matBadgeColor="warn"
          matBadgeSize="small">
          notifications
        </mat-icon>
      </button>

      <button mat-button [matMenuTriggerFor]="userMenu"
        class="user-btn">
        <div class="avatar">{{ getInitials() }}</div>
        <span class="user-name">
          {{ authService.currentUser()?.fullName }}
        </span>
        <mat-icon>arrow_drop_down</mat-icon>
      </button>

      <mat-menu #userMenu="matMenu">
        <div class="user-menu-header">
          <p class="menu-name">
            {{ authService.currentUser()?.fullName }}
          </p>
          <p class="menu-email">
            {{ authService.currentUser()?.email }}
          </p>
          <p class="menu-role">
            {{ authService.currentUser()?.role }}
          </p>
        </div>
        <mat-divider></mat-divider>
        <button mat-menu-item routerLink="/notifications">
          <mat-icon>notifications</mat-icon>
          Notifications
          @if (notifService.unreadCount() > 0) {
            <span class="menu-badge">
              {{ notifService.unreadCount() }}
            </span>
          }
        </button>
        <mat-divider></mat-divider>
        <button mat-menu-item (click)="authService.logout()">
          <mat-icon>logout</mat-icon>
          Sign Out
        </button>
      </mat-menu>
    </mat-toolbar>
  `,
  styles: [`
    .topbar {
      background: white;
      border-bottom: 1px solid #E2E8F0;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      position: sticky;
      top: 0;
      z-index: 100;
      height: 64px;
    }
    .menu-btn { margin-right: 8px; color: #64748B; }
    .app-title {
      font-size: 18px;
      font-weight: 700;
      color: #1E40AF;
    }
    .spacer { flex: 1; }
    .notif-btn { color: #64748B; margin-right: 8px; }
    .user-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      border-radius: 24px;
      padding: 4px 8px;
    }
    .avatar {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background: #2563EB;
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 12px;
      font-weight: 700;
    }
    .user-name {
      font-size: 14px;
      font-weight: 500;
      color: #1E293B;
      max-width: 120px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .user-menu-header { padding: 12px 16px; }
    .menu-name {
      margin: 0 0 2px;
      font-weight: 600;
      font-size: 14px;
      color: #1E293B;
    }
    .menu-email {
      margin: 0 0 2px;
      font-size: 12px;
      color: #64748B;
    }
    .menu-role {
      margin: 0;
      font-size: 11px;
      color: #2563EB;
      font-weight: 600;
      text-transform: uppercase;
    }
    .menu-badge {
      background: #DC2626;
      color: white;
      border-radius: 10px;
      padding: 2px 6px;
      font-size: 11px;
      margin-left: auto;
    }
  `],
})
export class TopbarComponent implements OnInit {
  @Output() toggleSidenav = new EventEmitter<void>();

  authService  = inject(AuthService);
  notifService = inject(NotificationService);

  ngOnInit(): void {
    // Load unread count after short delay
    // ensures token is ready in localStorage
    setTimeout(() => {
      const token = localStorage.getItem("accessToken");
      if (token) {
        this.notifService.getUnreadCount().subscribe();
      }
    }, 500);
  }

  getInitials(): string {
    const name = this.authService.currentUser()?.fullName ?? "";
    return name
      .split(" ")
      .map((n) => n[0])
      .join("")
      .toUpperCase()
      .slice(0, 2);
  }
}
