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
    standalone: true,
    imports: [PanelModule, HighchartsChartModule, ValueComponent, CustomDatePipe],
    templateUrl: './goal.component.html',
    styleUrl: './goal.component.scss',
})
export class GoalComponent implements OnInit {
    Highcharts: typeof Highcharts = Highcharts;
    chart: Highcharts.Options = {
        series: [
            {
                type: 'line',
            },
            {
                type: 'line',
            },
        ],
    };

    targetValue = 0;
    targetDate!: Date;
    requiredSavingPerMonth = 0;

    constructor(private summaryPageClient: SummaryPageClient) {}

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
            },
            xAxis: {
                type: 'datetime',
            },
            series: [
                {
                    name: 'Aktuell',
                    type: 'line',
                    data: r.actualHistory.map((x) => [x.date.valueOf(), x.balance / 100]),
                },
                {
                    name: 'Erwartet',
                    type: 'line',
                    dashStyle: 'ShortDash',
                    data: r.expectedHistory.map((x) => [x.date.valueOf(), x.balance / 100]),
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
