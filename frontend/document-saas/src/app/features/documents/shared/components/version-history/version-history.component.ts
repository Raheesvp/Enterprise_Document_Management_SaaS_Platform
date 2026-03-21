import { Component, Input } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatDividerModule } from "@angular/material/divider";
import { DocumentVersion } from "../../../shared/models/document.models";

@Component({
  selector: "app-version-history",
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatDividerModule],
  template: `
    <div class="version-panel">
      <h3 class="panel-title">
        <mat-icon>history</mat-icon>
        Version History
      </h3>

      @if (!versions || versions.length === 0) {
        <p class="empty-text">No versions available.</p>
      } @else {
        <div class="version-list">
          @for (v of versions; track v.id) {
            <div class="version-item">
              <div class="version-icon">
                <mat-icon>description</mat-icon>
              </div>
              <div class="version-info">
                <span class="version-label">
                  Version {{ v.versionNumber }}
                  @if (v.versionNumber === latestVersion) {
                    <span class="latest-tag">Latest</span>
                  }
                </span>
                <span class="version-meta">
                  {{ formatSize(v.fileSizeBytes) }} •
                  {{ v.createdAt | date: "dd MMM yyyy, HH:mm" }}
                </span>
                <span class="version-author">by {{ v.createdBy }}</span>
              </div>
            </div>
            <mat-divider></mat-divider>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .version-panel {
      background: white;
      border-radius: 12px;
      border: 1px solid #E2E8F0;
      padding: 16px;
    }
    .panel-title {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 15px;
      font-weight: 600;
      color: #1E293B;
      margin: 0 0 16px;
    }
    .panel-title mat-icon { color: #2563EB; font-size: 20px; }
    .empty-text { color: #94A3B8; font-size: 14px; text-align: center; padding: 16px 0; }
    .version-list { display: flex; flex-direction: column; }
    .version-item {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px 0;
    }
    .version-icon mat-icon { color: #94A3B8; font-size: 20px; }
    .version-info {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .version-label {
      font-size: 13px;
      font-weight: 600;
      color: #1E293B;
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .latest-tag {
      background: #EFF6FF;
      color: #2563EB;
      font-size: 10px;
      font-weight: 700;
      padding: 1px 6px;
      border-radius: 10px;
    }
    .version-meta { font-size: 12px; color: #64748B; }
    .version-author { font-size: 12px; color: #94A3B8; }
  `],
})
export class VersionHistoryComponent {
  @Input() versions: DocumentVersion[] = [];

  get latestVersion(): number {
    return this.versions.length > 0
      ? Math.max(...this.versions.map((v) => v.versionNumber))
      : 0;
  }

  formatSize(bytes: number): string {
    if (bytes < 1024)        return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
