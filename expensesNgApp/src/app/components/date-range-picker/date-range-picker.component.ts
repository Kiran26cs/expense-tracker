import { Component, Input, Output, EventEmitter } from '@angular/core';
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

  get displayLabel(): string {
    if (this.localStart && this.localEnd) return `${this.localStart} → ${this.localEnd}`;
    if (this.localStart) return `From ${this.localStart}`;
    return 'Date range';
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

