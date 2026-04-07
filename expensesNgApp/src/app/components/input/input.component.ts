import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => InputComponent), multi: true }],
  templateUrl: './input.component.html',
  styleUrl: './input.component.css'
})
export class InputComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() type = 'text';
  @Input() placeholder = '';
  @Input() disabled = false;
  @Input() required = false;
  @Input() icon = '';
  @Input() error = '';
  @Input() min = '';
  @Input() max = '';
  @Input() step = '';
  @Input() set value(val: string) { if (val !== this._value) { this._value = val ?? ''; } }
  get value() { return this._value; }
  @Output() valueChange = new EventEmitter<string>();

  _value = '';
  onChange: any = () => {};
  onTouched: any = () => {};

  onInput(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this._value = val;
    this.onChange(val);
    this.valueChange.emit(val);
  }

  writeValue(val: any) { this._value = val ?? ''; }
  registerOnChange(fn: any) { this.onChange = fn; }
  registerOnTouched(fn: any) { this.onTouched = fn; }
  setDisabledState(disabled: boolean) { this.disabled = disabled; }
}

export { TextareaComponent } from './textarea.component';
export { SelectComponent } from './select.component';

