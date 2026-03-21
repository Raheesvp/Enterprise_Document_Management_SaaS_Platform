export interface Notification {
  id:            string;
  tenantId:      string;
  userId:        string;
  title:         string;
  message:       string;
  type:          NotificationType;
  status:        NotificationStatus;
  referenceId?:  string;
  referenceType?: string;
  createdAt:     string;
  readAt?:       string;
}

export type NotificationType =
  | "WorkflowStarted"
  | "StageAssigned"
  | "StageApproved"
  | "StageRejected"
  | "WorkflowCompleted"
  | "WorkflowEscalated"
  | "DocumentUploaded"
  | "DocumentParsed";

export type NotificationStatus =
  | "Unread"
  | "Read"
  | "Archived";

export const NotificationIcons: Record<NotificationType, string> = {
  WorkflowStarted:   "account_tree",
  StageAssigned:     "assignment_ind",
  StageApproved:     "check_circle",
  StageRejected:     "cancel",
  WorkflowCompleted: "task_alt",
  WorkflowEscalated: "warning",
  DocumentUploaded:  "upload_file",
  DocumentParsed:    "document_scanner",
};

export const NotificationColors: Record<NotificationType, string> = {
  WorkflowStarted:   "#2563EB",
  StageAssigned:     "#EA580C",
  StageApproved:     "#16A34A",
  StageRejected:     "#DC2626",
  WorkflowCompleted: "#16A34A",
  WorkflowEscalated: "#DC2626",
  DocumentUploaded:  "#7C3AED",
  DocumentParsed:    "#0D9488",
};
