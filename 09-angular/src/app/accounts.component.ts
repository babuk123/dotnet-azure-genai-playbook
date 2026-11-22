import { Component, OnInit, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { AccountService } from './account.service';

// Standalone component + new control flow (@if / @for), OnPush-friendly with signals
@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [CurrencyPipe],
  template: `
    <h2>Accounts</h2>
    @if (svc.loading()) {
      <p>Loading…</p>
    } @else {
      <ul>
        @for (a of svc.accounts(); track a.id) {
          <li>{{ a.name }} — {{ a.balance | currency }}</li>
        } @empty {
          <li>No accounts</li>
        }
      </ul>
      <strong>Total: {{ svc.total() | currency }}</strong>
    }
  `,
})
export class AccountsComponent implements OnInit {
  protected svc = inject(AccountService);
  ngOnInit() { this.svc.load(); }
}
