import { Component, inject, OnInit } from '@angular/core';
import { CarouselModule } from 'primeng/carousel';
import { MonthSummaryResponse, SummaryPageClient } from '../../server';
import { PanelModule } from "primeng/panel";
import { filter, lastValueFrom } from 'rxjs';
import { ScrollPanelModule } from 'primeng/scrollpanel';
import { ValueComponent } from '../../common/value/value.component';
import { AppEvents } from '../../app-events';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
    selector: 'app-month-carousel',
    imports: [CarouselModule, PanelModule, ValueComponent, ScrollPanelModule],
    templateUrl: './month-carousel.component.html',
    styleUrl: './month-carousel.component.scss'
})
export class MonthCarouselComponent implements OnInit {

    summaryPageClient = inject(SummaryPageClient);
    appEvents = inject(AppEvents);

    constructor() {
        this.appEvents.events
            .pipe(takeUntilDestroyed(), filter(e => e.type === 'TransactionSyncDone'))
            .subscribe(async () => {
                await this.update();
            })
    }

    async ngOnInit(): Promise<void> {
        await this.update();
    }

    private async update() {
        const now = new Date();
        const curMonth = now.getFullYear() * 12 + now.getMonth();
        const entries = await lastValueFrom(this.summaryPageClient.getMonthSummary(curMonth - 12, curMonth));
        entries.reverse();
        this.months = entries;
    }

    getMonthName(monthIndex: number): string {
        const date = new Date(0);
        date.setMonth(monthIndex);
        return date.toLocaleString('default', { month: 'long' }) + " " + Math.floor(monthIndex / 12);
    }

    months: MonthSummaryResponse[] = [];
}
