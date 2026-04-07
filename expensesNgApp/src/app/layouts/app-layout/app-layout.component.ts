import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';
import { SidebarComponent } from '../../components/sidebar/sidebar.component';
import { TopbarComponent } from '../../components/topbar/topbar.component';
import { ToastContainerComponent } from '../../components/toast/toast-container.component';
import { ExpenseBookService } from '../../services/expense-book.service';
import { CurrentBookService } from '../../services/current-book.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, SidebarComponent, TopbarComponent, ToastContainerComponent],
  templateUrl: './app-layout.component.html',
  styleUrl: './app-layout.component.css'
})
export class AppLayoutComponent implements OnInit, OnDestroy {
  isSidebarCollapsed = false;
  isMobile = window.innerWidth < 768;
  bookName = '';
  private sub!: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private expenseBookService: ExpenseBookService,
    private currentBook: CurrentBookService
  ) {}

  ngOnInit() {
    this.checkMobile();
    window.addEventListener('resize', () => this.checkMobile());

    this.sub = this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(() => {
      this.loadBook();
    });
    this.loadBook();
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
    window.removeEventListener('resize', () => this.checkMobile());
    this.currentBook.setBook(null);
  }

  checkMobile() {
    this.isMobile = window.innerWidth < 768;
  }

  async loadBook() {
    const bookId = this.route.snapshot.paramMap.get('bookId') || this.router.url.split('/')[1];
    if (bookId && bookId !== 'login' && bookId !== 'signup') {
      try {
        const res = await this.expenseBookService.getExpenseBookById(bookId);
        if (res.success && res.data) {
          this.bookName = res.data.name;
          this.currentBook.setBook(res.data);
        }
      } catch { /* ignore */ }
    }
  }
}
