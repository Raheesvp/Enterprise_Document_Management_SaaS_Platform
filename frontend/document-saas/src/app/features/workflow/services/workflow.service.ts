import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import {
  WorkflowInstance,
  WorkflowDefinition,
} from "../models/workflow.models";

@Injectable({ providedIn: "root" })
export class WorkflowService {
  private http = inject(HttpClient);
  private base = `${environment.workflowUrl}/api/workflow`;

  getWorkflow(id: string): Observable<WorkflowInstance> {
    return this.http.get<WorkflowInstance>(
      `${this.base}/${id}`);
  }

  getWorkflowByDocumentId(documentId: string): Observable<WorkflowInstance> {
    return this.http.get<WorkflowInstance>(
      `${this.base}/document/${documentId}`);
  }

  approveStage(
    workflowId: string,
    comments: string
  ): Observable<WorkflowInstance> {
    return this.http.post<WorkflowInstance>(
      `${this.base}/${workflowId}/approve`,
      { comments });
  }

  rejectStage(
    workflowId: string,
    comments: string
  ): Observable<WorkflowInstance> {
    return this.http.post<WorkflowInstance>(
      `${this.base}/${workflowId}/reject`,
      { comments });
  }

  getDefinitions(): Observable<WorkflowDefinition[]> {
    return this.http.get<WorkflowDefinition[]>(
      `${this.base}/definitions`);
  }

  isOverdue(slaDeadline: string): boolean {
    return new Date() > new Date(slaDeadline);
  }

  getDaysRemaining(slaDeadline: string): number {
    const diff = new Date(slaDeadline).getTime()
               - new Date().getTime();
    return Math.ceil(diff / (1000 * 60 * 60 * 24));
  }
}
