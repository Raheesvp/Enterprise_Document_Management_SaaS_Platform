import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatCardModule } from "@angular/material/card";

@Component({
  selector: "app-upload",
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatCardModule],
  template: `
    <div class="page-container">
      <div class="page-header">
        <button mat-button routerLink="/documents">
          <mat-icon>arrow_back</mat-icon>
          Back to Documents
        </button>
      </div>
      <mat-card class="placeholder-card">
        <mat-icon class="placeholder-icon">upload_file</mat-icon>
        <h2>Upload Document</h2>
        <p>Chunked upload UI with progress bars coming on Day 33.</p>
        <button mat-raised-button color="primary" routerLink="/documents">
          Back to Documents
        </button>
      </mat-card>
    </div>
  `,
  styles: [`
    .page-container { padding: 24px; max-width: 800px; margin: 0 auto; }
    .page-header { margin-bottom: 16px; }
    .page-header button { display: flex; align-items: center; gap: 4px; color: #64748B; }
    .placeholder-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 60px;
      text-align: center;
      border-radius: 12px;
    }
    .placeholder-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #CBD5E1;
    }
    h2 { margin: 0; font-size: 20px; font-weight: 700; color: #1E293B; }
    p { margin: 0; color: #64748B; }
  `],
})
export class UploadComponent {}
