import {Component, OnInit, inject, signal} from '@angular/core';
import {AccountHistoryBalanceResponse, AccountHistoryClient} from '../server';
import {lastValueFrom} from 'rxjs';
import {SplitButtonModule} from 'primeng/splitbutton';
import {FormsModule} from '@angular/forms';
import {ButtonGroupModule} from 'primeng/buttongroup';
import {PanelModule} from 'primeng/panel';
import {DatePickerModule} from 'primeng/datepicker';
import {DateRange, DateRangePickerComponent} from "../common/date-range-picker/date-range-picker.component";
import {DateRangePresetsComponent} from "../common/date-range-presets/date-range-presets.component";
import {ActivatedRoute} from "@angular/router";
import {ToggleSwitchModule} from "primeng/toggleswitch";
import {TabsModule} from "primeng/tabs";
import {TotalHistoryChartComponent} from "./total-history-chart/total-history-chart.component";
import {ProfitHistoryChartComponent} from "./profit-history-chart/profit-history-chart.component";

@Component({
    selector: 'app-history',
    imports: [DatePickerModule, SplitButtonModule, FormsModule, ButtonGroupModule, PanelModule, DateRangePickerComponent, DateRangePresetsComponent, ToggleSwitchModule, TabsModule, TotalHistoryChartComponent, ProfitHistoryChartComponent],
    templateUrl: './history.component.html',
    styleUrl: './history.component.scss'
})
export class HistoryComponent implements OnInit {
    private accountHistoryClient = inject(AccountHistoryClient);
    private activatedRoute = inject(ActivatedRoute);

    private static readonly startFromZeroStorageKey = 'history.startFromZero';

    protected readonly dateRange = signal<DateRange | undefined>(undefined);
    protected startFromZero = false;
    protected readonly data = signal<AccountHistoryBalanceResponse[]>([]);

    async ngOnInit(): Promise<void> {
        this.startFromZero = localStorage.getItem(HistoryComponent.startFromZeroStorageKey) === 'true';

        const result = await lastValueFrom(this.accountHistoryClient.get());
        this.data.set(result);

        this.activatedRoute.queryParams.subscribe(x => {
            this.dateRange.set(DateRange.parse(x['dateRange']));
        });
    }

    protected onStartFromZeroChanged() {
        localStorage.setItem(HistoryComponent.startFromZeroStorageKey, String(this.startFromZero));
    }
}
