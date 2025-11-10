import { Component, OnInit, inject } from '@angular/core';
import * as Highcharts from 'highcharts';
import { HighchartsChartModule } from 'highcharts-angular';
import { AccountHistoryClient } from '../server';
import { lastValueFrom } from 'rxjs';
import { SplitButtonModule } from 'primeng/splitbutton';
import { FormsModule } from '@angular/forms';
import { ButtonGroupModule } from 'primeng/buttongroup';
import { PanelModule } from 'primeng/panel';
import { DatePickerModule } from 'primeng/datepicker';
import { DateRange, DateRangePickerComponent } from "../common/date-range-picker/date-range-picker.component";
import { DateRangePresetsComponent } from "../common/date-range-presets/date-range-presets.component";
import { ActivatedRoute } from "@angular/router";
import { ToggleButtonModule } from "primeng/togglebutton";


@Component({
    selector: 'app-history',
    imports: [HighchartsChartModule, DatePickerModule, SplitButtonModule, FormsModule, ButtonGroupModule, PanelModule, DateRangePickerComponent, DateRangePresetsComponent, ToggleButtonModule],
    templateUrl: './history.component.html',
    styleUrl: './history.component.scss'
})
export class HistoryComponent implements OnInit {
    private accountHistoryClient = inject(AccountHistoryClient);
    private activatedRoute = inject(ActivatedRoute);

    Highcharts: typeof Highcharts = Highcharts;
    charts: (Highcharts.Options & { index: number })[] = [];
    dateRange: DateRange | undefined;
    startFromZero: boolean = false;

    async ngOnInit(): Promise<void> {
        this.activatedRoute.queryParams.subscribe(async (x) => {
            this.dateRange = DateRange.parse(x['dateRange']);
            await this.update();
        });
    }

    convertDate(date: Date | undefined, addDay = false): string | undefined {
        if (date === undefined) {
            return undefined;
        }
        const converted = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0, 0));
        if (addDay) {
            converted.setDate(converted.getDate() + 1);
        }
        return converted.toISOString().substring(0, 10);
    }

    async update() {
        const start = this.convertDate(this.dateRange?.start);
        const end = this.convertDate(this.dateRange?.end, true);
        const result = await lastValueFrom(this.accountHistoryClient.get(start, end));

        this.charts = [];
        this.charts.push({
            index: this.charts.length,
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
                    stacking: "normal",
                    marker: {
                        enabled: false
                    }
                },
                line: {
                    marker: {
                        enabled: false
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
                    name: 'Konten',
                    type: 'area',
                    data: result.map((x) => [x.date.valueOf(), Math.round(x.balance * 100) / 100]),
                    threshold: this.startFromZero ? 0 : undefined,
                    fillOpacity: 0.15,
                    animation: {
                        duration: 0
                    }
                },
                {
                    name: 'Aktien',
                    type: 'area',
                    data: result.map((x) => [x.date.valueOf(), Math.round(x.stockValue * 100) / 100]),
                    threshold: this.startFromZero ? 0 : undefined,
                    fillOpacity: 0.15,
                    animation: {
                        duration: 0
                    }
                },
                {
                    name: 'Investment',
                    type: 'line',
                    data: result.map((x) => [x.date.valueOf(), Math.round(x.stockInvested * 100) / 100]),
                    threshold: this.startFromZero ? 0 : undefined,
                    animation: {
                        duration: 0
                    }
                },
            ]
        });
    }

    protected async onStartFromZeroChanged() {
        await this.update();
    }
}
