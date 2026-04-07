import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './card.component.html',
  styleUrl: './card.component.css'
})
export class CardComponent {
  @Input() cardClass = '';
}

export { CardHeaderComponent } from './card-header.component';
export { CardTitleComponent } from './card-title.component';
export { CardContentComponent } from './card-content.component';

