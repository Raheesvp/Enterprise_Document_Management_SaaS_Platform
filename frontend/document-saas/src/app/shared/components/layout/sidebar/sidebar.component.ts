import { Component, Output, EventEmitter, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Router } from "@angular/router";
import { MatIconModule } from "@angular/material/icon";
import { MatDividerModule } from "@angular/material/divider";
import { MatButtonModule } from "@angular/material/button";
import { MatBadgeModule } from "@angular/material/badge";
import { AuthService } from "../../../../core/services/auth.service";
import { NotificationService } from "../../../../features/notifications/services/notification.service";

interface NavItem {
  label:  string;
  icon:   string;
  route:  string;
  roles:  string[];
  badge?: boolean;
}

@Component({
  selector: "app-sidebar",
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatDividerModule,
    MatButtonModule,
    MatBadgeModule,
  ],
  template: `
    <div class="sidebar">

      <div class="sidebar-logo">
        <mat-icon class="logo-icon">description</mat-icon>
        <div class="logo-text">
          <span class="logo-title">Document SaaS</span>
          <span class="logo-tenant">
            {{ authService.currentUser()?.email }}
          </span>
        </div>
      </div>

      <mat-divider></mat-divider>

      <nav class="nav-list">
        <ng-container *ngFor="let item of getVisibleItems()">
          <div class="nav-item"
            [class.active]="isActive(item.route)"
            (click)="navigate(item.route)">
            <mat-icon class="nav-icon"
              [matBadge]="item.badge && notifService.unreadCount() > 0
                ? notifService.unreadCount() : null"
              matBadgeColor="warn"
              matBadgeSize="small">
              {{ item.icon }}
            </mat-icon>
            <span class="nav-label">{{ item.label }}</span>
            @if (item.badge && notifService.unreadCount() > 0) {
              <span class="nav-badge">
                {{ notifService.unreadCount() }}
              </span>
            }
          </div>
        </ng-container>
      </nav>

      <div class="spacer"></div>
      <mat-divider></mat-divider>

      <div class="sidebar-footer">
        <div class="user-info">
          <div class="user-avatar">{{ getInitials() }}</div>
          <div class="user-details">
            <span class="user-name">
              {{ authService.currentUser()?.fullName }}
            </span>
            <span class="user-role"
              [class]="'role-' +
                authService.currentUser()?.role?.toLowerCase()">
              {{ authService.currentUser()?.role }}
            </span>
          </div>
        </div>
        <button mat-icon-button (click)="authService.logout()"
          class="logout-btn" title="Sign out">
          <mat-icon>logout</mat-icon>
        </button>
      </div>

    </div>
  `,
  styles: [`
    .sidebar {
      height: 100%;
      display: flex;
      flex-direction: column;
      background: white;
    }
    .sidebar-logo {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px;
      min-height: 64px;
    }
    .logo-icon {
      color: #2563EB;
      font-size: 28px;
      width: 28px;
      height: 28px;
    }
    .logo-text { display: flex; flex-direction: column; }
    .logo-title {
      font-size: 16px;
      font-weight: 700;
      color: #1E293B;
    }
    .logo-tenant {
      font-size: 11px;
      color: #94A3B8;
      max-width: 160px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .nav-list {
      padding: 12px 8px;
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .nav-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 12px;
      border-radius: 8px;
      cursor: pointer;
      color: #475569;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.15s;
      position: relative;
    }
    .nav-item:hover {
      background: #F1F5F9;
      color: #1E293B;
    }
    .nav-item.active {
      background: #EFF6FF;
      color: #2563EB;
      font-weight: 600;
    }
    .nav-item.active .nav-icon { color: #2563EB; }
    .nav-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
      color: #94A3B8;
    }
    .nav-label { flex: 1; }
    .nav-badge {
      background: #DC2626;
      color: white;
      border-radius: 10px;
      padding: 1px 6px;
      font-size: 11px;
      font-weight: 700;
    }
    .spacer { flex: 1; }
    .sidebar-footer {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 16px;
    }
    .user-info {
      display: flex;
      align-items: center;
      gap: 10px;
      flex: 1;
      overflow: hidden;
    }
    .user-avatar {
      width: 36px;
      height: 36px;
      min-width: 36px;
      border-radius: 50%;
      background: #2563EB;
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 12px;
      font-weight: 700;
    }
    .user-details {
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }
    .user-name {
      font-size: 13px;
      font-weight: 600;
      color: #1E293B;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .user-role {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
    }
    .role-admin   { color: #D97706; }
    .role-manager { color: #16A34A; }
    .role-viewer  { color: #64748B; }
    .logout-btn { color: #94A3B8; }
  `],
})
export class SidebarComponent {
  @Output() closeSidenav = new EventEmitter<void>();

  authService  = inject(AuthService);
  notifService = inject(NotificationService);
  private router = inject(Router);

  private navItems: NavItem[] = [
    { label: "Dashboard",     icon: "dashboard",     route: "/dashboard",        roles: ["Admin", "Manager", "Viewer"] },
    { label: "Documents",     icon: "description",   route: "/documents",        roles: ["Admin", "Manager", "Viewer"] },
    { label: "Workflows",     icon: "account_tree",  route: "/workflow",         roles: ["Admin", "Manager"] },
    { label: "Upload",        icon: "upload_file",   route: "/documents/upload", roles: ["Admin", "Manager"] },
    { label: "Notifications", icon: "notifications", route: "/notifications",    roles: ["Admin", "Manager", "Viewer"], badge: true },
  ];

  getVisibleItems(): NavItem[] {
    const role = this.authService.currentUser()?.role ?? "";
    return this.navItems.filter(item => item.roles.includes(role));
  }

  isActive(route: string): boolean {
    return this.router.url === route ||
           this.router.url.startsWith(route + "/");
  }

  navigate(route: string): void {
    this.router.navigate([route]);
    this.closeSidenav.emit();
  }

  getInitials(): string {
    const name = this.authService.currentUser()?.fullName ?? "";
    return name.split(" ").map(n => n[0])
      .join("").toUpperCase().slice(0, 2);
  }
}
