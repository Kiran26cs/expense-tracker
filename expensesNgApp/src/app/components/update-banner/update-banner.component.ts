import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { VersionService } from '../../services/version.service';

@Component({
  selector: 'app-update-banner',
  standalone: true,
  imports: [AsyncPipe],
  templateUrl: './update-banner.component.html',
  styleUrl: './update-banner.component.css',
})
export class UpdateBannerComponent {
  version = inject(VersionService);
}
