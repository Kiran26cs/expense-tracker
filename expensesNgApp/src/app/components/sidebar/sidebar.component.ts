import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, ActivatedRoute, NavigationEnd } from '@angular/router';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { BookAccessService } from '../../services/book-access.service';

interface NavItem {
  id: string;
  label: string;
  icon: string;
  path: string;
  /** If set, the item is only shown when this permission key is true */
  requirePermission?: 'canViewExpenses' | 'canViewBudgets' | 'canViewInsights' | 'canViewSettings' | 'canViewDashboard' | 'canManageMembers';
}

const ALL_NAV_ITEMS: NavItem[] = [
  { id: 'dashboard', label: 'Dashboard', icon: 'fa-solid fa-chart-pie',  path: 'dashboard', requirePermission: 'canViewDashboard' },
  { id: 'expenses',  label: 'Expenses',  icon: 'fa-solid fa-receipt',     path: 'expenses',  requirePermission: 'canViewExpenses' },
  { id: 'budget',    label: 'Budget',    icon: 'fa-solid fa-bullseye',    path: 'budget',    requirePermission: 'canViewBudgets' },
  { id: 'insights',  label: 'Insights',  icon: 'fa-solid fa-chart-line',  path: 'insights',  requirePermission: 'canViewInsights' },
  { id: 'settings',  label: 'Settings',  icon: 'fa-solid fa-gear',        path: 'settings',  requirePermission: 'canViewSettings' },
  { id: 'members',   label: 'Members',   icon: 'fa-solid fa-users',       path: 'members',   requirePermission: 'canManageMembers' },
];

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent implements OnInit, OnDestroy {
  @Input() isCollapsed = false;
  @Input() isMobile = false;
  @Output() collapsedChange = new EventEmitter<boolean>();

  private bookAccess = inject(BookAccessService);

  /** Visible nav items filtered by the user's resolved permissions */
  navItems = computed(() => {
    const perms = this.bookAccess.permissions();
    if (perms.role === 'none') return ALL_NAV_ITEMS; // owner or not-yet-loaded: show all
    return ALL_NAV_ITEMS.filter(item => {
      if (!item.requirePermission) return true;
      return (this.bookAccess as any)[item.requirePermission]?.() ?? true;
    });
  });

  bookId = '';
  currentPath = '';
  private sub!: Subscription;

  constructor(private route: ActivatedRoute, private router: Router) {}

  ngOnInit() {
    this.currentPath = this.router.url;
    this.bookId = this.route.snapshot.paramMap.get('bookId') || '';
    this.sub = this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe((e: any) => {
      this.currentPath = e.url;
    });
    this.sub.add(this.route.params.subscribe(async p => {
      this.bookId = p['bookId'] || '';
      if (this.bookId) {
        await this.bookAccess.loadForBook(this.bookId);
      }
    }));
  }

  ngOnDestroy() { this.sub?.unsubscribe(); }

  setBookId(id: string) { this.bookId = id; }

  isActive(path: string): boolean {
    return this.currentPath.includes(`/${path}`);
  }

  toggleCollapse() {
    this.isCollapsed = !this.isCollapsed;
    this.collapsedChange.emit(this.isCollapsed);
  }
}

