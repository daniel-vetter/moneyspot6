import { Component, OnInit } from '@angular/core';
import { PanelModule } from 'primeng/panel';
import { SummaryPageClient } from '../../server';
import { lastValueFrom } from 'rxjs';
import { HighchartsChartModule } from 'highcharts-angular';
import Highcharts from 'highcharts';
import { ValueComponent } from '../../common/value/value.component';
import { CustomDatePipe } from '../../common/custom-date.pipe';

@Component({
    selector: 'app-goal',
    imports: [PanelModule, HighchartsChartModule, ValueComponent, CustomDatePipe],
    templateUrl: './goal.component.html',
    styleUrl: './goal.component.scss'
})
export class GoalComponent implements OnInit {
    Highcharts: typeof Highcharts = Highcharts;
    chart?: Highcharts.Options = undefined;

    targetValue = 0;
    targetDate!: Date;
    requiredSavingPerMonth = 0;

    constructor(private summaryPageClient: SummaryPageClient) { }

    async ngOnInit(): Promise<void> {
        const r = await lastValueFrom(this.summaryPageClient.getBankAccountGoal());
        this.targetValue = r.endBalance;
        this.targetDate = r.endDate;
        this.requiredSavingPerMonth = r.requiredSavingPerMonth;
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
                    name: 'Aktuell',
                    type: 'line',
                    data: r.actualHistory.map((x) => [x.date.valueOf(), Math.round(x.balance * 100) / 100]),
                    animation: {
                        duration: 0
                    }
                },
                {
                    name: 'Erwartet',
                    type: 'line',
                    dashStyle: 'ShortDash',
                    data: r.expectedHistory.map((x) => [x.date.valueOf(), Math.round(x.balance * 100) / 100]),
                    animation: {
                        duration: 0
                    }
                },
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
