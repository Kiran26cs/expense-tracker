import { Component, computed, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { UpperCasePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { AdminAuthService } from '../../core/services/admin-auth.service';
import { PwaService } from '../../core/services/pwa.service';
import { AdminPermissions } from '../../shared/models/admin.models';

interface NavItem { label: string; route: string; icon: string; permission?: string; }

// First 4 appear in the bottom tab bar; the rest go into the "More" sheet.
const PRIMARY_ROUTES = new Set(['/dashboard', '/users', '/credits', '/books']);

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard',       route: '/dashboard',       icon: '◈',  permission: AdminPermissions.Analytics },
  { label: 'Users',           route: '/users',           icon: '👤', permission: AdminPermissions.Users },
  { label: 'Credits',         route: '/credits',         icon: '⚡', permission: AdminPermissions.Credits },
  { label: 'Books',           route: '/books',           icon: '📒', permission: AdminPermissions.Books },
  { label: 'Cache',           route: '/cache',           icon: '🗄', permission: AdminPermissions.Cache },
  { label: 'Jobs',            route: '/jobs',            icon: '⚙',  permission: AdminPermissions.Jobs },
  { label: 'Platform Admins', route: '/platform-admins', icon: '🔑', permission: AdminPermissions.Admins },
];

@Component({
  selector: 'admin-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UpperCasePipe],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.css',
})
export class AdminShellComponent implements OnInit, OnDestroy {
  auth     = inject(AdminAuthService);
  pwa      = inject(PwaService);
  private router = inject(Router);

  isMobile    = signal(window.innerWidth < 768);
  sidebarOpen = signal(window.innerWidth >= 768);
  moreOpen    = signal(false);

  private currentUrl = signal(this.router.url);

  currentTitle = computed(() => {
    const url = this.currentUrl();
    return NAV_ITEMS.find(item => url.startsWith(item.route))?.label ?? 'Admin';
  });

  constructor() {
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe(e => {
        this.currentUrl.set(e.urlAfterRedirects);
        if (this.isMobile()) this.moreOpen.set(false);
      });
  }

  private onResize = () => {
    const mobile = window.innerWidth < 768;
    this.isMobile.set(mobile);
    if (!mobile) { this.sidebarOpen.set(true); this.moreOpen.set(false); }
  };

  ngOnInit()    { window.addEventListener('resize', this.onResize); }
  ngOnDestroy() { window.removeEventListener('resize', this.onResize); }

  get navItems(): NavItem[] {
    return NAV_ITEMS.filter(i => !i.permission || this.auth.hasPermission(i.permission));
  }

  get primaryNavItems(): NavItem[] { return this.navItems.filter(i => PRIMARY_ROUTES.has(i.route)); }
  get moreNavItems():    NavItem[] { return this.navItems.filter(i => !PRIMARY_ROUTES.has(i.route)); }

  toggleSidebar() { this.sidebarOpen.update(v => !v); }
  onNavClick()    { if (this.isMobile()) this.sidebarOpen.set(false); }
  toggleMore()    { this.moreOpen.update(v => !v); }
}
