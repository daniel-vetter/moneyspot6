import { Component, OnInit, inject } from '@angular/core';
import { PanelModule } from 'primeng/panel';
import { SummaryPageClient } from '../../server';
import { lastValueFrom } from 'rxjs';
import { HighchartsChartModule } from 'highcharts-angular';
import Highcharts from 'highcharts';

@Component({
    selector: 'app-balance-history',
    imports: [PanelModule, HighchartsChartModule],
    templateUrl: './balance-history.component.html',
    styleUrl: './balance-history.component.scss'
})
export class BalanceHistoryComponent implements OnInit {
    private summaryPageClient = inject(SummaryPageClient);

    Highcharts: typeof Highcharts = Highcharts;
    chart?: Highcharts.Options = undefined;

    async ngOnInit(): Promise<void> {
        const r = await lastValueFrom(this.summaryPageClient.getCurrentMonthBalanceHistory());
        const now = new Date();
        const startOfCurrentMonth = new Date(now.getFullYear(), now.getMonth(), 1).valueOf();

        this.chart = {
            title: {
                text: undefined,
            },
            yAxis: {
                title: undefined,
                labels: {
                    style: {
                        fontSize: "1rem"
                    }
                }
            },
            xAxis: {
                type: 'datetime',
                labels: {
                    style: {
                        fontSize: "1rem"
                    }
                },
                plotLines: [{
                    color: '#888888',
                    width: 2,
                    value: startOfCurrentMonth,
                    dashStyle: 'Dash',
                    zIndex: 5
                }]
            },
            tooltip: {
                shared: true,
                style: {
                    fontSize: "1rem"
                }
            },
            series: [
                {
                    name: 'Kontostand',
                    type: 'line',
                    data: r.entries.map((x) => [x.date.valueOf(), Math.round(x.balance * 100) / 100]),
                    animation: {
                        duration: 0
                    }
                }
            ],
            credits: {
                enabled: false,
            },
            chart: {
                animation: {
                    duration: 0,
                },
                zooming: {
                    type: 'x',
                },
            },
        };
    }
}
