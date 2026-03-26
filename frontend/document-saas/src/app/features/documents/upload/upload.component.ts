import {
  Component,
  signal,
  inject,
  Input,
  OnDestroy,
  OnInit,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink, Router, ActivatedRoute } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import { MatChipsModule } from "@angular/material/chips";
import { MatDividerModule } from "@angular/material/divider";
import * as tus from "tus-js-client";
import { environment } from "../../../../environments/environment";
import { AuthService } from "../../../core/services/auth.service";



interface UploadFile {
  id:           string;
  file:         File;
  status:       UploadStatus;
  progress:     number;
  uploadId?:    string;
  error?:       string;
  upload?:      any;
  startedAt?:   Date;
  completedAt?: Date;
}

type UploadStatus =
  | "pending"
  | "uploading"
  | "paused"
  | "complete"
  | "error";

@Component({
  selector: "app-upload",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatChipsModule,
    MatDividerModule,
  ],
  template: `
    <div class="page-container">

      <!-- Header -->
      <div class="page-header">
        <button mat-button (click)="goBack()" class="back-btn">
          <mat-icon>arrow_back</mat-icon>
          Back {{ documentId ? 'to Document' : 'to Documents' }}
        </button>
        <div>
          <h1>{{ documentId ? 'Upload New Version' : 'Upload Documents' }}</h1>
          <p>{{ documentId ? 'Add a new version to the existing document' : 'Upload files with automatic chunking and resume support' }}</p>
        </div>
      </div>

      <!-- Drop Zone -->
      <mat-card class="drop-zone"
        [class.drag-over]="isDragOver()"
        (dragover)="onDragOver($event)"
        (dragleave)="onDragLeave()"
        (drop)="onDrop($event)"
        (click)="fileInput.click()">

        <mat-icon class="drop-icon">cloud_upload</mat-icon>
        <h3>Drop files here or click to browse</h3>
        <p>Supports PDF, Word, Excel, images � Max 500MB per file</p>

        <button mat-stroked-button color="primary"
          (click)="$event.stopPropagation(); fileInput.click()">
          <mat-icon>folder_open</mat-icon>
          Browse Files
        </button>

        <input #fileInput type="file" multiple hidden
          (change)="onFileSelected($event)"
          accept=".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png,.txt"/>

      </mat-card>

      <!-- Upload Queue -->
      @if (files().length > 0) {
        <div class="queue-section">

          <div class="queue-header">
            <h2>Upload Queue ({{ files().length }})</h2>
            <div class="queue-actions">
              @if (hasPendingFiles()) {
                <button mat-raised-button color="primary"
                  (click)="startAll()">
                  <mat-icon>play_arrow</mat-icon>
                  Upload All
                </button>
              }
              @if (hasUploadingFiles()) {
                <button mat-stroked-button
                  (click)="pauseAll()">
                  <mat-icon>pause</mat-icon>
                  Pause All
                </button>
              }
              <button mat-stroked-button color="warn"
                (click)="clearCompleted()">
                <mat-icon>clear_all</mat-icon>
                Clear Done
              </button>
            </div>
          </div>

          <div class="file-list">
            @for (f of files(); track f.id) {
              <mat-card class="file-card">

                <div class="file-row">

                  <!-- File Icon -->
                  <div class="file-icon"
                    [style.background]="getFileColor(f.file.type) + '20'">
                    <mat-icon [style.color]="getFileColor(f.file.type)">
                      {{ getFileIcon(f.file.type) }}
                    </mat-icon>
                  </div>

                  <!-- File Info -->
                  <div class="file-info">
                    <span class="file-name">{{ f.file.name }}</span>
                    <span class="file-meta">
                      {{ formatSize(f.file.size) }}
                      @if (f.status === "uploading") {
                        � {{ f.progress }}% uploaded
                      }
                      @if (f.status === "complete") {
                        � Completed {{ f.completedAt | date: "HH:mm:ss" }}
                      }
                      @if (f.error) {
                        � {{ f.error }}
                      }
                    </span>

                    <!-- Progress Bar -->
                    @if (f.status === "uploading" ||
                         f.status === "paused") {
                      <mat-progress-bar
                        [mode]="f.status === 'uploading'
                          ? 'determinate' : 'buffer'"
                        [value]="f.progress"
                        [color]="f.status === 'paused'
                          ? 'accent' : 'primary'">
                      </mat-progress-bar>
                    }
                    @if (f.status === "complete") {
                      <mat-progress-bar
                        mode="determinate"
                        value="100"
                        color="primary">
                      </mat-progress-bar>
                    }
                  </div>

                  <!-- Status Badge -->
                  <span class="status-badge"
                    [class]="'status-' + f.status">
                    @if (f.status === "uploading") {
                      <mat-icon>sync</mat-icon>
                    } @else if (f.status === "complete") {
                      <mat-icon>check_circle</mat-icon>
                    } @else if (f.status === "paused") {
                      <mat-icon>pause_circle</mat-icon>
                    } @else if (f.status === "error") {
                      <mat-icon>error</mat-icon>
                    } @else {
                      <mat-icon>schedule</mat-icon>
                    }
                    {{ f.status }}
                  </span>

                  <!-- Actions -->
                  <div class="file-actions">
                    @if (f.status === "pending") {
                      <button mat-icon-button color="primary"
                        matTooltip="Start upload"
                        (click)="startUpload(f)">
                        <mat-icon>play_arrow</mat-icon>
                      </button>
                    }
                    @if (f.status === "uploading") {
                      <button mat-icon-button
                        matTooltip="Pause upload"
                        (click)="pauseUpload(f)">
                        <mat-icon>pause</mat-icon>
                      </button>
                    }
                    @if (f.status === "paused") {
                      <button mat-icon-button color="primary"
                        matTooltip="Resume upload"
                        (click)="resumeUpload(f)">
                        <mat-icon>play_arrow</mat-icon>
                      </button>
                    }
                    @if (f.status === "error") {
                      <button mat-icon-button color="warn"
                        matTooltip="Retry upload"
                        (click)="retryUpload(f)">
                        <mat-icon>refresh</mat-icon>
                      </button>
                    }
                    @if (f.status !== "uploading") {
                      <button mat-icon-button color="warn"
                        matTooltip="Remove"
                        (click)="removeFile(f)">
                        <mat-icon>close</mat-icon>
                      </button>
                    }
                  </div>

                </div>

              </mat-card>
            }
          </div>

        </div>
      }

    </div>
  `,
  styles: [`
    .page-container {
      padding: 24px;
      max-width: 900px;
      margin: 0 auto;
    }
    .page-header {
      margin-bottom: 24px;
    }
    .back-btn {
      display: flex;
      align-items: center;
      gap: 4px;
      color: #64748B;
      margin-bottom: 8px;
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
    .drop-zone {
      border: 2px dashed #CBD5E1;
      border-radius: 16px;
      padding: 48px;
      text-align: center;
      cursor: pointer;
      transition: all 0.2s;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      margin-bottom: 24px;
    }
    .drop-zone:hover {
      border-color: #2563EB;
      background: #EFF6FF;
    }
    .drop-zone.drag-over {
      border-color: #2563EB;
      background: #EFF6FF;
      transform: scale(1.01);
    }
    .drop-icon {
      font-size: 56px;
      width: 56px;
      height: 56px;
      color: #CBD5E1;
    }
    .drop-zone h3 {
      font-size: 18px;
      font-weight: 600;
      color: #1E293B;
      margin: 0;
    }
    .drop-zone p {
      color: #64748B;
      margin: 0;
      font-size: 13px;
    }
    .drop-zone button {
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .queue-section { margin-top: 8px; }
    .queue-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
      flex-wrap: wrap;
      gap: 12px;
    }
    .queue-header h2 {
      font-size: 18px;
      font-weight: 600;
      color: #1E293B;
      margin: 0;
    }
    .queue-actions {
      display: flex;
      gap: 8px;
      flex-wrap: wrap;
    }
    .queue-actions button {
      display: flex;
      align-items: center;
      gap: 4px;
    }
    .file-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .file-card {
      border-radius: 10px;
      padding: 4px;
    }
    .file-row {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 8px;
    }
    .file-icon {
      width: 40px;
      height: 40px;
      min-width: 40px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .file-info {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 4px;
      overflow: hidden;
    }
    .file-name {
      font-size: 14px;
      font-weight: 500;
      color: #1E293B;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .file-meta {
      font-size: 12px;
      color: #94A3B8;
    }
    .status-badge {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      padding: 3px 8px;
      border-radius: 12px;
      white-space: nowrap;
    }
    .status-badge mat-icon {
      font-size: 14px;
      width: 14px;
      height: 14px;
    }
    .status-pending   { background: #F1F5F9; color: #64748B; }
    .status-uploading { background: #EFF6FF; color: #2563EB; }
    .status-paused    { background: #FFF7ED; color: #EA580C; }
    .status-complete  { background: #F0FDF4; color: #16A34A; }
    .status-error     { background: #FEF2F2; color: #DC2626; }
    .file-actions {
      display: flex;
      gap: 4px;
    }
    mat-progress-bar {
      border-radius: 4px;
      height: 4px;
    }
  `],
})
export class UploadComponent implements OnInit, OnDestroy {

  @Input() documentId? :string;

  private authService = inject(AuthService);
  private snackBar    = inject(MatSnackBar);
  private router      = inject(Router);
  private route       = inject(ActivatedRoute);

  files     = signal<UploadFile[]>([]);
  isDragOver = signal(false);

  ngOnInit(): void {
    // Read from route param if Input not provided
    if (!this.documentId) {
      this.documentId = this.route.snapshot.paramMap.get("documentId") || undefined;
    }
  }

  goBack(): void {
    if (this.documentId) {
      this.router.navigate(['/documents', this.documentId]);
    } else {
      this.router.navigate(['/documents']);
    }
  }

  // -- Drag and Drop ---------------------------------------

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(true);
  }

  onDragLeave(): void {
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver.set(false);
    const files = event.dataTransfer?.files;
    if (files) this.addFiles(Array.from(files));
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.addFiles(Array.from(input.files));
      input.value = "";
    }
  }

  private addFiles(files: File[]): void {
    const newFiles: UploadFile[] = files.map(file => ({
      id:       crypto.randomUUID(),
      file,
      status:   "pending",
      progress: 0,
    }));
    this.files.update(existing => [...existing, ...newFiles]);
  }

  // -- Upload Controls -------------------------------------

  startAll(): void {
    this.files()
      .filter(f => f.status === "pending")
      .forEach(f => this.startUpload(f));
  }

  pauseAll(): void {
    this.files()
      .filter(f => f.status === "uploading")
      .forEach(f => this.pauseUpload(f));
  }

  clearCompleted(): void {
    this.files.update(files =>
      files.filter(f => f.status !== "complete"));
  }

  removeFile(file: UploadFile): void {
    if (file.upload) {
      try { file.upload.abort(); } catch {}
    }
    this.files.update(files =>
      files.filter(f => f.id !== file.id));
  }

  pauseUpload(file: UploadFile): void {
    if (file.upload) {
      file.upload.abort();
      this.updateFile(file.id, { status: "paused" });
    }
  }

  resumeUpload(file: UploadFile): void {
    if (file.upload) {
      file.upload.start();
      this.updateFile(file.id, { status: "uploading" });
    }
  }

  retryUpload(file: UploadFile): void {
    this.updateFile(file.id, {
      status:   "pending",
      progress: 0,
      error:    undefined,
      upload:   undefined,
    });
    this.startUpload(
      this.files().find(f => f.id === file.id)!);
  }

  startUpload(file: UploadFile): void {
    const token = localStorage.getItem("accessToken");
    if (!token) {
      this.snackBar.open("Not authenticated", "Close",
        { duration: 3000 });
      return;
    }

    // Use chunked upload via our custom API
    // tus-js-client sends chunks to our UploadController
    const chunkSize = 1024 * 1024; // 1MB chunks
    const totalSize = file.file.size;

    // Init upload session first
    fetch(
      `${environment.documentUrl}/api/upload/init`,
      {
        method:  "POST",
        headers: {
          "Content-Type":  "application/json",
          "Authorization": `Bearer ${token}`,
        },
        body: JSON.stringify({
          fileName:    file.file.name,
          contentType: file.file.type || "application/octet-stream",
          totalSize:   totalSize,
          documentId :this.documentId || null
        }),
      }
    )
    .then(res => res.json())
    .then(data => {
      const uploadId = data.uploadId;
      this.updateFile(file.id, {
        uploadId,
        status:   "uploading",
        startedAt: new Date(),
      });
      this.uploadChunks(file, uploadId, token, totalSize, chunkSize);
    })
    .catch(() => {
      this.updateFile(file.id, {
        status: "error",
        error:  "Failed to initialize upload",
      });
    });
  }

  private uploadChunks(
    file: UploadFile,
    uploadId: string,
    token: string,
    totalSize: number,
    chunkSize: number
  ): void {
    let offset = 0;

    const uploadNextChunk = () => {
      const currentFile = this.files().find(f => f.id === file.id);
      if (!currentFile || currentFile.status === "paused") return;

      const chunk = file.file.slice(offset, offset + chunkSize);

      fetch(
        `${environment.documentUrl}/api/upload/${uploadId}/chunk?offset=${offset}`,
        {
          method:  "POST",
          headers: {
            "Content-Type":  "application/octet-stream",
            "Authorization": `Bearer ${token}`,
          },
          body: chunk,
        }
      )
      .then(res => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        return res.json();
      })
      .then(data => {
        offset += chunk.size;
        const progress = Math.round((offset / totalSize) * 100);
        this.updateFile(file.id, { progress });

        if (data.status === "Complete" || offset >= totalSize) {
          this.updateFile(file.id, {
            status:      "complete",
            progress:    100,
            completedAt: new Date(),
          });
          this.snackBar.open(
            `"${file.file.name}" uploaded successfully`,
            "View", { duration: 4000 })
            .onAction().subscribe(() => {
              if (this.documentId) {
                this.router.navigate(["/documents", this.documentId]);
              } else {
                this.router.navigate(["/documents"]);
              }
            });
          
          if (this.documentId) {
             setTimeout(() => this.router.navigate(["/documents", this.documentId]), 1500);
          }
        } else {
          uploadNextChunk();
        }
      })
      .catch(() => {
        this.updateFile(file.id, {
          status: "error",
          error:  "Upload failed � click retry",
        });
      });
    };

    uploadNextChunk();
  }

  // -- Helpers ---------------------------------------------

  private updateFile(
    id: string,
    changes: Partial<UploadFile>
  ): void {
    this.files.update(files =>
      files.map(f => f.id === id ? { ...f, ...changes } : f));
  }

  hasPendingFiles(): boolean {
    return this.files().some(f => f.status === "pending");
  }

  hasUploadingFiles(): boolean {
    return this.files().some(f => f.status === "uploading");
  }

  formatSize(bytes: number): string {
    if (bytes === 0) return "0 B";
    const k     = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i     = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
  }

  getFileColor(mimeType: string): string {
    const colors: Record<string, string> = {
      "application/pdf": "#DC2626",
      "image/jpeg":      "#2563EB",
      "image/png":       "#2563EB",
      "text/plain":      "#64748B",
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document": "#1D4ED8",
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": "#16A34A",
    };
    return colors[mimeType] ?? "#7C3AED";
  }

  getFileIcon(mimeType: string): string {
    if (mimeType.includes("pdf"))   return "picture_as_pdf";
    if (mimeType.includes("image")) return "image";
    if (mimeType.includes("word"))  return "description";
    if (mimeType.includes("sheet")) return "table_chart";
    return "insert_drive_file";
  }

  ngOnDestroy(): void {
    // Pause all active uploads on component destroy
    this.files()
      .filter(f => f.status === "uploading")
      .forEach(f => this.pauseUpload(f));
  }
}
