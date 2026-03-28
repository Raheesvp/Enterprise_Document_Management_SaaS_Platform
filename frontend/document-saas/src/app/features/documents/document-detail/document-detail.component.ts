import {
  Component,
  OnInit,
  signal,
  inject,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatDividerModule } from "@angular/material/divider";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatChipsModule } from "@angular/material/chips";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import { Document, DocumentVersion, DocumentStatusColors, DocumentStatusLabels, DocumentStatus } from "../models/document.models";
import { DocumentService } from "../services/document.service";

@Component({
  selector: "app-document-detail",
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
    MatSnackBarModule,
  ],
  template: `
    <div class="page-container">

      <!-- Back Button -->
      <button mat-button routerLink="/documents" class="back-btn">
        <mat-icon>arrow_back</mat-icon>
        Back to Documents
      </button>

      @if (isLoading()) {
        <div class="loading-container">
          <mat-spinner diameter="40"></mat-spinner>
        </div>
      }

      @if (!isLoading() && document()) {
        <div class="detail-layout">

          <!-- Main Info Card -->
          <mat-card class="main-card">
            <mat-card-header>
              <div class="doc-header">
                <div class="doc-icon"
                  [style.background]="getTypeColor(document()!.mimeType)">
                  <mat-icon>description</mat-icon>
                </div>
                <div>
                  <h2>{{ document()!.title }}</h2>
                  <span class="status-chip"
                    [style.background]="getStatusColor(document()!.status) + '20'"
                    [style.color]="getStatusColor(document()!.status)">
                    {{ getStatusLabel(document()!.status) }}
                  </span>
                </div>
              </div>
            </mat-card-header>

            <mat-divider></mat-divider>

            <mat-card-content>
    <div class="detail-grid">
      <div class="detail-item">
        <span class="detail-label">File Type</span>
        <span class="detail-value">{{ docService.formatMimeType(document()!.mimeType) }}</span>
      </div>
      <div class="detail-item">
        <span class="detail-label">File Size</span>
        <span class="detail-value">{{ docService.formatFileSize(document()!.fileSizeBytes) }}</span>
      </div>
      <div class="detail-item">
        <span class="detail-label">Uploaded</span>
        <span class="detail-value">{{ document()!.createdAt | date: "dd MMM yyyy HH:mm" }}</span>
      </div>
      <div class="detail-item">
        <span class="detail-label">Last Updated</span>
        <span class="detail-value">{{ document()!.updatedAt | date: "dd MMM yyyy HH:mm" }}</span>
      </div>
    </div>
  </mat-card-content>

  <mat-card-actions>
    <button mat-stroked-button color="primary" routerLink="/workflow">
      <mat-icon>account_tree</mat-icon>
      View Workflow
    </button>
    <button mat-raised-button color="accent" [routerLink]="['/upload', document()!.id]">
      <mat-icon>history</mat-icon>
      Upload New Version
    </button>
  </mat-card-actions>
</mat-card>


          <!-- Version History Card -->
        <mat-card class="versions-card">
  <mat-card-header>
    <h3>Version History</h3>
  </mat-card-header>
  <mat-divider></mat-divider>
  <mat-card-content>
    @if (versions().length === 0) {
      <p class="no-versions">No versions available</p>
    }
    <div class="versions-list">
      @for (version of versions(); track version.id) {
        <div class="version-item">
          <div class="version-info">
            <span class="version-text">
              v{{ version.versionNumber }} — uploaded {{ version.createdAt | date: "dd MMM yyyy" }}
            </span>
            @if (version.versionNumber === document()?.currentVersion) {
              <span class="current-label">(current)</span>
            }
          </div>

          <div class="actions">
            <button mat-icon-button color="primary" 
                    (click)="previewVersion(version)" 
                    title="Preview this version">
              <mat-icon>visibility</mat-icon>
            </button>
            <button mat-icon-button color="primary" 
                    (click)="downloadVersion(version)" 
                    title="Download this version">
              <mat-icon>download</mat-icon>
            </button>
          </div>
        </div>
      }
    </div>
  </mat-card-content>
</mat-card>

        </div>
      }

      @if (!isLoading() && !document()) {
        <div class="not-found">
          <mat-icon>error_outline</mat-icon>
          <h3>Document not found</h3>
          <button mat-raised-button color="primary"
            routerLink="/documents">
            Back to Documents
          </button>
        </div>
      }

    </div>
  `,
  styles: [`
    .page-container {
      padding: 24px;
      max-width: 1000px;
      margin: 0 auto;
    }
    .back-btn {
      display: flex;
      align-items: center;
      gap: 4px;
      color: #64748B;
      margin-bottom: 16px;
    }
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 60px;
    }
    .detail-layout {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 20px;
    }
    .main-card, .versions-card {
      border-radius: 12px;
    }
    .doc-header {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 8px 0;
      width: 100%;
    }
    .doc-icon {
      width: 48px;
      height: 48px;
      min-width: 48px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .doc-icon mat-icon { color: white; }
    .doc-header h2 {
      margin: 0 0 6px;
      font-size: 20px;
      font-weight: 700;
      color: #1E293B;
    }
    .status-chip {
      display: inline-block;
      padding: 3px 10px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
    }
    .detail-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 20px;
      padding: 16px 0;
    }
    .detail-item {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .detail-label {
      font-size: 11px;
      font-weight: 600;
      color: #94A3B8;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
    .detail-value {
      font-size: 14px;
      color: #1E293B;
      font-weight: 500;
    }
    mat-card-actions {
      padding: 8px 16px 16px;
    }
    mat-card-actions button {
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .versions-card h3 {
      font-size: 16px;
      font-weight: 600;
      color: #1E293B;
      margin: 0;
      padding: 16px;
    }
    .no-versions {
      color: #94A3B8;
      font-size: 14px;
      padding: 16px 0;
      text-align: center;
    }
    .version-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 12px 16px;
      border-bottom: 1px solid #F1F5F9;
      transition: background-color 0.2s;
    }
    .version-item:hover {
      background-color: #F8FAFC;
    }
    .version-item:last-child {
      border-bottom: none;
    }
    .version-info {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .actions {
      display: flex;
      gap: 4px;
    }
    .version-text {
      font-size: 14px;
      color: #334155;
      font-weight: 500;
    }
    .current-label {
      font-size: 14px;
      color: #64748B;
      font-weight: 400;
    }
    .not-found {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 60px;
      text-align: center;
      color: #64748B;
    }
    .not-found mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
    }
    @media (max-width: 768px) {
      .detail-layout {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class DocumentDetailComponent implements OnInit {
  docService = inject(DocumentService);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);

  document   = signal<Document | null>(null);
  versions   = signal<DocumentVersion[]>([]);
  isLoading  = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");
    if (!id) { this.isLoading.set(false); return; }

    this.docService.getDocument(id).subscribe({
      next: (doc) => {
        this.document.set(doc);
        this.isLoading.set(false);
        this.loadVersions(id);
      },
      error: () => {
        this.document.set(null);
        this.isLoading.set(false);
      },
    });
  }

private loadVersions(documentId: string): void {
  this.docService.getVersions(documentId).subscribe({
    next: (versions) => {
      // Sort by version number descending so the newest is always on top
      const sorted = (versions ?? []).sort((a, b) => b.versionNumber - a.versionNumber);
      this.versions.set(sorted);
    },
    error: () => this.versions.set([]),
  });
}

  previewDocument(): void {
    const doc = this.document();
    if (!doc?.downloadUrl) return;
    window.open(doc.downloadUrl, "_blank");
  }

  downloadDocument(): void {
    const doc = this.document();
    if (!doc?.downloadUrl) return;

    this.docService.downloadFromUrl(
      doc.downloadUrl,
      this.docService.buildDownloadFileName(doc.title, doc.mimeType)
    );
  }

  previewVersion(version: DocumentVersion): void {
    if (!version.downloadUrl) return;
    window.open(version.downloadUrl, "_blank");
  }

  downloadVersion(version: DocumentVersion): void {
    const doc = this.document();
    if (!doc || !version.downloadUrl) return;

    this.docService.downloadFromUrl(
      version.downloadUrl,
      this.docService.buildDownloadFileName(
        doc.title,
        doc.mimeType,
        `-v${version.versionNumber}`
      )
    );
  }

  getStatusColor(status: string): string {
    return DocumentStatusColors[status as DocumentStatus] ?? "#64748B";
  }

  getStatusLabel(status: string): string {
    return DocumentStatusLabels[status as DocumentStatus] ?? status;
  }

  getTypeColor(mimeType: string): string {
    const colors: Record<string, string> = {
      "application/pdf": "#DC2626",
      "image/jpeg":      "#2563EB",
      "image/png":       "#2563EB",
      "text/plain":      "#64748B",
    };
    return colors[mimeType] ?? "#7C3AED";
  }
}
