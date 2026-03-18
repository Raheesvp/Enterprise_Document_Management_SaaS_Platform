import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";

@Component({
  selector: "app-notifications",
  standalone: true,
  imports: [CommonModule],
  template: `
    <div style="padding:24px">
      <h2>Notifications</h2>
      <p style="color:#64748B">
        Notifications UI coming in Day 34.
      </p>
    </div>
  `,
})
export class NotificationsComponent {}
