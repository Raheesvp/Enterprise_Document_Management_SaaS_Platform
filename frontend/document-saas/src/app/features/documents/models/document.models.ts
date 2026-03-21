export interface Document {
  id:             string;
  tenantId:       string;
  title:          string;
  status:         DocumentStatus;
  mimeType:       string;
  fileSizeBytes:  number;
  storagePath:    string;
  uploadedByUserId: string;
  createdAt:      string;
  updatedAt:      string;
}

export interface DocumentVersion {
  id:            string;
  documentId:    string;
  versionNumber: number;
  storagePath:   string;
  createdAt:     string;
  createdByUserId: string;
}

export interface DocumentListResponse {
  items:       Document[];
  totalCount:  number;
  page:        number;
  pageSize:    number;
  totalPages:  number;
}

export interface DocumentFilter {
  status?:   string;
  search?:   string;
  page:      number;
  pageSize:  number;
}

export type DocumentStatus =
  | "Draft"
  | "UnderReview"
  | "Approved"
  | "Rejected"
  | "Archived";

export const DocumentStatusLabels: Record<DocumentStatus, string> = {
  Draft:       "Draft",
  UnderReview: "Under Review",
  Approved:    "Approved",
  Rejected:    "Rejected",
  Archived:    "Archived",
};

export const DocumentStatusColors: Record<DocumentStatus, string> = {
  Draft:       "#64748B",
  UnderReview: "#EA580C",
  Approved:    "#16A34A",
  Rejected:    "#DC2626",
  Archived:    "#7C3AED",
};
