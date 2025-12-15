import { Component, OnInit, inject } from '@angular/core';
import { PanelModule } from 'primeng/panel';
import { SummaryPageClient } from '../../server';
import { lastValueFrom } from 'rxjs';
import { HighchartsChartModule } from 'highcharts-angular';
import Highcharts from 'highcharts';

@Component({
    selector: 'app-stock-history',
    imports: [PanelModule, HighchartsChartModule],
    templateUrl: './stock-history.component.html',
    styleUrl: './stock-history.component.scss'
})
export class StockHistoryComponent implements OnInit {
    private summaryPageClient = inject(SummaryPageClient);

    Highcharts: typeof Highcharts = Highcharts;
    chart?: Highcharts.Options = undefined;

    async ngOnInit(): Promise<void> {
        const r = await lastValueFrom(this.summaryPageClient.getStockValueHistory());
        const monthStarts = this.getMonthStarts(62);

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
                plotLines: monthStarts.map(date => ({
                    color: '#888888',
                    width: 2,
                    value: date,
                    dashStyle: 'Dash' as Highcharts.DashStyleValue,
                    zIndex: 5
                }))
            },
            tooltip: {
                shared: true,
                style: {
                    fontSize: "1rem"
                }
            },
            series: [
                {
                    name: 'Depotwert',
                    type: 'line',
                    data: r.entries.map((x) => [x.date.valueOf(), Math.round(x.value * 100) / 100]),
                    animation: {
                        duration: 0
                    }
                }
            ],
            credits: {
                enabled: false,
            },
            legend: {
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

    private getMonthStarts(daysBack: number): number[] {
        const result: number[] = [];
        const now = new Date();
        const startDate = new Date(now.getTime() - daysBack * 24 * 60 * 60 * 1000);

        let current = new Date(startDate.getFullYear(), startDate.getMonth() + 1, 1);
        while (current <= now) {
            result.push(current.valueOf());
            current = new Date(current.getFullYear(), current.getMonth() + 1, 1);
        }
        return result;
    }
}
