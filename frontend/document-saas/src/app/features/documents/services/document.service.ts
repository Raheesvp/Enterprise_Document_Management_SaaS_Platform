import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams, HttpHeaders } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import {
  Document,
  DocumentListResponse,
  DocumentFilter,
  DocumentVersion,
} from "../models/document.models";

@Injectable({ providedIn: "root" })
export class DocumentService {
  private http = inject(HttpClient);
  private base = `${environment.documentUrl}/api/documents`;

  getDocuments(filter: DocumentFilter): Observable<DocumentListResponse> {
    let params = new HttpParams()
      .set("page",     filter.page.toString())
      .set("pageSize", filter.pageSize.toString());

    if (filter.status) {
      params = params.set("status", filter.status);
    }
    if (filter.search) {
      params = params.set("searchTerm", filter.search);
    }

    return this.http.get<DocumentListResponse>(
      this.base, { params });
  }

  getDocument(id: string): Observable<Document> {
    return this.http.get<Document>(`${this.base}/${id}`);
  }

  getVersions(documentId: string): Observable<DocumentVersion[]> {
    return this.http.get<DocumentVersion[]>(
      `${this.base}/${documentId}/versions`);
  }

  archiveDocument(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.base}/${id}/archive`, {});
  }

  downloadDocument(id: string): Observable<Blob> {
    return this.http.get(`${this.base}/${id}/download`, {
      responseType: "blob",
    });
  }

  downloadVersion(versionId: string): Observable<Blob> {
    return this.http.get(`${this.base}/versions/${versionId}/download`, {
      responseType: "blob",
    });
  }

  formatFileSize(bytes: number): string {
    if (!bytes || bytes === 0) return "0 B";
    const k     = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i     = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
  }

  formatMimeType(mimeType: string): string {
    if (!mimeType) return "File";
    const map: Record<string, string> = {
      "application/pdf": "PDF",
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document": "Word",
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": "Excel",
      "image/jpeg": "JPEG",
      "image/png":  "PNG",
      "text/plain": "Text",
    };
    return map[mimeType] ?? mimeType.split("/")[1]?.toUpperCase() ?? "File";
  }
}
