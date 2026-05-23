import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PwaService } from '../../services/pwa.service';

@Component({
  selector: 'app-pwa-install-prompt',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './pwa-install-prompt.component.html',
  styleUrl: './pwa-install-prompt.component.css',
})
export class PwaInstallPromptComponent {
  pwa = inject(PwaService);

  install(): void {
    this.pwa.promptInstall();
  }

  dismiss(): void {
    this.pwa.dismiss();
  }
}
