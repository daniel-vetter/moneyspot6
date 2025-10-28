import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AvatarModule } from 'primeng/avatar';
import { BadgeModule } from 'primeng/badge';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MenuModule } from 'primeng/menu';
import { RippleModule } from 'primeng/ripple';
import { OverlayBadgeModule } from 'primeng/overlaybadge';
import { TransactionPageClient } from '../server';
import { AppEvents } from '../app-events';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, lastValueFrom } from 'rxjs';

@Component({
    selector: 'app-menu',
    imports: [MenuModule, BadgeModule, AvatarModule, CommonModule, RippleModule, ButtonModule, DialogModule, RouterLink, RouterLinkActive, MenuModule, OverlayBadgeModule, BadgeModule],
    templateUrl: './menu.component.html',
    styleUrl: './menu.component.scss'
})
export class MenuComponent implements OnInit {
    private transactionPageClient = inject(TransactionPageClient);
    private appEvents = inject(AppEvents);
    newTransactionCount: number = 0;

    constructor() {
        this.appEvents.events
            .pipe(
                takeUntilDestroyed(),
                filter(e => e.type === 'NewTransactionsSeenEvent' || e.type === 'TransactionSyncDone')
            )
            .subscribe(async () => {
                await this.updateTransactionCount();
            });
    }

    async ngOnInit() {
        await this.updateTransactionCount();
    }

    private async updateTransactionCount() {
        this.newTransactionCount = await lastValueFrom(this.transactionPageClient.getNewCount());
    }
}
