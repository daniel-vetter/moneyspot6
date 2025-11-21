import {Component, OnInit, inject} from '@angular/core';
import * as Highcharts from 'highcharts';
import {HighchartsChartModule} from 'highcharts-angular';
import {AccountHistoryClient} from '../server';
import {lastValueFrom} from 'rxjs';
import {SplitButtonModule} from 'primeng/splitbutton';
import {FormsModule} from '@angular/forms';
import {ButtonGroupModule} from 'primeng/buttongroup';
import {PanelModule} from 'primeng/panel';
import {DatePickerModule} from 'primeng/datepicker';
import {DateRange, DateRangePickerComponent} from "../common/date-range-picker/date-range-picker.component";
import {DateRangePresetsComponent} from "../common/date-range-presets/date-range-presets.component";
import {ActivatedRoute} from "@angular/router";
import {ToggleButtonModule} from "primeng/togglebutton";
import {Point, Tooltip, TooltipOptions} from "highcharts";


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
                useHTML: true,
                style: {
                    fontSize: "1rem"
                },
                formatter: function () {
                    return HistoryComponent.tooltipFormatter(this);
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
                {
                    name: 'Gewinn',
                    type: 'area',
                    data: result.map((x) => [x.date.valueOf(), Math.round((x.stockValue - x.stockInvested) * 100) / 100]),
                    threshold: this.startFromZero ? 0 : undefined,
                    animation: {
                        duration: 0
                    }
                },
            ]
        });
    }

    private static tooltipFormatter(point: Point): string {
        let tooltipText = `<b>Datum: ${Highcharts.dateFormat('%d.%m.%Y', point.x)}</b><br/><br/>`;
        tooltipText += `<table style="border-collapse: collapse; width: 100%;">`;

        let sum = 0;
        point.points?.forEach((point) => {
            if (point.series.name === 'Konten' || point.series.name === 'Aktien') {
                sum += point.y as number;
                tooltipText += `<tr>
                    <td style="padding: 2px 8px 2px 0;"><span style="color:${point.color}">\u25CF</span> ${point.series.name}</td>
                    <td style="padding: 2px 0; text-align: right;"><b>${Highcharts.numberFormat(point.y as number, 2, ',', '.')}</b></td>
                </tr>`;
            }
        });

        tooltipText += `<tr style="border-top: 1px solid #ccc;">
            <td style="padding: 4px 8px 2px 0;"><b>Summe</b></td>
            <td style="padding: 4px 0 2px 0; text-align: right;"><b>${Highcharts.numberFormat(sum, 2, ',', '.')}</b></td>
        </tr>`;

        point.points?.forEach((point) => {
            if (point.series.name === 'Investment') {
                tooltipText += `<tr style="border-top: 2px solid #ccc;">
                    <td style="padding: 24px 8px 2px 0;"><span style="color:${point.color}">\u25CF</span> ${point.series.name}</td>
                    <td style="padding: 24px 0 2px 0; text-align: right;"><b>${Highcharts.numberFormat(point.y as number, 2, ',', '.')}</b></td>
                </tr>`;
            }
        });

        point.points?.forEach((point) => {
            if (point.series.name === 'Gewinn') {
                tooltipText += `<tr>
                    <td style="padding: 4px 8px 2px 0;"><span style="color:${point.color}">\u25CF</span> ${point.series.name}</td>
                    <td style="padding: 4px 0 2px 0; text-align: right;"><b>${Highcharts.numberFormat(point.y as number, 2, ',', '.')}</b></td>
                </tr>`;
            }
        });

        tooltipText += `</table>`;
        return tooltipText;
    }

    protected async onStartFromZeroChanged() {
        await this.update();
    }
}
