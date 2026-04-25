import {Component, inject, OnDestroy, OnInit} from '@angular/core';
import { CarouselModule } from 'primeng/carousel';
import { MonthSummaryResponse, SummaryPageClient } from '../../server';
import { PanelModule } from "primeng/panel";
import {filter, lastValueFrom, Subscription} from 'rxjs';
import { ScrollPanelModule } from 'primeng/scrollpanel';
import { ValueComponent } from '../../common/value/value.component';
import { AppEvents } from '../../app-events';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {BreakpointObserver, LayoutModule} from "@angular/cdk/layout";
import { ScrollFadeDirective } from '../../common/scroll-fade.directive';

@Component({
    selector: 'app-month-carousel',
    imports: [CarouselModule, PanelModule, ValueComponent, ScrollPanelModule, LayoutModule, ScrollFadeDirective],
    templateUrl: './month-carousel.component.html',
    styleUrl: './month-carousel.component.scss'
})
export class MonthCarouselComponent implements OnInit, OnDestroy {

    summaryPageClient = inject(SummaryPageClient);
    appEvents = inject(AppEvents);
    breakpointObserver: BreakpointObserver = inject(BreakpointObserver);
    tileCount = 1;
    showNavigator = false;
    mediaObserverSubscription: Subscription | undefined;

    constructor() {
        this.appEvents.events
            .pipe(takeUntilDestroyed(), filter(e => e.type === 'TransactionSyncDone'))
            .subscribe(async () => {
                await this.update();
            })


    }

    async ngOnInit(): Promise<void> {
        this.mediaObserverSubscription = this.breakpointObserver.observe('(max-width: 1000px)').subscribe(x => {
            if (x.matches) {
                this.tileCount = 1;
                this.showNavigator = false;
            } else {
                this.tileCount = 4;
                this.showNavigator = true;
            }
        });

        await this.update();
    }

    ngOnDestroy(): void {
        if (this.mediaObserverSubscription) {
            this.mediaObserverSubscription.unsubscribe();
        }
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
