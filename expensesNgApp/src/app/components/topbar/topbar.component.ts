import { Component, Input, Output, EventEmitter, inject, HostListener, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthStateService } from '../../services/auth-state.service';
import { ThemeService } from '../../services/theme.service';
import { ImportService } from '../../services/import.service';
import { CurrentBookService } from '../../services/current-book.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.css'
})
export class TopbarComponent {
  @Input() isSidebarCollapsed = false;
  @Input() isMobile = false;
  @Input() bookName = '';
  @Output() searchChanged = new EventEmitter<string>();

  authState    = inject(AuthStateService);
  themeService = inject(ThemeService);
  importService = inject(ImportService);
  currentBook  = inject(CurrentBookService);
  isMenuOpen = false;

  @ViewChild('menuWrapper') menuWrapper!: ElementRef;

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

  onSearch(event: Event) {
    this.searchChanged.emit((event.target as HTMLInputElement).value);
  }

  getUserInitials(): string {
    const name = this.authState.user()?.name;
    if (!name) return 'U';
    return name.split(' ').map((n: string) => n[0]).join('').toUpperCase().slice(0, 2);
  }
}
