import {
  Component,
  OnInit,
  signal,
  inject,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatDividerModule } from "@angular/material/divider";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatChipsModule } from "@angular/material/chips";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import {
  Notification,
  NotificationType,
  NotificationIcons,
  NotificationColors,
} from "./models/notification.models";
import { NotificationService } from "./services/notification.service";

@Component({
  selector: "app-notifications",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatTooltipModule,
    MatSnackBarModule,
  ],
  template: `
    <div class="page-container">

      <!-- Header -->
      <div class="page-header">
        <div>
          <h1>Notifications</h1>
          <p>
            {{ unreadCount() }} unread
            of {{ notifications().length }} total
          </p>
        </div>
        @if (unreadCount() > 0) {
          <button mat-stroked-button (click)="markAllRead()">
            <mat-icon>done_all</mat-icon>
            Mark All Read
          </button>
        }
      </div>

      <!-- Loading -->
      @if (isLoading()) {
        <div class="loading-container">
          <mat-spinner diameter="40"></mat-spinner>
        </div>
      }

      <!-- Empty State -->
      @if (!isLoading() && notifications().length === 0) {
        <div class="empty-state">
          <mat-icon class="empty-icon">notifications_none</mat-icon>
          <h3>No notifications yet</h3>
          <p>You will be notified when workflow actions are needed</p>
        </div>
      }

      <!-- Notifications List -->
      @if (!isLoading() && notifications().length > 0) {
        <div class="notifications-list">
          @for (notif of notifications(); track notif.id) {
            <mat-card
              class="notif-card"
              [class.unread]="notif.status === 'Unread'"
              (click)="onNotificationClick(notif)">

              <div class="notif-row">

                <!-- Icon -->
                <div class="notif-icon"
                  [style.background]="getColor(notif.type) + '20'">
                  <mat-icon [style.color]="getColor(notif.type)">
                    {{ getIcon(notif.type) }}
                  </mat-icon>
                </div>

                <!-- Content -->
                <div class="notif-content">
                  <div class="notif-header">
                    <span class="notif-title">
                      {{ notif.title }}
                    </span>
                    @if (notif.status === "Unread") {
                      <span class="unread-dot"></span>
                    }
                  </div>
                  <p class="notif-message">{{ notif.message }}</p>
                  <span class="notif-time">
                    {{ notif.createdAt | date: "dd MMM yyyy HH:mm" }}
                  </span>
                </div>

                <!-- Actions -->
                <div class="notif-actions">
                  @if (notif.status === "Unread") {
                    <button mat-icon-button
                      matTooltip="Mark as read"
                      (click)="$event.stopPropagation();
                               markAsRead(notif)">
                      <mat-icon>check</mat-icon>
                    </button>
                  }
                  @if (notif.referenceId &&
                       notif.referenceType === "WorkflowInstance") {
                    <button mat-icon-button color="primary"
                      matTooltip="View workflow"
                      [routerLink]="['/workflow', notif.referenceId]"
                      (click)="$event.stopPropagation()">
                      <mat-icon>open_in_new</mat-icon>
                    </button>
                  }
                </div>

              </div>

            </mat-card>
          }
        </div>
      }

    </div>
  `,
  styles: [`
    .page-container {
      padding: 24px;
      max-width: 800px;
      margin: 0 auto;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 24px;
      flex-wrap: wrap;
      gap: 16px;
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
    .page-header button {
      display: flex;
      align-items: center;
      gap: 6px;
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
    .notifications-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .notif-card {
      border-radius: 10px;
      cursor: pointer;
      transition: all 0.15s;
      border-left: 3px solid transparent;
    }
    .notif-card:hover {
      box-shadow: 0 4px 12px rgba(0,0,0,0.08);
      transform: translateX(2px);
    }
    .notif-card.unread {
      border-left-color: #2563EB;
      background: #FAFBFF;
    }
    .notif-row {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px;
    }
    .notif-icon {
      width: 40px;
      height: 40px;
      min-width: 40px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .notif-icon mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
    }
    .notif-content {
      flex: 1;
      overflow: hidden;
    }
    .notif-header {
      display: flex;
      align-items: center;
      gap: 8px;
      margin-bottom: 4px;
    }
    .notif-title {
      font-size: 14px;
      font-weight: 600;
      color: #1E293B;
    }
    .unread-dot {
      width: 8px;
      height: 8px;
      min-width: 8px;
      border-radius: 50%;
      background: #2563EB;
    }
    .notif-message {
      font-size: 13px;
      color: #475569;
      margin: 0 0 4px;
      line-height: 1.4;
    }
    .notif-time {
      font-size: 11px;
      color: #94A3B8;
    }
    .notif-actions {
      display: flex;
      gap: 4px;
      flex-shrink: 0;
    }
  `],
})
export class NotificationsComponent implements OnInit {
  private notifService = inject(NotificationService);
  private snackBar     = inject(MatSnackBar);

  notifications = signal<Notification[]>([]);
  isLoading     = signal(true);
  unreadCount   = signal(0);

  ngOnInit(): void {
    this.loadNotifications();
  }

  private loadNotifications(): void {
    this.notifService.getNotifications().subscribe({
      next: (items) => {
        const list = Array.isArray(items) ? items : [];
        this.notifications.set(list);
        this.unreadCount.set(
          list.filter(n => n.status === "Unread").length);
        this.isLoading.set(false);
      },
      error: () => {
        this.notifications.set([]);
        this.isLoading.set(false);
      },
    });
  }

  markAsRead(notif: Notification): void {
    this.notifService.markAsRead(notif.id).subscribe({
      next: () => {
        this.notifications.update(items =>
          items.map(n =>
            n.id === notif.id
              ? { ...n, status: "Read" as const }
              : n));
        this.unreadCount.update(c => Math.max(0, c - 1));
      },
      error: () => {
        this.snackBar.open(
          "Failed to mark as read", "Close",
          { duration: 2000 });
      },
    });
  }

  markAllRead(): void {
    const unread = this.notifications()
      .filter(n => n.status === "Unread");

    unread.forEach(n => {
      this.notifService.markAsRead(n.id).subscribe();
    });

    this.notifications.update(items =>
      items.map(n => ({ ...n, status: "Read" as const })));
    this.unreadCount.set(0);

    this.snackBar.open(
      "All notifications marked as read", "Close",
      { duration: 2000 });
  }

  onNotificationClick(notif: Notification): void {
    if (notif.status === "Unread") {
      this.markAsRead(notif);
    }
  }

  getIcon(type: string): string {
    return NotificationIcons[type as NotificationType]
      ?? "notifications";
  }

  getColor(type: string): string {
    return NotificationColors[type as NotificationType]
      ?? "#64748B";
  }
}
