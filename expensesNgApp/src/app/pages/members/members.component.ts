import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MemberService } from '../../services/member.service';
import { BookAccessService } from '../../services/book-access.service';
import { ToastService } from '../../services/toast.service';
import {
  ExpenseBookMember,
  InviteMemberRequest,
  MemberRole,
  ResolvedPermissions,
  UpdateMemberRequest,
} from '../../models/member.model';
import { Category } from '../../models/expense.model';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { LoadingComponent } from '../../components/loading/loading.component';
import { ConfirmDialogComponent } from '../../components/confirm-dialog/confirm-dialog.component';
import { MemberPermissionModalComponent } from '../../components/member-permission-modal/member-permission-modal.component';

const ROLE_RANK: Record<string, number> = { viewer: 0, member: 1, admin: 2, owner: 3 };

@Component({
  selector: 'app-members',
  standalone: true,
  imports: [
    CommonModule,
    CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent,
    ButtonComponent, LoadingComponent, ConfirmDialogComponent, MemberPermissionModalComponent,
  ],
  templateUrl: './members.component.html',
  styleUrl: './members.component.css',
})
export class MembersComponent implements OnInit {
  private memberService = inject(MemberService);
  private bookAccess    = inject(BookAccessService);
  private toast         = inject(ToastService);
  private route         = inject(ActivatedRoute);

  bookId     = '';
  members    = signal<ExpenseBookMember[]>([]);
  loading    = signal(true);
  myPerms    = signal<ResolvedPermissions | null>(null);
  categories = signal<Category[]>([]);

  // Modal state
  showModal    = signal(false);
  modalMode    = signal<'add' | 'edit'>('add');
  modalMember  = signal<ExpenseBookMember | null>(null);
  modalLoading = signal(false);

  // Invite result banner
  inviteLink       = signal('');
  showInviteResult = signal(false);

  // Remove confirm
  showRemoveConfirm = signal(false);
  memberToRemove    = signal<ExpenseBookMember | null>(null);
  removeLoading     = false;

  readonly statusLabel: Record<string, string> = {
    pending:  'Pending',
    accepted: 'Active',
    revoked:  'Revoked',
  };

  ngOnInit() {
    this.route.parent?.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
      this.loadAll();
    });
  }

  private async loadAll() {
    this.loading.set(true);
    try {
      const [membersRes, permsRes, catsRes] = await Promise.all([
        this.memberService.getMembers(this.bookId),
        this.memberService.getMyPermissions(this.bookId),
        this.memberService.getAccessibleCategories(this.bookId),
      ]);
      if (membersRes.success && membersRes.data) this.members.set(membersRes.data);
      if (permsRes.success  && permsRes.data)   this.myPerms.set(permsRes.data);
      if (catsRes.success   && catsRes.data)    this.categories.set(catsRes.data);
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to load members');
    } finally {
      this.loading.set(false);
    }
  }

  // ── Visibility helpers ───────────────────────────────────────────────────

  get canManage(): boolean {
    return this.myPerms()?.canManageMembers ?? false;
  }

  get myUserId(): string {
    return (this.bookAccess as any).currentUserId?.() ?? '';
  }

  get myRole(): string {
    return this.myPerms()?.role ?? 'none';
  }

  canEdit(m: ExpenseBookMember): boolean {
    if (!this.canManage) return false;
    if (m.userId === this.myUserId) return false;
    if (this.myRole === 'admin' && m.role === 'owner') return false;
    return true;
  }

  canRemove(m: ExpenseBookMember): boolean {
    if (m.userId === this.myUserId) return false;
    if (this.myRole === 'owner') return true;
    if (this.myRole === 'admin') return (ROLE_RANK[m.role] ?? 0) <= ROLE_RANK['admin'];
    return false;
  }

  // ── Modal ────────────────────────────────────────────────────────────────

  openAddModal() {
    this.modalMode.set('add');
    this.modalMember.set(null);
    this.showInviteResult.set(false);
    this.showModal.set(true);
  }

  openEditModal(m: ExpenseBookMember) {
    this.modalMode.set('edit');
    this.modalMember.set(m);
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.modalMember.set(null);
  }

  async onModalSave(event: { request: InviteMemberRequest | UpdateMemberRequest; memberId?: string }) {
    this.modalLoading.set(true);
    try {
      if (this.modalMode() === 'add') {
        const res = await this.memberService.inviteMember(this.bookId, event.request as InviteMemberRequest);
        if (res.success && res.data) {
          this.inviteLink.set(res.data.inviteLink);
          this.showInviteResult.set(true);
          this.toast.success('Invite created — share the link below');
          this.closeModal();
          await this.loadAll();
        } else {
          this.toast.error(res.error || 'Failed to invite member');
        }
      } else {
        const res = await this.memberService.updateMember(
          this.bookId, event.memberId!, event.request as UpdateMemberRequest
        );
        if (res.success) {
          this.toast.success('Member updated');
          this.closeModal();
          await this.loadAll();
        } else {
          this.toast.error(res.error || 'Failed to update member');
        }
      }
    } catch (e: any) {
      this.toast.error(e.message || 'Operation failed');
    } finally {
      this.modalLoading.set(false);
    }
  }

  copyLink() {
    navigator.clipboard.writeText(this.inviteLink()).then(() => this.toast.success('Link copied!'));
  }

  // ── Remove ───────────────────────────────────────────────────────────────

  openRemoveConfirm(m: ExpenseBookMember) {
    this.memberToRemove.set(m);
    this.showRemoveConfirm.set(true);
  }

  async confirmRemove() {
    const m = this.memberToRemove();
    if (!m) return;
    this.removeLoading = true;
    try {
      await this.memberService.removeMember(this.bookId, m.id);
      this.toast.success('Member removed');
      this.showRemoveConfirm.set(false);
      await this.loadAll();
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to remove member');
    } finally {
      this.removeLoading = false;
    }
  }
}
