import { Injectable, signal, computed } from '@angular/core';
import { MemberService } from './member.service';
import { ResolvedPermissions } from '../models/member.model';

const NONE_PERMISSIONS: ResolvedPermissions = {
  role: 'none',
  dashboard: 'none',
  expenses: 'none',
  budgets: 'none',
  settings: 'none',
  insights: 'none',
  canDeleteExpenses: false,
  canManageMembers: false,
  canModifyBook: false,
  isOwner: false,
  allowedCategoryIds: [],
};

@Injectable({ providedIn: 'root' })
export class BookAccessService {
  private _permissions = signal<ResolvedPermissions>(NONE_PERMISSIONS);
  private _loading = signal(false);
  private _currentBookId = signal<string | null>(null);

  readonly permissions = this._permissions.asReadonly();
  readonly loading = this._loading.asReadonly();

  readonly canViewExpenses = computed(() => this._permissions().expenses !== 'none');
  readonly canWriteExpenses = computed(() => this._permissions().expenses === 'write');
  readonly canDeleteExpenses = computed(() => this._permissions().canDeleteExpenses);

  readonly canViewBudgets = computed(() => this._permissions().budgets !== 'none');
  readonly canWriteBudgets = computed(() => this._permissions().budgets === 'write');

  readonly canViewSettings = computed(() => this._permissions().settings !== 'none');

  readonly canViewInsights = computed(() => this._permissions().insights !== 'none');
  readonly canViewDashboard = computed(() => this._permissions().dashboard !== 'none');

  readonly canManageMembers = computed(() => this._permissions().canManageMembers);
  readonly canModifyBook = computed(() => this._permissions().canModifyBook);
  readonly isOwner = computed(() => this._permissions().isOwner);

  constructor(private memberService: MemberService) {}

  async loadForBook(bookId: string): Promise<void> {
    if (this._currentBookId() === bookId) return;
    this._currentBookId.set(bookId);
    this._loading.set(true);
    try {
      const res = await this.memberService.getMyPermissions(bookId);
      if (res.success && res.data) {
        this._permissions.set(res.data);
      } else {
        this._permissions.set(NONE_PERMISSIONS);
      }
    } catch {
      this._permissions.set(NONE_PERMISSIONS);
    } finally {
      this._loading.set(false);
    }
  }

  /** Force a permissions refresh (e.g. after accepting an invite). */
  async refreshForBook(bookId: string): Promise<void> {
    this._currentBookId.set(null);
    await this.loadForBook(bookId);
  }

  reset() {
    this._permissions.set(NONE_PERMISSIONS);
    this._currentBookId.set(null);
  }
}
