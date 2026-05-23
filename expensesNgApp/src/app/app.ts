import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from './components/toast/toast-container.component';
import { UpgradeModalComponent } from './components/upgrade-modal/upgrade-modal.component';
import { AuthStateService } from './services/auth-state.service';
import { PwaInstallPromptComponent } from './components/pwa-install-prompt/pwa-install-prompt.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastContainerComponent, UpgradeModalComponent, PwaInstallPromptComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class App implements OnInit {
  private auth = inject(AuthStateService);

  ngOnInit() {
    this.auth.checkAuth();
    this.enforceSubdomain();
  }

  private enforceSubdomain() {
    const host = window.location.hostname;
    const path = window.location.pathname;
    // On root domain, any non-landing path gets redirected to app subdomain
    const landingPaths = ['/', '/features', '/pricing', '/faq', '/ai'];
    if (host === 'nidhiwise.com' && !landingPaths.some(p => path === p || path.startsWith(p + '#'))) {
      window.location.replace(`https://app.nidhiwise.com${path}${window.location.search}`);
    }
  }
}

