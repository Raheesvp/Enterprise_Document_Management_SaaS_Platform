import { Component, Input } from "@angular/core";
import { CommonModule } from "@angular/common";
import { DocumentStatus } from "../../../shared/models/document.models";

@Component({
  selector: "app-status-badge",
  standalone: true,
  imports: [CommonModule],
  template: `
    <span class="badge" [ngClass]="getClass()">
      {{ status }}
    </span>
  `,
  styles: [`
    .badge {
      display: inline-block;
      padding: 3px 10px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
      letter-spacing: 0.3px;
    }
    .draft      { background: #F1F5F9; color: #475569; }
    .pending    { background: #FEF3C7; color: #92400E; }
    .approved   { background: #DCFCE7; color: #166534; }
    .rejected   { background: #FEF2F2; color: #991B1B; }
    .processing { background: #EFF6FF; color: #1D4ED8; }
  `],
})
export class StatusBadgeComponent {
  @Input() status: DocumentStatus = "Draft";

  getClass(): string {
    return this.status.toLowerCase();
  }
}
