import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MemberService } from '../../services/member.service';
import { BookAccessService } from '../../services/book-access.service';
import { ToastService } from '../../services/toast.service';
import { ExpenseBookMember, InviteMemberRequest, MemberRole } from '../../models/member.model';
import { CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent } from '../../components/card/card.component';
import { ButtonComponent } from '../../components/button/button.component';
import { InputComponent, SelectComponent } from '../../components/input/input.component';
import { LoadingComponent } from '../../components/loading/loading.component';
import { ConfirmDialogComponent } from '../../components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-members',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardComponent, CardHeaderComponent, CardTitleComponent, CardContentComponent,
    ButtonComponent, InputComponent, SelectComponent,
    LoadingComponent, ConfirmDialogComponent,
  ],
  templateUrl: './members.component.html',
  styleUrl: './members.component.css',
})
export class MembersComponent implements OnInit {
  private memberService = inject(MemberService);
  private bookAccess = inject(BookAccessService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);

  bookId = '';
  members = signal<ExpenseBookMember[]>([]);
  loading = signal(true);
  canManage = signal(false);

  // Invite form
  inviteEmail = signal('');
  inviteRole = signal<MemberRole>('member');
  setInviteRole(val: string) { this.inviteRole.set(val as MemberRole); }
  inviteLoading = signal(false);
  inviteLink = signal('');
  showInviteResult = signal(false);

  // Remove confirm
  showRemoveConfirm = signal(false);
  memberToRemove = signal<ExpenseBookMember | null>(null);
  removeLoading = false;

  readonly roleOptions = [
    { value: 'admin',  label: 'Admin — full access, can manage members' },
    { value: 'member', label: 'Member — can view & add expenses' },
    { value: 'viewer', label: 'Viewer — read only' },
  ];

  statusLabel: Record<string, string> = {
    pending: 'Pending',
    accepted: 'Active',
    revoked: 'Revoked',
  };

  ngOnInit() {
    this.route.parent?.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
      this.loadMembers();
    });

    // Mirror canManageMembers from BookAccessService
    this.canManage.set(this.bookAccess.canManageMembers());
  }

  async loadMembers() {
    this.loading.set(true);
    try {
      const res = await this.memberService.getMembers(this.bookId);
      if (res.success && res.data) this.members.set(res.data);
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to load members');
    } finally {
      this.loading.set(false);
    }
  }

  async invite() {
    const email = this.inviteEmail().trim();
    if (!email) { this.toast.error('Email is required'); return; }

    this.inviteLoading.set(true);
    this.showInviteResult.set(false);
    try {
      const req: InviteMemberRequest = {
        email,
        role: this.inviteRole(),
        allowedCategoryIds: [],
        canDeleteExpenses: false,
      };
      const res = await this.memberService.inviteMember(this.bookId, req);
      if (res.success && res.data) {
        this.inviteLink.set(res.data.inviteLink);
        this.showInviteResult.set(true);
        this.inviteEmail.set('');
        this.toast.success('Invite created');
        await this.loadMembers();
      } else {
        this.toast.error(res.error || 'Failed to invite member');
      }
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to invite member');
    } finally {
      this.inviteLoading.set(false);
    }
  }

  copyLink() {
    navigator.clipboard.writeText(this.inviteLink()).then(() => this.toast.success('Link copied!'));
  }

  openRemoveConfirm(member: ExpenseBookMember) {
    this.memberToRemove.set(member);
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
      await this.loadMembers();
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to remove member');
    } finally {
      this.removeLoading = false;
    }
  }
}
