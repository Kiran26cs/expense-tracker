import { Component, Input, inject, HostListener, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { TruncateDirective } from '../../directives/truncate.directive';
import { AuthStateService } from '../../services/auth-state.service';
import { ThemeService } from '../../services/theme.service';
import { ImportService } from '../../services/import.service';
import { CurrentBookService } from '../../services/current-book.service';
import { ExpenseBookService } from '../../services/expense-book.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TruncateDirective],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.css'
})
export class TopbarComponent {
  @Input() isSidebarCollapsed = false;
  @Input() isMobile = false;
  @Input() bookName = '';

  authState     = inject(AuthStateService);
  themeService  = inject(ThemeService);
  importService = inject(ImportService);
  currentBook   = inject(CurrentBookService);
  private bookService = inject(ExpenseBookService);
  private toast       = inject(ToastService);

  isMenuOpen    = false;
  isEditingName = false;
  editName      = '';

  @ViewChild('menuWrapper') menuWrapper!: ElementRef;
  @ViewChild('nameInput')   nameInput!: ElementRef<HTMLInputElement>;

  @HostListener('document:mousedown', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (this.menuWrapper && !this.menuWrapper.nativeElement.contains(event.target)) {
      this.isMenuOpen = false;
    }
  }

  toggleMenu() { this.isMenuOpen = !this.isMenuOpen; }

  openImportDrawer() {
    const bookId = this.currentBook.book()?.id;
    if (bookId) this.importService.openDrawer(bookId);
  }

getUserInitials(): string {
    const name = this.authState.user()?.name;
    if (!name) return 'U';
    return name.split(' ').map((n: string) => n[0]).join('').toUpperCase().slice(0, 2);
  }

  startEditName() {
    const book = this.currentBook.book();
    if (!book) return;
    this.editName = book.name;
    this.isEditingName = true;
    setTimeout(() => this.nameInput?.nativeElement.select(), 0);
  }

  async saveEditName() {
    if (!this.isEditingName) return;
    const book = this.currentBook.book();
    if (!book) { this.isEditingName = false; return; }
    const name = this.editName.trim();
    if (!name || name === book.name) { this.isEditingName = false; return; }
    try {
      const res = await this.bookService.updateExpenseBook(book.id, { name });
      if (res.success && res.data) {
        this.currentBook.setBook({ ...book, name: res.data.name });
        this.toast.success('Book name updated');
      } else {
        this.toast.error(res.error || 'Failed to rename book');
      }
    } catch (e: any) {
      this.toast.error(e.message || 'Failed to rename book');
    } finally {
      this.isEditingName = false;
    }
  }

  cancelEditName() { this.isEditingName = false; }


  onNameKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter')  { event.preventDefault(); this.saveEditName(); }
    if (event.key === 'Escape') { this.cancelEditName(); }
  }
}
