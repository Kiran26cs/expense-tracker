import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';

@Component({
  selector: 'app-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => SelectComponent), multi: true }],
  templateUrl: './select.component.html',
  styleUrl: './select.component.css'
})
export class SelectComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = '';
  @Input() disabled = false;
  @Input() options: { value: string; label: string }[] = [];
  @Input() set value(val: string) { if (val !== this._value) { this._value = val ?? ''; } }
  @Output() valueChange = new EventEmitter<string>();
  _value = '';
  onChange: any = () => {};
  onTouched: any = () => {};
  onSelect(val: string) { this._value = val; this.onChange(val); this.onTouched(); this.valueChange.emit(val); }
  writeValue(v: any) { this._value = v ?? ''; }
  registerOnChange(fn: any) { this.onChange = fn; }
  registerOnTouched(fn: any) { this.onTouched = fn; }
  setDisabledState(d: boolean) { this.disabled = d; }
}
