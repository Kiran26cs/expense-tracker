import { Directive, ElementRef, Input, OnChanges } from '@angular/core';

@Directive({
  selector: '[appTruncate]',
  standalone: true
})
export class TruncateDirective implements OnChanges {
  @Input('appTruncate') text = '';
  @Input() truncateAt = 20;

  constructor(private el: ElementRef<HTMLElement>) {}

  ngOnChanges() {
    const t = this.text ?? '';
    if (t.length > this.truncateAt) {
      this.el.nativeElement.textContent = t.slice(0, this.truncateAt) + '...';
      this.el.nativeElement.title = t;
    } else {
      this.el.nativeElement.textContent = t;
      this.el.nativeElement.title = '';
    }
  }
}
