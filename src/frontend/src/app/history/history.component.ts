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
import { DatePickerModule } from 'primeng/datepicker';

@Component({
    selector: 'app-history',
    imports: [HighchartsChartModule, DatePickerModule, SplitButtonModule, FormsModule, ButtonGroupModule, DaterangePresetSelectorComponent, PanelModule],
    templateUrl: './history.component.html',
    styleUrl: './history.component.scss'
})
export class HistoryComponent implements OnInit {
    Highcharts: typeof Highcharts = Highcharts;
    charts: Highcharts.Options[] = [];
    dateRange: [Date, Date];

    constructor(private accountHistoryClient: AccountHistoryClient) {
        const start = new Date();
        start.setDate(start.getDate() + 1);
        start.setMonth(start.getMonth() - 12);
        start.setHours(0, 0, 0, 0);

        const end = new Date();
        end.setDate(end.getDate() + 1);
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

        this.charts = [];
        this.charts.push({
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
                endOnTick: false,
                startOnTick: false,
                labels: {
                    style: {
                        fontSize: "1rem"
                    }
                }
            },
            xAxis: {
                type: 'datetime',
                title: {
                    text: 'Datum',
                },
                labels: {
                    style: {
                        fontSize: "1rem"
                    }
                }
            },
            plotOptions: {
                area: {
                    stacking: "normal"
                }
            },
            tooltip: {
                shared: true,
                style: {
                    fontSize: "1rem"
                }
            },
            series: [
                {
                    name: 'Konten',
                    type: 'area',
                    data: result.map((x) => [x.date.valueOf(), Math.round(x.balance * 100) / 100]),
                    fillOpacity: 0.15,
                    animation: {
                        duration: 0
                    }
                },
                {
                    name: 'Aktien',
                    type: 'area',
                    data: result.map((x) => [x.date.valueOf(), Math.round(x.stockValue * 100) / 100]),
                    fillOpacity: 0.15,
                    animation: {
                        duration: 0
                    }
                },
                {
                    name: 'Investment',
                    type: 'line',
                    data: result.map((x) => [x.date.valueOf(), Math.round(x.stockInvested * 100) / 100]),
                    animation: {
                        duration: 0
                    }
                },
            ]
        });
    }
}
