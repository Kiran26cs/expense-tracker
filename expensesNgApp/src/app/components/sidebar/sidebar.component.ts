import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, ActivatedRoute, NavigationEnd } from '@angular/router';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';

interface NavItem {
  id: string;
  label: string;
  icon: string;
  path: string;
}

const NAV_ITEMS: NavItem[] = [
  { id: 'dashboard', label: 'Dashboard', icon: 'fa-solid fa-chart-pie', path: 'dashboard' },
  { id: 'expenses', label: 'Expenses', icon: 'fa-solid fa-receipt', path: 'expenses' },
  { id: 'budget', label: 'Budget', icon: 'fa-solid fa-bullseye', path: 'budget' },
  { id: 'insights', label: 'Insights', icon: 'fa-solid fa-chart-line', path: 'insights' },
  { id: 'settings', label: 'Settings', icon: 'fa-solid fa-gear', path: 'settings' },
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

  navItems = NAV_ITEMS;
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
    // bookId is a param on THIS route (/:bookId), not on firstChild
    this.sub.add(this.route.params.subscribe(p => {
      this.bookId = p['bookId'] || '';
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
