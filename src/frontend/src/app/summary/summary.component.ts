import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { filter, lastValueFrom, Subscription } from 'rxjs';
import { BankAccountSummaryResponse, StockSummaryResponse, SummaryPageClient } from '../server';
import { RippleModule } from 'primeng/ripple';
import { CardModule } from 'primeng/card';
import { ValueComponent } from '../common/value/value.component';
import { PanelModule } from 'primeng/panel';
import { AccountSyncComponent } from '../account-sync/account-sync.component';
import { GoalComponent } from './goal/goal.component';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { minDelay } from '../common/load-delay';
import { MonthCarouselComponent } from "./month-carousel/month-carousel.component";
import { AppEvents } from '../app-events';

@Component({
    selector: 'app-summary',
    imports: [ButtonModule, RippleModule, CardModule, ValueComponent, PanelModule, AccountSyncComponent, GoalComponent, ProgressSpinnerModule, MonthCarouselComponent],
    templateUrl: './summary.component.html',
    styleUrl: './summary.component.scss'
})
export class SummaryComponent implements OnInit, OnDestroy {
    private summaryPageClient = inject(SummaryPageClient);
    private appEvents = inject(AppEvents);

    bankAccountSummary?: BankAccountSummaryResponse;
    stockSummary?: StockSummaryResponse;
    total = 0;
    private _onAccountSyncDoneSubscription?: Subscription;
    isLoading = true;

    async ngOnInit(): Promise<void> {
        this._onAccountSyncDoneSubscription = this.appEvents.events.pipe(filter(x => x.type === 'TransactionSyncDone')).subscribe(async () => await this.update());
        await this.update();
    }

    ngOnDestroy(): void {
        this._onAccountSyncDoneSubscription?.unsubscribe();
    }

    private async update() {
        const bankAccountsPromise = minDelay(lastValueFrom(this.summaryPageClient.getBankAccountSummary()));
        const stocksPromise = minDelay(lastValueFrom(this.summaryPageClient.getStockSummary()));

        const [bankAccounts, stockSummary] = await Promise.all([bankAccountsPromise, stocksPromise]);

        this.bankAccountSummary = bankAccounts;
        this.stockSummary = stockSummary;
        this.total = bankAccounts.accounts.reduce((a, b) => a + b.total, 0)! + stockSummary.total;
        this.isLoading = false;
    }
}
