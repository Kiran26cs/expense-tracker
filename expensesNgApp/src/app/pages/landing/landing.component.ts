import { Component, inject, OnInit } from '@angular/core';
import { AuthStateService } from '../../services/auth-state.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.css'
})
export class LandingComponent implements OnInit {
  auth = inject(AuthStateService);
  mobileMenuOpen = false;
  readonly appUrl = environment.appUrl;

  ngOnInit() {
    const hash = window.location.hash;
    if (hash) {
      setTimeout(() => {
        const el = document.querySelector(hash);
        el?.scrollIntoView({ behavior: 'smooth' });
      }, 150);
    }
  }
  openFaq: number | null = null;

  features = [
    { icon: 'fa-book-open', title: 'Expense Books', desc: 'Organize finances into separate workbooks — personal, work, trips, and more.', color: 'indigo' },
    { icon: 'fa-wand-magic-sparkles', title: 'AI Assistant', desc: 'Chat naturally to add expenses, set budgets, and get insights — no forms needed.', color: 'purple' },
    { icon: 'fa-chart-pie', title: 'Budget Tracking', desc: 'Set monthly category budgets and monitor spend in real time.', color: 'green' },
    { icon: 'fa-handshake', title: 'Lending Tracker', desc: 'Track money lent or borrowed and mark settlements with ease.', color: 'amber' },
    { icon: 'fa-rotate', title: 'Recurring Expenses', desc: 'Never miss a subscription or bill — automate recurring expense tracking.', color: 'blue' },
    { icon: 'fa-users', title: 'Team Collaboration', desc: 'Invite members with role-based permissions — Admin, Member, or Viewer.', color: 'rose' },
    { icon: 'fa-file-import', title: 'CSV Import', desc: 'Migrate your existing data in seconds with our smart CSV importer.', color: 'teal' },
    { icon: 'fa-chart-line', title: 'Dashboard & Insights', desc: 'Visualize spending trends, monthly comparisons, and upcoming payments.', color: 'orange' },
  ];

  faqs = [
    { q: 'Is NidhiWise free to use?', a: 'Yes! NidhiWise is free with 40 AI credits every month, automatically reset on the 1st. You can top up with paid credit packs whenever you need more.' },
    { q: 'What can the AI assistant actually do?', a: 'The AI can create and update expenses, set budgets, invite members to your book, and answer questions about your spending — all through natural conversation.' },
    { q: 'Can I share a book with my team or family?', a: 'Absolutely. Invite unlimited members with role-based access — Admins manage everything, Members add expenses, and Viewers have read-only access.' },
    { q: 'What are AI credits?', a: 'Each AI chat turn costs 1 credit. Free accounts get 40 credits per month, automatically reset. Paid packs let you top up anytime with no subscription required.' },
    { q: 'Is my financial data secure?', a: 'Yes. All data is encrypted in transit and at rest. We never share or sell your data. Each expense book is fully isolated and only accessible to invited members.' },
  ];

  toggleFaq(i: number) {
    this.openFaq = this.openFaq === i ? null : i;
  }
}
