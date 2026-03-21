// DTOs matching DocumentService.API responses exactly

export type DocumentStatus =
  | 'Draft'
  | 'Pending'
  | 'Approved'
  | 'Rejected'
  | 'Processing';

export interface DocumentListItem {
  id:            string;
  title:         string;
  status:        DocumentStatus;
  mimeType:      string;
  fileSizeBytes: number;
  createdAt:     string;
  updatedAt:     string;
  uploadedBy:    string;
}

export interface DocumentDetail {
  id:            string;
  title:         string;
  status:        DocumentStatus;
  mimeType:      string;
  fileSizeBytes: number;
  storagePath:   string;
  createdAt:     string;
  updatedAt:     string;
  uploadedBy:    string;
  versions:      DocumentVersion[];
}

export interface DocumentVersion {
  id:            string;
  versionNumber: number;
  storagePath:   string;
  fileSizeBytes: number;
  createdAt:     string;
  createdBy:     string;
}

export interface DocumentListResponse {
  items:      DocumentListItem[];
  totalCount: number;
  page:       number;
  pageSize:   number;
}

export interface DocumentFilter {
  search?:    string;
  status?:    DocumentStatus | '';
  dateFrom?:  string;
  dateTo?:    string;
  page:       number;
  pageSize:   number;
}
