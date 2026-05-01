import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthStateService } from '../../services/auth-state.service';
import { MemberService } from '../../services/member.service';

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './accept-invite.component.html',
  styleUrl: './accept-invite.component.css',
})
export class AcceptInviteComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auth = inject(AuthStateService);
  private memberService = inject(MemberService);

  state = signal<'loading' | 'error'>('loading');
  errorMessage = signal('');
  token = '';

  async ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token') || '';
    if (!this.token) {
      this.state.set('error');
      this.errorMessage.set('Invalid or missing invite token.');
      return;
    }

    // Wait for auth state to settle before checking
    if (this.auth.isLoading()) {
      await new Promise<void>(resolve => {
        const interval = setInterval(() => {
          if (!this.auth.isLoading()) { clearInterval(interval); resolve(); }
        }, 50);
      });
    }

    if (!this.auth.isAuthenticated()) {
      sessionStorage.setItem('pendingInviteToken', this.token);
      this.router.navigate(['/signup'], { queryParams: { redirect: '/accept-invite', token: this.token } });
      return;
    }

    this.acceptInvite();
  }

  async acceptInvite() {
    this.state.set('loading');
    try {
      const res = await this.memberService.acceptInvite(this.token);
      if (res.success && res.data) {
        sessionStorage.removeItem('pendingInviteToken');
        this.router.navigate(['/', res.data.expenseBookId, 'dashboard']);
      } else {
        this.state.set('error');
        this.errorMessage.set(res.error || 'Failed to accept invite.');
      }
    } catch (e: any) {
      const backendMsg = e?.error?.error || e?.error?.message || e?.error?.title;
      // Always clear the stale token — it's either consumed or invalid
      sessionStorage.removeItem('pendingInviteToken');
      const lmsg = (backendMsg || '').toLowerCase();
      // Already a member or token already used → silently go home
      if (lmsg.includes('already a member') || lmsg.includes('not found') || lmsg.includes('already used')) {
        this.router.navigate(['/']);
        return;
      }
      this.state.set('error');
      this.errorMessage.set(backendMsg || e.message || 'An error occurred.');
    }
  }
}
