import { Component, OnInit, inject, signal, HostListener } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AdminApiService } from '../../core/services/admin-api.service';
import { AdminAuthService } from '../../core/services/admin-auth.service';
import { AdminPermissions, ApiResponse } from '../../shared/models/admin.models';

interface PlatformAdminSummary {
  id: string;
  email: string;
  name: string;
  permissions: string[];
  isActive: boolean;
  grantedBy: string;
  grantedAt: string;
  lastLoginAt: string | null;
}

interface PlatformAdminList {
  admins: PlatformAdminSummary[];
  total: number;
}

const ALL_PERMISSIONS = Object.values(AdminPermissions);

@Component({
  selector: 'admin-platform-admins',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './platform-admins.component.html',
  styleUrl: './platform-admins.component.css',
})
export class PlatformAdminsComponent implements OnInit {
  private api  = inject(AdminApiService);
  auth         = inject(AdminAuthService);

  admins  = signal<PlatformAdminSummary[]>([]);
  total   = signal(0);
  loading = signal(false);
  loadError = signal<string | null>(null);

  // Create form
  showCreateForm  = signal(false);
  createEmail     = signal('');
  createName      = signal('');
  createPerms     = signal<string[]>([]);
  createSaving    = signal(false);
  createError     = signal<string | null>(null);

  // Edit panel
  editTarget = signal<PlatformAdminSummary | null>(null);
  editName   = signal('');
  editPerms  = signal<string[]>([]);
  editSaving = signal(false);
  editError  = signal<string | null>(null);

  // Permission popover
  permPopover = signal<{ top: number; left: number; permissions: string[] } | null>(null);

  allPermissions = ALL_PERMISSIONS;

  @HostListener('document:click')
  onDocumentClick() { this.permPopover.set(null); }

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.loadError.set(null);
    this.api.get<ApiResponse<PlatformAdminList>>('/admin/platform-admins').subscribe({
      next: r => {
        this.admins.set(r.data?.admins ?? []);
        this.total.set(r.data?.total ?? 0);
        this.loading.set(false);
      },
      error: (err: any) => {
        this.loadError.set(err?.error?.message ?? `Failed to load admins (${err?.status ?? 'network error'})`);
        this.loading.set(false);
      },
    });
  }

  // ---- Create ----
  openCreate() {
    this.createEmail.set(''); this.createName.set('');
    this.createPerms.set([]); this.createError.set(null);
    this.showCreateForm.set(true);
  }

  toggleCreatePerm(p: string) {
    this.createPerms.update(list => list.includes(p) ? list.filter(x => x !== p) : [...list, p]);
  }

  submitCreate() {
    if (!this.createEmail().trim() || !this.createName().trim()) {
      this.createError.set('Email and name are required.');
      return;
    }
    this.createSaving.set(true);
    this.createError.set(null);
    this.api.post<ApiResponse<PlatformAdminSummary>>('/admin/platform-admins', {
      email: this.createEmail(),
      name: this.createName(),
      permissions: this.createPerms(),
    }).subscribe({
      next: r => {
        if (r.data) this.admins.update(list => [r.data!, ...list]);
        this.total.update(t => t + 1);
        this.showCreateForm.set(false);
        this.createSaving.set(false);
      },
      error: err => {
        this.createError.set(err.error?.message ?? 'Failed to create admin.');
        this.createSaving.set(false);
      },
    });
  }

  // ---- Edit ----
  openEdit(admin: PlatformAdminSummary) {
    this.editTarget.set(admin);
    this.editName.set(admin.name);
    this.editPerms.set([...admin.permissions]);
    this.editError.set(null);
  }

  toggleEditPerm(p: string) {
    this.editPerms.update(list => list.includes(p) ? list.filter(x => x !== p) : [...list, p]);
  }

  submitEdit() {
    const target = this.editTarget();
    if (!target) return;
    this.editSaving.set(true);
    this.editError.set(null);
    this.api.patch<ApiResponse<PlatformAdminSummary>>(`/admin/platform-admins/${target.id}`, {
      name: this.editName(),
      permissions: this.editPerms(),
    }).subscribe({
      next: r => {
        if (r.data) this.admins.update(list => list.map(a => a.id === target.id ? r.data! : a));
        this.editTarget.set(null);
        this.editSaving.set(false);
      },
      error: err => {
        this.editError.set(err.error?.message ?? 'Update failed.');
        this.editSaving.set(false);
      },
    });
  }

  // ---- Toggle active ----
  toggleActive(admin: PlatformAdminSummary) {
    const newState = !admin.isActive;
    const label = newState ? 'activate' : 'deactivate';
    if (!confirm(`${label.charAt(0).toUpperCase() + label.slice(1)} ${admin.email}?`)) return;

    this.api.patch<ApiResponse<object>>(`/admin/platform-admins/${admin.id}/active`, { isActive: newState }).subscribe({
      next: () => this.admins.update(list => list.map(a => a.id === admin.id ? { ...a, isActive: newState } : a)),
    });
  }

  isCurrentAdmin(id: string): boolean {
    return this.auth.currentAdmin()?.id === id;
  }

  openPermPopover(admin: PlatformAdminSummary, event: MouseEvent) {
    event.stopPropagation();
    if (this.permPopover()) { this.permPopover.set(null); return; }
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const left = Math.min(rect.left, window.innerWidth - 192);
    this.permPopover.set({
      top:  rect.bottom + 6,
      left: Math.max(8, left),
      permissions: admin.permissions,
    });
  }
}
