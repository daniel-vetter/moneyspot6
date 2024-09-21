import { Component, OnDestroy, OnInit } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { lastValueFrom, Subscription } from 'rxjs';
import { BankAccountSummaryResponse, SummaryPageClient } from '../server';
import { RippleModule } from 'primeng/ripple';
import { CardModule } from 'primeng/card';
import { ValueComponent } from "../common/value/value.component";
import { PanelModule } from 'primeng/panel';
import { AccountSyncComponent } from "../account-sync/account-sync.component";
import { GlobalEvents } from '../common/global-events';

@Component({
  selector: 'app-summary',
  standalone: true,
  imports: [ButtonModule, RippleModule, CardModule, ValueComponent, PanelModule, AccountSyncComponent],
  templateUrl: './summary.component.html',
  styleUrl: './summary.component.scss'
})
export class SummaryComponent implements OnInit, OnDestroy {

  bankAccounts?: BankAccountSummaryResponse;
  total = 0;
  private _onAccountSyncDoneSubscription?: Subscription;

  constructor(private summaryPageClient: SummaryPageClient, private globalEvents: GlobalEvents) { }
  
  async ngOnInit(): Promise<void> {
    this._onAccountSyncDoneSubscription = this.globalEvents.onAccountSyncDone.subscribe(async () => await this.update());
    await this.update();
  }

  ngOnDestroy(): void {
    this._onAccountSyncDoneSubscription?.unsubscribe();
  }

  private async update() {
    this.bankAccounts = await lastValueFrom(this.summaryPageClient.getBackAccountSummary())
    this.total = this.bankAccounts.entries?.reduce((a, b) => a + b.total!, 0)!;
  }
}

