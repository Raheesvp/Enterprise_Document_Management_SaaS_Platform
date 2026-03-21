import { Injectable, inject, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable, tap } from "rxjs";
import { environment } from "../../../../environments/environment";
import { Notification } from "../models/notification.models";

@Injectable({ providedIn: "root" })
export class NotificationService {
  private http = inject(HttpClient);
  private base = `${environment.notificationUrl}/api/notifications`;

  // Global unread count signal — used by topbar badge
  unreadCount = signal(0);

  getNotifications(
    page = 1,
    pageSize = 20
  ): Observable<Notification[]> {
    return this.http.get<Notification[]>(
      `${this.base}?page=${page}&pageSize=${pageSize}`);
  }

  getUnreadCount(): Observable<{ unreadCount: number }> {
    return this.http
      .get<{ unreadCount: number }>(`${this.base}/unread-count`)
      .pipe(
        tap(res => this.unreadCount.set(res.unreadCount ?? 0))
      );
  }

  markAsRead(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/read`, {})
      .pipe(
        tap(() => {
          const current = this.unreadCount();
          if (current > 0) this.unreadCount.set(current - 1);
        })
      );
  }
}
