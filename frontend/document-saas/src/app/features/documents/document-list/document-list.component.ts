import {
  Component,
  OnInit,
  signal,
  inject,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { Router, RouterLink } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { MatTableModule } from "@angular/material/table";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatSortModule } from "@angular/material/sort";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatChipsModule } from "@angular/material/chips";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatMenuModule } from "@angular/material/menu";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { debounceTime, distinctUntilChanged, Subject } from "rxjs";
import {
  Document,
  DocumentFilter,
  DocumentStatus,
  DocumentStatusColors,
  DocumentStatusLabels,
} from "../models/document.models";
import { DocumentService } from "../services/document.service";
import { AuthService } from "../../../core/services/auth.service";
import { WorkflowService } from "../../workflow/services/workflow.service";
import { PreviewDialogComponent } from "../shared/components/preview-dialog/preview-dialog.component";

@Component({
  selector: "app-document-list",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatMenuModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDialogModule,
  ],
  template: `
    <div class="page-container">

      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1>Documents</h1>
          <p>Manage and track all your documents</p>
        </div>
        <button mat-raised-button color="primary"
          routerLink="/documents/upload">
          <mat-icon>upload_file</mat-icon>
          Upload Document
        </button>
      </div>

      <!-- Filters Row -->
      <div class="filters-row">

        <!-- Search -->
        <mat-form-field appearance="outline" class="search-field">
          <mat-label>Search documents</mat-label>
          <input matInput
            [(ngModel)]="searchTerm"
            (ngModelChange)="onSearchChange($event)"
            placeholder="Search by title..."/>
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>

        <!-- Status Filter -->
        <mat-form-field appearance="outline" class="status-field">
          <mat-label>Status</mat-label>
          <mat-select [(ngModel)]="selectedStatus"
            (ngModelChange)="onStatusChange($event)">
            <mat-option value="">All Statuses</mat-option>
            <mat-option value="Draft">Draft</mat-option>
            <mat-option value="UnderReview">Under Review</mat-option>
            <mat-option value="Approved">Approved</mat-option>
            <mat-option value="Rejected">Rejected</mat-option>
            <mat-option value="Archived">Archived</mat-option>
          </mat-select>
        </mat-form-field>

        <!-- Clear Filters -->
        @if (selectedStatus || searchTerm) {
          <button mat-stroked-button (click)="clearFilters()">
            <mat-icon>clear</mat-icon>
            Clear
          </button>
        }

      </div>

      <!-- Loading -->
      @if (isLoading()) {
        <div class="loading-container">
          <mat-spinner diameter="40"></mat-spinner>
          <p>Loading documents...</p>
        </div>
      }

      <!-- Empty State -->
      @if (!isLoading() && documents().length === 0) {
        <div class="empty-state">
          <mat-icon class="empty-icon">description</mat-icon>
          <h3>No documents found</h3>
          <p>Upload your first document to get started</p>
          <button mat-raised-button color="primary"
            routerLink="/documents/upload">
            <mat-icon>upload_file</mat-icon>
            Upload Document
          </button>
        </div>
      }

      <!-- Documents Table -->
      @if (!isLoading() && documents().length > 0) {
        <div class="table-container">
          <table mat-table [dataSource]="documents()"
            class="documents-table">

            <!-- Title Column -->
            <ng-container matColumnDef="title">
              <th mat-header-cell *matHeaderCellDef>Title</th>
              <td mat-cell *matCellDef="let doc">
                <div class="doc-title-cell">
                  <div class="doc-icon"
                    [style.background]="getTypeColor(doc.mimeType)">
                    <mat-icon>description</mat-icon>
                  </div>
                  <div class="doc-info">
                    <span class="doc-title"
                      [routerLink]="['/documents', doc.id]">
                      {{ doc.title }}
                    </span>
                    <span class="doc-type">
                      {{ docService.formatMimeType(doc.mimeType) }}
                      · {{ docService.formatFileSize(doc.fileSizeBytes) }}
                    </span>
                  </div>
                </div>
              </td>
            </ng-container>

            <!-- Status Column -->
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let doc">
                <span class="status-chip"
                  [style.background]="getStatusColor(doc.status) + '20'"
                  [style.color]="getStatusColor(doc.status)">
                  {{ getStatusLabel(doc.status) }}
                </span>
              </td>
            </ng-container>

            <!-- Uploaded By Column -->
            <ng-container matColumnDef="uploadedByName">
              <th mat-header-cell *matHeaderCellDef>Uploaded By</th>
              <td mat-cell *matCellDef="let doc">
                <div class="uploader-cell">
                   <span class="uploader-name">{{ doc.uploadedByName || 'System' }}</span>
                </div>
              </td>
            </ng-container>

            <!-- Date Column -->
            <ng-container matColumnDef="createdAt">
              <th mat-header-cell *matHeaderCellDef>Uploaded On</th>
              <td mat-cell *matCellDef="let doc">
                <span class="date-text">
                  {{ doc.createdAt | date: "dd MMM yyyy" }}
                </span>
              </td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef></th>
              <td mat-cell *matCellDef="let doc">
                <button mat-icon-button
                  [matMenuTriggerFor]="docMenu"
                  matTooltip="Actions"
                  (click)="$event.stopPropagation()">
                  <mat-icon>more_vert</mat-icon>
                </button>
                <mat-menu #docMenu="matMenu">
                  <button mat-menu-item
                    [routerLink]="['/documents', doc.id]">
                    <mat-icon>visibility</mat-icon>
                    View Details
                  </button>
                  
                  <!-- Admin/Manager Actions -->
                  @if (authService.isManager()) {
                    <button mat-menu-item (click)="previewDocument(doc)">
                      <mat-icon>preview</mat-icon>
                      Preview
                    </button>
                    <button mat-menu-item (click)="downloadDocument(doc)">
                      <mat-icon>download</mat-icon>
                      Download
                    </button>
                    <button mat-menu-item (click)="updateStatus(doc)">
                      <mat-icon>rule</mat-icon>
                      Process Workflow
                    </button>
                  }

                  <button mat-menu-item
                    [routerLink]="['/workflow']">
                    <mat-icon>account_tree</mat-icon>
                    View History
                  </button>
                  @if (doc.status !== "Archived") {
                    <button mat-menu-item
                      (click)="archiveDocument(doc)">
                      <mat-icon>archive</mat-icon>
                      Archive
                    </button>
                  }
                </mat-menu>
              </td>
            </ng-container>

            <tr mat-header-row
              *matHeaderRowDef="displayedColumns">
            </tr>
            <tr mat-row
              *matRowDef="let row; columns: displayedColumns;"
              class="table-row"
              [routerLink]="['/documents', row.id]">
            </tr>

          </table>

          <!-- Paginator -->
          <mat-paginator
            [length]="totalCount()"
            [pageSize]="pageSize"
            [pageSizeOptions]="[10, 25, 50]"
            [pageIndex]="currentPage() - 1"
            (page)="onPageChange($event)"
            showFirstLastButtons>
          </mat-paginator>

        </div>
      }

    </div>
  `,
  styles: [`
    .page-container {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 24px;
      flex-wrap: wrap;
      gap: 16px;
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
    .page-header button {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .filters-row {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;
      flex-wrap: wrap;
      align-items: center;
    }
    .search-field {
      flex: 1;
      min-width: 200px;
    }
    .status-field {
      width: 180px;
    }
    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 60px;
      gap: 16px;
      color: #64748B;
    }
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 60px;
      gap: 12px;
      text-align: center;
    }
    .empty-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: #CBD5E1;
    }
    .empty-state h3 {
      font-size: 18px;
      color: #1E293B;
      margin: 0;
    }
    .empty-state p {
      color: #64748B;
      margin: 0;
    }
    .empty-state button {
      margin-top: 8px;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .table-container {
      background: white;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0,0,0,0.08);
    }
    .documents-table {
      width: 100%;
    }
    .doc-title-cell {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 8px 0;
    }
    .doc-icon {
      width: 36px;
      height: 36px;
      min-width: 36px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .doc-icon mat-icon {
      font-size: 18px;
      width: 18px;
      height: 18px;
      color: white;
    }
    .doc-info {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .doc-title {
      font-size: 14px;
      font-weight: 500;
      color: #1E293B;
      cursor: pointer;
    }
    .doc-title:hover { color: #2563EB; }
    .doc-type {
      font-size: 12px;
      color: #94A3B8;
    }
    .status-chip {
      display: inline-block;
      padding: 3px 10px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
    }
    .uploader-cell {
      display: flex;
      align-items: center;
    }
    .uploader-name {
      font-size: 13px;
      color: #1E293B;
      font-weight: 500;
    }
    .date-text {
      font-size: 13px;
      color: #64748B;
    }
    .table-row {
      cursor: pointer;
      transition: background 0.15s;
    }
    .table-row:hover { background: #F8FAFC; }
    th.mat-header-cell {
      font-size: 12px;
      font-weight: 600;
      color: #64748B;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }
  `],
})
export class DocumentListComponent implements OnInit {
  docService = inject(DocumentService);
  authService     = inject(AuthService);
  workflowService = inject(WorkflowService);
  private router  = inject(Router);
  private snackBar = inject(MatSnackBar);
  private dialog   = inject(MatDialog);

  displayedColumns = ["title", "status", "uploadedByName", "createdAt", "actions"];

  documents    = signal<Document[]>([]);
  totalCount   = signal(0);
  currentPage  = signal(1);
  isLoading    = signal(false);

  pageSize       = 10;
  searchTerm     = "";
  selectedStatus = "";

  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    // Debounce search input
    this.searchSubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage.set(1);
      this.loadDocuments();
    });

    this.loadDocuments();
  }

  loadDocuments(): void {
    this.isLoading.set(true);

    const filter: DocumentFilter = {
      page:     this.currentPage(),
      pageSize: this.pageSize,
      status:   this.selectedStatus || undefined,
      search:   this.searchTerm || undefined,
    };

    this.docService.getDocuments(filter).subscribe({
      next: (response: any) => {
        this.documents.set(response.items ?? []);
        this.totalCount.set(response.totalCount ?? 0);
        this.isLoading.set(false);
      },
      error: () => {
        this.documents.set([]);
        this.totalCount.set(0);
        this.isLoading.set(false);
      },
    });
  }

  onSearchChange(term: string): void {
    this.searchSubject.next(term);
  }

  onStatusChange(status: string): void {
    this.currentPage.set(1);
    this.loadDocuments();
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.pageIndex + 1);
    this.pageSize = event.pageSize;
    this.loadDocuments();
  }

  clearFilters(): void {
    this.searchTerm     = "";
    this.selectedStatus = "";
    this.currentPage.set(1);
    this.loadDocuments();
  }

  archiveDocument(doc: Document): void {
    this.docService.archiveDocument(doc.id).subscribe({
      next: () => {
        this.snackBar.open(
          `"${doc.title}" archived`, "Close",
          { duration: 3000 });
        this.loadDocuments();
      },
      error: () => {
        this.snackBar.open(
          "Failed to archive document", "Close",
          { duration: 3000 });
      },
    });
  }

  previewDocument(doc: Document): void {
    this.dialog.open(PreviewDialogComponent, {
      data: { document: doc },
      width: "600px"
    });
  }

  downloadDocument(doc: Document): void {
    this.docService.downloadDocument(doc.id).subscribe({
      next: (blob: Blob) => {
        this.docService.saveBlobAsFile(
          blob,
          this.docService.buildDownloadFileName(
            doc.title,
            doc.mimeType
          )
        );
      },
      error: () => {
        this.snackBar.open("Failed to download document", "Close", { duration: 3000 });
      }
    });
  }

  updateStatus(doc: Document): void {
    this.workflowService.getWorkflowByDocumentId(doc.id).subscribe({
      next: (wf: any) => {
        this.router.navigate(["/workflow", wf.id]);
      },
      error: (err: any) => {
        console.error("Workflow not found", err);
        this.snackBar.open("No active workflow found for this document", "Close", { duration: 3000 });
      }
    });
  }

  getStatusColor(status: string): string {
    return DocumentStatusColors[status as DocumentStatus]
      ?? "#64748B";
  }

  getStatusLabel(status: string): string {
    return DocumentStatusLabels[status as DocumentStatus]
      ?? status;
  }

  getTypeColor(mimeType: string): string {
    const colors: Record<string, string> = {
      "application/pdf":   "#DC2626",
      "image/jpeg":        "#2563EB",
      "image/png":         "#2563EB",
      "text/plain":        "#64748B",
    };
    return colors[mimeType] ?? "#7C3AED";
  }
}
