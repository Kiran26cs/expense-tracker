import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '../modal/modal.component';
import { ButtonComponent } from '../button/button.component';
import { InputComponent } from '../input/input.component';
import { SelectComponent } from '../input/select.component';
import {
  ExpenseBookMember,
  InviteMemberRequest,
  MemberRole,
  PagePermissions,
  UpdateMemberRequest,
} from '../../models/member.model';
import { Category } from '../../models/expense.model';

export type ModalMode = 'add' | 'edit';

interface PageRow {
  key: keyof PagePermissions;
  label: string;
  options: { value: string; label: string }[];
}

// Permissions preset per role
const ROLE_DEFAULTS: Record<MemberRole, {
  permissions: Required<PagePermissions>;
  canDeleteExpenses: boolean;
}> = {
  owner: {
    permissions: { dashboard: 'view', expenses: 'write', budgets: 'write', settings: 'write', insights: 'view' },
    canDeleteExpenses: true,
  },
  admin: {
    permissions: { dashboard: 'view', expenses: 'write', budgets: 'write', settings: 'view', insights: 'view' },
    canDeleteExpenses: true,
  },
  member: {
    permissions: { dashboard: 'view', expenses: 'write', budgets: 'none', settings: 'none', insights: 'view' },
    canDeleteExpenses: false,
  },
  viewer: {
    permissions: { dashboard: 'view', expenses: 'view', budgets: 'none', settings: 'none', insights: 'view' },
    canDeleteExpenses: false,
  },
};

@Component({
  selector: 'app-member-permission-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent, ButtonComponent, InputComponent, SelectComponent],
  templateUrl: './member-permission-modal.component.html',
  styleUrl: './member-permission-modal.component.css',
})
export class MemberPermissionModalComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() mode: ModalMode = 'add';
  @Input() member: ExpenseBookMember | null = null;
  @Input() categories: Category[] = [];
  @Input() loading = false;

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<{ request: InviteMemberRequest | UpdateMemberRequest; memberId?: string }>();

  // Form state
  email = '';
  role: MemberRole = 'member';
  permissions: Required<PagePermissions> = { ...ROLE_DEFAULTS.member.permissions };
  canDeleteExpenses = false;
  allowedCategoryIds: Set<string> = new Set();

  readonly roleOptions = [
    { value: 'owner',  label: 'Owner — full access, can promote others' },
    { value: 'admin',  label: 'Admin — full access, can manage members' },
    { value: 'member', label: 'Member — can view & add expenses' },
    { value: 'viewer', label: 'Viewer — read only' },
  ];

  readonly pageRows: PageRow[] = [
    { key: 'dashboard', label: 'Dashboard',  options: [{ value: 'view', label: 'View' }, { value: 'none', label: 'None' }] },
    { key: 'expenses',  label: 'Expenses',   options: [{ value: 'write', label: 'Write' }, { value: 'view', label: 'View' }, { value: 'none', label: 'None' }] },
    { key: 'budgets',   label: 'Budgets',    options: [{ value: 'write', label: 'Write' }, { value: 'view', label: 'View' }, { value: 'none', label: 'None' }] },
    { key: 'settings',  label: 'Settings',   options: [{ value: 'write', label: 'Write' }, { value: 'view', label: 'View' }, { value: 'none', label: 'None' }] },
    { key: 'insights',  label: 'Fin. Tools', options: [{ value: 'view', label: 'View' }, { value: 'none', label: 'None' }] },
  ];

  get isOwnerRole() { return this.role === 'owner'; }
  get title() { return this.mode === 'add' ? 'Invite Member' : 'Edit Member'; }
  get allCategoriesChecked() { return this.categories.every(c => this.allowedCategoryIds.has(c.id)); }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['isOpen'] && this.isOpen) {
      this.initForm();
    }
  }

  private initForm() {
    if (this.mode === 'edit' && this.member) {
      this.email = this.member.invitedEmail;
      this.role  = this.member.role;
      const p = this.member.permissions;
      this.permissions = {
        dashboard: (p?.dashboard ?? ROLE_DEFAULTS[this.role].permissions.dashboard) as any,
        expenses:  (p?.expenses  ?? ROLE_DEFAULTS[this.role].permissions.expenses)  as any,
        budgets:   (p?.budgets   ?? ROLE_DEFAULTS[this.role].permissions.budgets)   as any,
        settings:  (p?.settings  ?? ROLE_DEFAULTS[this.role].permissions.settings)  as any,
        insights:  (p?.insights  ?? ROLE_DEFAULTS[this.role].permissions.insights)  as any,
      };
      this.canDeleteExpenses = this.member.canDeleteExpenses;
      this.allowedCategoryIds = new Set(this.member.allowedCategoryIds ?? []);
    } else {
      this.email = '';
      this.role  = 'member';
      this.applyRolePreset('member');
    }
  }

  onRoleChange(val: string) {
    this.role = val as MemberRole;
    this.applyRolePreset(this.role);
  }

  private applyRolePreset(role: MemberRole) {
    const preset = ROLE_DEFAULTS[role];
    this.permissions = { ...preset.permissions };
    this.canDeleteExpenses = preset.canDeleteExpenses;
    // For non-owner roles, default all available categories as checked
    if (role !== 'owner') {
      this.allowedCategoryIds = new Set(this.categories.map(c => c.id));
    } else {
      this.allowedCategoryIds = new Set();
    }
  }

  toggleCategory(id: string) {
    if (this.allowedCategoryIds.has(id)) this.allowedCategoryIds.delete(id);
    else this.allowedCategoryIds.add(id);
  }

  toggleAllCategories() {
    if (this.allCategoriesChecked) this.allowedCategoryIds = new Set();
    else this.allowedCategoryIds = new Set(this.categories.map(c => c.id));
  }

  getPermValue(key: keyof PagePermissions): string {
    return (this.permissions as any)[key] ?? 'none';
  }

  setPermValue(key: keyof PagePermissions, val: string) {
    (this.permissions as any)[key] = val;
  }

  save() {
    if (this.mode === 'add') {
      const request: InviteMemberRequest = {
        email: this.email.trim(),
        role: this.role,
        permissions: this.isOwnerRole ? null : { ...this.permissions },
        allowedCategoryIds: this.isOwnerRole ? [] : Array.from(this.allowedCategoryIds),
        canDeleteExpenses: this.canDeleteExpenses,
      };
      this.saved.emit({ request });
    } else {
      const request: UpdateMemberRequest = {
        role: this.role,
        permissions: this.isOwnerRole ? null : { ...this.permissions },
        allowedCategoryIds: this.isOwnerRole ? [] : Array.from(this.allowedCategoryIds),
        canDeleteExpenses: this.canDeleteExpenses,
      };
      this.saved.emit({ request, memberId: this.member!.id });
    }
  }
}
