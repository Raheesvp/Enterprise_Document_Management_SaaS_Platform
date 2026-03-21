import {
  Component,
  Output,
  EventEmitter,
  OnInit,
  OnDestroy,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
} from "@angular/forms";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatNativeDateModule } from "@angular/material/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { Subject, debounceTime, takeUntil } from "rxjs";
import { DocumentFilter, DocumentStatus } from "../../../shared/models/document.models";

@Component({
  selector: "app-filter-bar",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatIconModule,
  ],
  template: `
    <div class="filter-bar" [formGroup]="filterForm">

      <!-- Search -->
      <mat-form-field appearance="outline" class="filter-search">
        <mat-label>Search documents</mat-label>
        <input matInput formControlName="search" placeholder="Title..."/>
        <mat-icon matSuffix>search</mat-icon>
      </mat-form-field>

      <!-- Status -->
      <mat-form-field appearance="outline" class="filter-status">
        <mat-label>Status</mat-label>
        <mat-select formControlName="status">
          <mat-option value="">All</mat-option>
          <mat-option value="Draft">Draft</mat-option>
          <mat-option value="Pending">Pending</mat-option>
          <mat-option value="Approved">Approved</mat-option>
          <mat-option value="Rejected">Rejected</mat-option>
          <mat-option value="Processing">Processing</mat-option>
        </mat-select>
      </mat-form-field>

      <!-- Date From -->
      <mat-form-field appearance="outline" class="filter-date">
        <mat-label>From date</mat-label>
        <input matInput [matDatepicker]="fromPicker" formControlName="dateFrom"/>
        <mat-datepicker-toggle matSuffix [for]="fromPicker"></mat-datepicker-toggle>
        <mat-datepicker #fromPicker></mat-datepicker>
      </mat-form-field>

      <!-- Date To -->
      <mat-form-field appearance="outline" class="filter-date">
        <mat-label>To date</mat-label>
        <input matInput [matDatepicker]="toPicker" formControlName="dateTo"/>
        <mat-datepicker-toggle matSuffix [for]="toPicker"></mat-datepicker-toggle>
        <mat-datepicker #toPicker></mat-datepicker>
      </mat-form-field>

      <!-- Clear -->
      <button mat-stroked-button (click)="clearFilters()" class="clear-btn">
        <mat-icon>clear</mat-icon>
        Clear
      </button>

    </div>
  `,
  styles: [`
    .filter-bar {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-wrap: wrap;
      padding: 16px;
      background: white;
      border-radius: 12px;
      border: 1px solid #E2E8F0;
      margin-bottom: 16px;
    }
    .filter-search { flex: 1; min-width: 200px; }
    .filter-status { width: 140px; }
    .filter-date   { width: 150px; }
    .clear-btn     { height: 56px; color: #64748B; }
    mat-form-field { margin-bottom: -1.25em; }
  `],
})
export class FilterBarComponent implements OnInit, OnDestroy {
  @Output() filterChanged = new EventEmitter<DocumentFilter>();

  filterForm: FormGroup;
  private destroy$ = new Subject<void>();

  constructor(private fb: FormBuilder) {
    this.filterForm = this.fb.group({
      search:   [""],
      status:   [""],
      dateFrom: [null],
      dateTo:   [null],
    });
  }

  ngOnInit(): void {
    this.filterForm.valueChanges
      .pipe(debounceTime(400), takeUntil(this.destroy$))
      .subscribe(() => this.emitFilter());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  clearFilters(): void {
    this.filterForm.reset({ search: "", status: "", dateFrom: null, dateTo: null });
  }

  private emitFilter(): void {
    const v = this.filterForm.value;
    this.filterChanged.emit({
      search:   v.search   || undefined,
      status:   v.status   || undefined,
      dateFrom: v.dateFrom ? new Date(v.dateFrom).toISOString() : undefined,
      dateTo:   v.dateTo   ? new Date(v.dateTo).toISOString()   : undefined,
      page:     1,
      pageSize: 10,
    });
  }
}
