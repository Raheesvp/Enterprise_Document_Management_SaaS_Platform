import { Component, Inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { Document } from "../../../models/document.models";

@Component({
  selector: "app-preview-dialog",
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule
  ],
  template: `
    <div class="dialog-container">
      <div class="dialog-header">
        <div class="header-info">
          <mat-icon color="primary">description</mat-icon>
          <h3>{{ data.document.title }}</h3>
        </div>
        <button mat-icon-button (click)="close()">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="dialog-content">
        <!-- Preview Placeholder -->
        <div class="preview-frame">
          @if (isPreviewable()) {
            <div class="preview-mock">
               <mat-icon class="preview-large-icon">insert_drive_file</mat-icon>
               <p>Previewing {{ data.document.title }}</p>
               <span class="mime-type">{{ data.document.mimeType }}</span>
               <div class="mock-content">
                  <div class="line" style="width: 80%"></div>
                  <div class="line" style="width: 60%"></div>
                  <div class="line" style="width: 90%"></div>
                  <div class="line" style="width: 40%"></div>
               </div>
            </div>
          } @else {
            <div class="no-preview">
              <mat-icon>block</mat-icon>
              <p>Preview not available for this file type.</p>
            </div>
          }
        </div>
        
        <div class="doc-metadata">
           <div class="meta-item">
              <span class="label">Uploaded By:</span>
              <span class="value">{{ data.document.uploadedByName || 'System' }}</span>
           </div>
           <div class="meta-item">
              <span class="label">Status:</span>
              <span class="value">{{ data.document.status }}</span>
           </div>
        </div>
      </div>

      <div mat-dialog-actions align="end">
        <button mat-button (click)="close()">Close</button>
        <button mat-raised-button color="primary" (click)="download()">
          <mat-icon>download</mat-icon>
          Download
        </button>
      </div>
    </div>
  `,
  styles: [`
    .dialog-container {
      min-width: 500px;
      max-width: 800px;
    }
    .dialog-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px 24px;
      border-bottom: 1px solid #E2E8F0;
    }
    .header-info {
      display: flex;
      align-items: center;
      gap: 12px;
    }
    .header-info h3 {
      margin: 0;
      font-size: 18px;
      font-weight: 600;
      color: #1E293B;
    }
    .dialog-content {
      padding: 24px;
    }
    .preview-frame {
      width: 100%;
      height: 400px;
      background: #F8FAFC;
      border: 1px dashed #CBD5E1;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 20px;
    }
    .preview-mock {
       text-align: center;
       display: flex;
       flex-direction: column;
       align-items: center;
       gap: 12px;
       width: 100%;
    }
    .preview-large-icon {
       font-size: 64px;
       width: 64px;
       height: 64px;
       color: #94A3B8;
    }
    .mime-type {
       font-size: 12px;
       color: #64748B;
       background: #E2E8F0;
       padding: 2px 8px;
       border-radius: 4px;
    }
    .mock-content {
       width: 60%;
       padding: 20px;
       background: white;
       border-radius: 4px;
       box-shadow: 0 1px 2px rgba(0,0,0,0.05);
       display: flex;
       flex-direction: column;
       gap: 8px;
    }
    .line {
       height: 6px;
       background: #F1F5F9;
       border-radius: 3px;
    }
    .no-preview {
       text-align: center;
       color: #64748B;
    }
    .doc-metadata {
       display: flex;
       gap: 24px;
       padding: 12px;
       background: #F8FAFC;
       border-radius: 6px;
    }
    .meta-item {
       display: flex;
       flex-direction: column;
       gap: 2px;
    }
    .label {
       font-size: 11px;
       color: #64748B;
       text-transform: uppercase;
       font-weight: 600;
    }
    .value {
       font-size: 14px;
       color: #1E293B;
       font-weight: 500;
    }
  `]
})
export class PreviewDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<PreviewDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { document: Document }
  ) {}

  close(): void {
    this.dialogRef.close();
  }

  download(): void {
    // In real app, call download service
    console.log("Downloading", this.data.document.title);
    this.dialogRef.close();
  }

  isPreviewable(): boolean {
    const type = this.data.document.mimeType;
    return type.includes("pdf") || type.includes("image") || type.includes("text");
  }
}
