import {
  Component,
  signal,
  inject,
  OnInit,
} from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterLink } from "@angular/router";
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from "@angular/forms";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTableModule } from "@angular/material/table";
import { MatChipsModule } from "@angular/material/chips";
import { MatDividerModule } from "@angular/material/divider";
import { MatSnackBar, MatSnackBarModule } from "@angular/material/snack-bar";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { HttpClient } from "@angular/common/http";
import { environment } from "../../../environments/environment";
import { AuthService } from "../../core/services/auth.service";

interface TenantUser {
  userId:    string;
  email:     string;
  fullName:  string;
  role:      string;
  isActive:  boolean;
  createdAt: string;
}

@Component({
  selector: "app-user-management",
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatChipsModule,
    MatDividerModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <div class="page-container">

      <!-- Header -->
      <div class="page-header">
        <div>
          <h1>User Management</h1>
          <p>Add and manage users in your organization</p>
        </div>
      </div>

      <!-- Add User Form -->
      <mat-card class="form-card">
        <mat-card-header>
          <h2>Add New User</h2>
          <p>Invite a Manager or Viewer to your organization</p>
        </mat-card-header>
        <mat-divider></mat-divider>
        <mat-card-content>
          <form [formGroup]="addUserForm"
            (ngSubmit)="onAddUser()"
            class="user-form">

            <mat-form-field appearance="outline">
              <mat-label>Full Name</mat-label>
              <input matInput formControlName="fullName"
                placeholder="John Smith"/>
              <mat-icon matSuffix>person</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Email</mat-label>
              <input matInput type="email"
                formControlName="email"
                placeholder="john@company.com"/>
              <mat-icon matSuffix>email</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Password</mat-label>
              <input matInput type="password"
                formControlName="password"
                placeholder="Min 8 chars"/>
              <mat-icon matSuffix>lock</mat-icon>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Role</mat-label>
              <mat-select formControlName="role">
                <mat-option value="Manager">
                  Manager — Can upload, approve, reject
                </mat-option>
                <mat-option value="Viewer">
                  Viewer — Read only access
                </mat-option>
              </mat-select>
              <mat-icon matSuffix>admin_panel_settings</mat-icon>
            </mat-form-field>

            @if (errorMessage()) {
              <div class="error-banner">
                <mat-icon>error_outline</mat-icon>
                {{ errorMessage() }}
              </div>
            }

            <button mat-raised-button color="primary"
              type="submit"
              [disabled]="isAdding() || addUserForm.invalid">
              @if (isAdding()) {
                <mat-spinner diameter="20"></mat-spinner>
                Adding...
              } @else {
                <mat-icon>person_add</mat-icon>
                Add User
              }
            </button>

          </form>
        </mat-card-content>
      </mat-card>

      <!-- Users Table -->
      <mat-card class="users-card">
        <mat-card-header>
          <h2>Team Members ({{ users().length }})</h2>
        </mat-card-header>
        <mat-divider></mat-divider>
        <mat-card-content>

          @if (isLoading()) {
            <div class="loading">
              <mat-spinner diameter="32"></mat-spinner>
            </div>
          }

          @if (!isLoading() && users().length === 0) {
            <div class="empty">
              <p>No users yet. Add your first team member above.</p>
            </div>
          }

          @if (!isLoading() && users().length > 0) {
            <table mat-table [dataSource]="users()"
              class="users-table">

              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>Name</th>
                <td mat-cell *matCellDef="let user">
                  <div class="user-cell">
                    <div class="user-avatar">
                      {{ getInitials(user.fullName) }}
                    </div>
                    <div>
                      <span class="user-name">
                        {{ user.fullName }}
                      </span>
                      <span class="user-email">
                        {{ user.email }}
                      </span>
                    </div>
                  </div>
                </td>
              </ng-container>

              <ng-container matColumnDef="role">
                <th mat-header-cell *matHeaderCellDef>Role</th>
                <td mat-cell *matCellDef="let user">
                  <span class="role-badge"
                    [class]="'role-' + user.role.toLowerCase()">
                    {{ user.role }}
                  </span>
                </td>
              </ng-container>

              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let user">
                  <span class="status-badge"
                    [class]="user.isActive ? 'active' : 'inactive'">
                    {{ user.isActive ? "Active" : "Inactive" }}
                  </span>
                </td>
              </ng-container>

              <tr mat-header-row
                *matHeaderRowDef="displayedColumns">
              </tr>
              <tr mat-row
                *matRowDef="let row; columns: displayedColumns;">
              </tr>

            </table>
          }

        </mat-card-content>
      </mat-card>

    </div>
  `,
  styles: [`
    .page-container {
      padding: 24px;
      max-width: 900px;
      margin: 0 auto;
    }
    .page-header {
      margin-bottom: 24px;
    }
    .page-header h1 {
      font-size: 24px;
      font-weight: 700;
      color: #1E293B;
      margin: 0 0 4px;
    }
    .page-header p {
      color: #64748B;
      margin: 0;
      font-size: 14px;
    }
    .form-card, .users-card {
      border-radius: 12px;
      margin-bottom: 20px;
    }
    .form-card h2, .users-card h2 {
      font-size: 16px;
      font-weight: 600;
      color: #1E293B;
      margin: 0 0 4px;
      padding: 16px;
    }
    .form-card p {
      font-size: 13px;
      color: #64748B;
      margin: 0;
      padding: 0 16px 16px;
    }
    .user-form {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
      padding: 16px 0;
    }
    .user-form mat-form-field {
      width: 100%;
    }
    .user-form button {
      grid-column: 1 / -1;
      height: 44px;
      display: flex;
      align-items: center;
      gap: 8px;
      width: fit-content;
    }
    .error-banner {
      grid-column: 1 / -1;
      display: flex;
      align-items: center;
      gap: 8px;
      background: #FEF2F2;
      color: #DC2626;
      padding: 10px 14px;
      border-radius: 8px;
      font-size: 14px;
    }
    .loading, .empty {
      display: flex;
      justify-content: center;
      padding: 32px;
      color: #64748B;
    }
    .users-table { width: 100%; }
    .user-cell {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 8px 0;
    }
    .user-avatar {
      width: 36px;
      height: 36px;
      min-width: 36px;
      border-radius: 50%;
      background: #2563EB;
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 12px;
      font-weight: 700;
    }
    .user-name {
      display: block;
      font-size: 14px;
      font-weight: 500;
      color: #1E293B;
    }
    .user-email {
      display: block;
      font-size: 12px;
      color: #94A3B8;
    }
    .role-badge {
      padding: 3px 10px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
    }
    .role-admin   { background: #FEF3C7; color: #D97706; }
    .role-manager { background: #F0FDF4; color: #16A34A; }
    .role-viewer  { background: #F1F5F9; color: #64748B; }
    .status-badge {
      padding: 3px 10px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
    }
    .active   { background: #F0FDF4; color: #16A34A; }
    .inactive { background: #FEF2F2; color: #DC2626; }
    @media (max-width: 600px) {
      .user-form { grid-template-columns: 1fr; }
    }
  `],
})
export class UserManagementComponent implements OnInit {
  private http     = inject(HttpClient);
  private fb       = inject(FormBuilder);
  private snackBar = inject(MatSnackBar);
  authService      = inject(AuthService);

  users        = signal<TenantUser[]>([]);
  isLoading    = signal(true);
  isAdding     = signal(false);
  errorMessage = signal("");

  displayedColumns = ["name", "role", "status"];

  addUserForm: FormGroup;

  constructor() {
    this.addUserForm = this.fb.group({
      fullName: ["", Validators.required],
      email:    ["", [Validators.required, Validators.email]],
      password: ["", [Validators.required, Validators.minLength(8)]],
      role:     ["Viewer", Validators.required],
    });
  }

  ngOnInit(): void {
    this.loadUsers();
  }

  private loadUsers(): void {
    this.http.get<TenantUser[]>(
      `${environment.identityUrl}/api/identity/users`)
      .subscribe({
        next:  (users) => {
          this.users.set(Array.isArray(users) ? users : []);
          this.isLoading.set(false);
        },
        error: () => {
          this.users.set([]);
          this.isLoading.set(false);
        },
      });
  }

  onAddUser(): void {
    if (this.addUserForm.invalid) return;

    this.isAdding.set(true);
    this.errorMessage.set("");

    this.http.post<any>(
      `${environment.identityUrl}/api/identity/users`,
      this.addUserForm.value)
      .subscribe({
        next: (newUser) => {
          this.users.update(u => [...u, {
            userId:    newUser.userId,
            email:     this.addUserForm.value.email,
            fullName:  this.addUserForm.value.fullName,
            role:      newUser.role,
            isActive:  true,
            createdAt: new Date().toISOString(),
          }]);
          this.addUserForm.reset({ role: "Viewer" });
          this.isAdding.set(false);
          this.snackBar.open(
            "User added successfully", "Close",
            { duration: 3000 });
        },
        error: (err) => {
          this.isAdding.set(false);
          this.errorMessage.set(
            err.error?.error ?? "Failed to add user");
        },
      });
  }

  getInitials(name: string): string {
    return name.split(" ").map(n => n[0])
      .join("").toUpperCase().slice(0, 2);
  }
}
