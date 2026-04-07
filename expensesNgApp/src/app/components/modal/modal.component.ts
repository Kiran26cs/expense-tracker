import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.css'
})
export class ModalComponent {
  @Input() isOpen = false;
  @Input() title = '';
  @Input() maxWidth = '500px';
  @Input() closeOnBackdrop = true;
  @Output() closed = new EventEmitter<void>();

  onBackdropClick() {
    if (this.closeOnBackdrop) this.closed.emit();
  }
}
