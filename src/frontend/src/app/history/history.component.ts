import { Component, OnInit } from '@angular/core';
import * as Highcharts from 'highcharts';
import { HighchartsChartModule } from 'highcharts-angular';
import { AccountHistoryClient } from '../server';
import { lastValueFrom } from 'rxjs';
import { CalendarModule } from 'primeng/calendar';
import { SplitButtonModule } from 'primeng/splitbutton';
import { FormsModule } from '@angular/forms';
import { ButtonGroupModule } from 'primeng/buttongroup';
import { DaterangePresetSelectorComponent } from './daterange-preset-selector/daterange-preset-selector.component';
import { PanelModule } from 'primeng/panel';

@Component({
    selector: 'app-history',
    standalone: true,
    imports: [HighchartsChartModule, CalendarModule, SplitButtonModule, FormsModule, ButtonGroupModule, DaterangePresetSelectorComponent, PanelModule],
    templateUrl: './history.component.html',
    styleUrl: './history.component.scss',
})
export class HistoryComponent implements OnInit {
    Highcharts: typeof Highcharts = Highcharts;
    charts2: Highcharts.Options[] = [];
    dateRange: [Date, Date];

    constructor(private accountHistoryClient: AccountHistoryClient) {
        const start = new Date();
        start.setMonth(0);
        start.setDate(1);
        start.setHours(0, 0, 0, 0);

        const end = new Date();
        end.setMonth(11);
        end.setDate(1);
        end.setHours(0, 0, 0, 0);
        this.dateRange = [start, end];
    }

    async ngOnInit(): Promise<void> {
        await this.update();
    }

    async update() {
        if (this.dateRange[0] === null || this.dateRange[0] === undefined) return;
        if (this.dateRange[1] === null || this.dateRange[1] === undefined) return;

        // TODO: DateTime is wrong time zone
        const result = await lastValueFrom(
            this.accountHistoryClient.get([1, 2, 3], this.dateRange[0].toISOString().substring(0, 10), this.dateRange[1].toISOString().substring(0, 10)),
        );

        let min = Number.MAX_VALUE;
        let max = Number.MIN_VALUE;

        for (const n of result.map((x) => x.balance)) {
            if (n < min) min = n;
            if (n > max) max = n;
        }

        const diff = max - min;
        min -= diff * 0.1;
        max += diff * 0.1;

        this.charts2 = [];
        this.charts2.push({
            chart: {
                height: '70%',
            },
            title: {
                text: undefined
            },
            yAxis: {
                title: {
                    text: 'Vermögen',
                },
                min: min / 100,
                max: max / 100,
                endOnTick: false,
                startOnTick: false,
            },
            xAxis: {
                type: 'datetime',
                title: {
                    text: 'Datum',
                },
            },
            series: [
                {
                    name: 'Alle Konten',
                    type: 'area',
                    data: result.map((x) => [x.date.valueOf(), x.balance / 100]),
                    fillOpacity: 0.15,
                    animation: {
                        duration: 0
                    }
                },
            ]
        });
    }
}
