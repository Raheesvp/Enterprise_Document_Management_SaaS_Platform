import {
  Injectable,
  inject,
  signal,
  OnDestroy,
} from "@angular/core";
import * as signalR from "@microsoft/signalr";
import { environment } from "../../../environments/environment";
import { NotificationService } from "../../features/notifications/services/notification.service";

@Injectable({ providedIn: "root" })
export class SignalRService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private notifService = inject(NotificationService);

  isConnected = signal(false);

  constructor() {
    // Listen for login event from AuthService
    window.addEventListener("auth:login",
      this.onLogin.bind(this));

    // Listen for logout event from AuthService
    window.addEventListener("auth:logout",
      this.onLogout.bind(this));
  }

  ngOnDestroy(): void {
    window.removeEventListener("auth:login",
      this.onLogin.bind(this));
    window.removeEventListener("auth:logout",
      this.onLogout.bind(this));
    this.stopConnection();
  }

  private onLogin(): void {
    this.startConnection();
  }

  private onLogout(): void {
    this.stopConnection();
  }

  startConnection(): void {
    const token = localStorage.getItem("accessToken");
    if (!token) return;

    // Stop existing connection first
    if (this.hubConnection) {
      this.hubConnection.stop();
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(
        `${environment.notificationUrl}/hubs/notifications`,
        {
          accessTokenFactory: () =>
            localStorage.getItem("accessToken") ?? "",
          transport: signalR.HttpTransportType.WebSockets,
          skipNegotiation: true,
        }
      )
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.registerHandlers();

    this.hubConnection
      .start()
      .then(() => {
        this.isConnected.set(true);
        console.log("[SignalR] Connected");
      })
      .catch((err) => {
        console.error("[SignalR] Connection failed:", err);
        this.isConnected.set(false);
      });

    this.hubConnection.onreconnected(() => {
      this.isConnected.set(true);
    });

    this.hubConnection.onreconnecting(() => {
      this.isConnected.set(false);
    });

    this.hubConnection.onclose(() => {
      this.isConnected.set(false);
    });
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.isConnected.set(false);
    }
  }

  private registerHandlers(): void {
    if (!this.hubConnection) return;

    // New notification ? increment badge
    this.hubConnection.on("NewNotification", (notification) => {
      const current = this.notifService.unreadCount();
      this.notifService.unreadCount.set(current + 1);
      console.log("[SignalR] New notification received");
    });

    // Unread count updated
    this.hubConnection.on("UnreadCountUpdated",
      (count: number) => {
        this.notifService.unreadCount.set(count);
      });
  }
}
