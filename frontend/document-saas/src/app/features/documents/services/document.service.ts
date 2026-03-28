import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
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

  downloadFromUrl(url: string, fileName: string): void {
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.target = "_blank"; // Ensure it doesn't navigate away
    anchor.style.display = "none";
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
  }

  saveBlobAsFile(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.style.display = "none";
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    window.URL.revokeObjectURL(url);
  }

  buildDownloadFileName(title: string, mimeType: string, suffix = ""): string {
    const safeTitle = title?.trim() || "document";
    const extension = this.getExtensionFromMimeType(mimeType);

    if (safeTitle.includes(".")) {
      const lastDotIndex = safeTitle.lastIndexOf(".");
      const name = safeTitle.slice(0, lastDotIndex);
      const existingExtension = safeTitle.slice(lastDotIndex);
      return `${name}${suffix}${existingExtension}`;
    }

    if (!extension) {
      return `${safeTitle}${suffix}`;
    }

    return `${safeTitle}${suffix}${extension}`;
  }

  private getExtensionFromMimeType(mimeType: string): string {
    const map: Record<string, string> = {
      "application/pdf": ".pdf",
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document": ".docx",
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet": ".xlsx",
      "image/jpeg": ".jpg",
      "image/png": ".png",
      "text/plain": ".txt",
    };

    return map[mimeType] ?? "";
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
