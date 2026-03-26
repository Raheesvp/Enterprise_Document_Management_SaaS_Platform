export interface Document {
  id:               string;
  tenantId:         string;
  title:            string;
  status:           DocumentStatus;
  mimeType:         string;
  fileSizeBytes:    number;
  storagePath?:     string;
  uploadedByUserId: string;
  uploadedByName:   string;
  createdAt:        string;
  updatedAt:        string;
  currentVersion : number;
}

export interface DocumentVersion {
  id:               string;
  documentId:       string;
  versionNumber:    number;
  storagePath:      string;
  createdAt:        string;
  createdByUserId:  string;
}

export interface DocumentListResponse {
  items:      Document[];
  totalCount: number;
  page:       number;
  pageSize:   number;
  totalPages: number;
}

export interface DocumentFilter {
  status?:   string;
  search?:   string;
  page:      number;
  pageSize:  number;
}

export type DocumentStatus =
  | "Uploading"
  | "Processing"
  | "Active"
  | "UnderReview"
  | "Approved"
  | "Rejected"
  | "Archived";

export const DocumentStatusLabels: Record<DocumentStatus, string> = {
  Uploading:   "Uploading",
  Processing:  "Processing",
  Active:      "Active",
  UnderReview: "Under Review",
  Approved:    "Approved",
  Rejected:    "Rejected",
  Archived:    "Archived",
};

export const DocumentStatusColors: Record<DocumentStatus, string> = {
  Uploading:   "#94A3B8",
  Processing:  "#EA580C",
  Active:      "#2563EB",
  UnderReview: "#D97706",
  Approved:    "#16A34A",
  Rejected:    "#DC2626",
  Archived:    "#7C3AED",
};
