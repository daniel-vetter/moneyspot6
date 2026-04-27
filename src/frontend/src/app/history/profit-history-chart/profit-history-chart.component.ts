import {Component, computed, input} from '@angular/core';
import {EChartsOption} from 'echarts';
import {AccountHistoryBalanceResponse} from '../../server';
import {EchartComponent} from '../../common/echart/echart.component';
import {DateRange} from '../../common/date-range-picker/date-range-picker.component';
import {formatDateDe, formatEur, formatTimeAxisLabelDe} from '../../common/echart/chart-format';

interface TooltipParam {
    axisValue: number;
    seriesName: string;
    color: string;
    value: [number, number];
}

@Component({
    selector: 'app-profit-history-chart',
    template: '<app-echart [options]="options()"/>',
    styles: [':host { display: block; width: 100%; height: 100%; }'],
    imports: [EchartComponent]
})
export class ProfitHistoryChartComponent {
    readonly data = input.required<AccountHistoryBalanceResponse[]>();
    readonly visibleRange = input<DateRange | undefined>(undefined);

    readonly options = computed<EChartsOption>(() => {
        const range = this.visibleRange();
        const data = this.data();
        const profit: [number, number][] = data.map(x => [
            x.date.valueOf(),
            Math.round((x.stockValue - x.stockInvested) * 100) / 100
        ]);
        const startValue = range?.start.valueOf() ?? data[0]?.date.valueOf();
        const endValue = range?.end.valueOf() ?? data[data.length - 1]?.date.valueOf();

        return {
            tooltip: {
                trigger: 'axis',
                formatter: (params: unknown) => ProfitHistoryChartComponent.tooltipFormatter(params as TooltipParam[])
            },
            grid: {left: 10, right: 20, top: 30, bottom: 40, containLabel: true},
            dataZoom: [{
                type: 'inside',
                xAxisIndex: 0,
                zoomLock: true,
                moveOnMouseMove: false,
                moveOnMouseWheel: false,
                startValue,
                endValue
            }],
            xAxis: {
                type: 'time',
                axisLabel: {
                    fontSize: 14,
                    formatter: formatTimeAxisLabelDe
                }
            },
            yAxis: {
                type: 'value',
                scale: true,
                axisLabel: {
                    fontSize: 14,
                    formatter: (value: number) => `${value.toLocaleString('de-DE')} €`
                }
            },
            series: [
                {
                    name: 'Aktiengewinn',
                    type: 'line',
                    showSymbol: false,
                    color: '#22c55e',
                    areaStyle: {opacity: 0.3, origin: 0},
                    data: profit,
                    markLine: {
                        symbol: 'none',
                        silent: true,
                        lineStyle: {color: '#888', type: 'dashed'},
                        data: [{yAxis: 0}]
                    }
                }
            ]
        };
    });

    private static tooltipFormatter(params: TooltipParam[]): string {
        let html = `<b>Datum: ${formatDateDe(params[0].axisValue)}</b><br/><br/>`;
        html += `<table style="border-collapse: collapse; width: 100%;">`;
        for (const p of params) {
            html += `<tr>
                <td style="padding: 2px 8px 2px 0;"><span style="color:${p.color}">●</span> ${p.seriesName}</td>
                <td style="padding: 2px 0; text-align: right;"><b>${formatEur(p.value[1])}</b></td>
            </tr>`;
        }
        html += `</table>`;
        return html;
    }
}
