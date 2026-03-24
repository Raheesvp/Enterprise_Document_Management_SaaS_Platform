import { Component, inject, OnInit } from "@angular/core";
import { RouterOutlet } from "@angular/router";
import { SignalRService } from "./core/services/signalr.service";

@Component({
  selector: "app-root",
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet></router-outlet>`,
})
export class AppComponent implements OnInit {
  private signalRService = inject(SignalRService);

  ngOnInit(): void {
    // SignalRService listens for auth events automatically
    // If user is already logged in (page refresh)
    // the auth:login event fires from restoreSession
    const token = localStorage.getItem("accessToken");
    if (token) {
      setTimeout(() => {
        this.signalRService.startConnection();
      }, 1000);
    }
  }
}
