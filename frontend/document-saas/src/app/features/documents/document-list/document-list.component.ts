import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatCardModule } from "@angular/material/card";

@Component({
  selector: "app-document-list",
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div style="padding:24px">
      <h2>Documents</h2>
      <p style="color:#64748B">
        Document list coming in Day 31.
      </p>
    </div>
  `,
})
export class DocumentListComponent {}
