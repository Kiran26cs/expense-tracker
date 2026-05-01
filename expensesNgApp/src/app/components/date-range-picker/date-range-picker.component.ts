import { Component, Input, Output, EventEmitter, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-date-range-picker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './date-range-picker.component.html',
  styleUrl: './date-range-picker.component.css'
})
export class DateRangePickerComponent {
  @Input() set startDate(v: string) { this.localStart = v || ''; }
  @Input() set endDate(v: string) { this.localEnd = v || ''; }
  @Output() rangeChange = new EventEmitter<{ start: string; end: string }>();

  open = false;
  localStart = '';
  localEnd = '';
  popoverStyle: Record<string, string> = {};

  constructor(private elRef: ElementRef) {}

  get displayLabel(): string {
    if (this.localStart && this.localEnd) return `${this.localStart} → ${this.localEnd}`;
    if (this.localStart) return `From ${this.localStart}`;
    return 'Date range';
  }

  toggle() {
    if (this.open) {
      this.open = false;
      return;
    }
    const trigger = this.elRef.nativeElement.querySelector('.drp-trigger') as HTMLElement;
    if (trigger) {
      const rect = trigger.getBoundingClientRect();
      const popoverWidth = 280;
      let left = rect.right - popoverWidth;
      if (left < 8) left = 8;
      this.popoverStyle = {
        top: `${rect.bottom + 8}px`,
        left: `${left}px`,
        width: `${popoverWidth}px`
      };
    }
    this.open = true;
  }

  apply() {
    this.rangeChange.emit({ start: this.localStart, end: this.localEnd });
    this.open = false;
  }

  clear() {
    this.localStart = '';
    this.localEnd = '';
    this.rangeChange.emit({ start: '', end: '' });
    this.open = false;
  }
}

