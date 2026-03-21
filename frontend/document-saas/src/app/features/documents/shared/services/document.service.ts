import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../../../environments/environment";
import {
  DocumentListResponse,
  DocumentDetail,
  DocumentFilter,
} from "../models/document.models";

@Injectable({ providedIn: "root" })
export class DocumentService {
  private http = inject(HttpClient);
  private base = `${environment.documentUrl}/api/documents`;

  getDocuments(filter: DocumentFilter): Observable<DocumentListResponse> {
    let params = new HttpParams()
      .set("page",     filter.page.toString())
      .set("pageSize", filter.pageSize.toString());

    if (filter.search)   params = params.set("search",   filter.search);
    if (filter.status)   params = params.set("status",   filter.status);
    if (filter.dateFrom) params = params.set("dateFrom", filter.dateFrom);
    if (filter.dateTo)   params = params.set("dateTo",   filter.dateTo);

    return this.http.get<DocumentListResponse>(this.base, { params });
  }

  getDocumentById(id: string): Observable<DocumentDetail> {
    return this.http.get<DocumentDetail>(`${this.base}/${id}`);
  }

  deleteDocument(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  downloadDocument(id: string): Observable<Blob> {
    return this.http.get(`${this.base}/${id}/download`, {
      responseType: "blob",
    });
  }
}
