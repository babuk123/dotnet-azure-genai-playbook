import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal, computed } from '@angular/core';

export interface Account { id: string; name: string; balance: number; }

@Injectable({ providedIn: 'root' })
export class AccountService {
  private http = inject(HttpClient);

  // Signals: state + derived state, no manual subscriptions
  readonly accounts = signal<Account[]>([]);
  readonly loading = signal(false);
  readonly total = computed(() => this.accounts().reduce((s, a) => s + a.balance, 0));

  load(): void {
    this.loading.set(true);
    this.http.get<Account[]>('/api/v1/accounts').subscribe({
      next: data => this.accounts.set(data),
      complete: () => this.loading.set(false),
      error: () => this.loading.set(false),
    });
  }
}
