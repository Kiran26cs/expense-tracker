import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AdminAuthService } from './core/services/admin-auth.service';

@Component({
  selector: 'admin-root',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App implements OnInit {
  private auth = inject(AdminAuthService);

  async ngOnInit() {
    await this.auth.loadAdminFromToken();
  }
}
