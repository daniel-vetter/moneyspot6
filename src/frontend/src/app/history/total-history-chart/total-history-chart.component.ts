import { Component, computed, input } from '@angular/core';
import { EChartsOption } from 'echarts';
import { AccountHistoryBalanceResponse } from '../../server';
import { EchartComponent } from '../../common/echart/echart.component';
import { DateRange } from '../../common/date-range-picker/date-range-picker.component';
import { formatDateDe, formatEur, formatTimeAxisLabelDe } from '../../common/echart/chart-format';

interface TooltipParam {
    axisValue: number;
    seriesName: string;
    color: string;
    value: [number, number];
}

@Component({
    selector: 'app-total-history-chart',
    template: '<app-echart [options]="options()"/>',
    styles: [':host { display: block; width: 100%; height: 100%; }'],
    imports: [EchartComponent]
})
export class TotalHistoryChartComponent {
    readonly data = input.required<AccountHistoryBalanceResponse[]>();
    readonly startFromZero = input(false);
    readonly visibleRange = input<DateRange | undefined>(undefined);

    readonly options = computed<EChartsOption>(() => {
        const result = this.data();
        const startFromZero = this.startFromZero();
        const range = this.visibleRange();
        const accounts: [number, number][] = result.map(x => [x.date.valueOf(), Math.round(x.balance * 100) / 100]);
        const stocks: [number, number][] = result.map(x => [x.date.valueOf(), Math.round(x.stockValue * 100) / 100]);
        const investment: [number, number][] = result.map(x => [x.date.valueOf(), Math.round(x.stockInvested * 100) / 100]);
        const startValue = range?.start.valueOf() ?? result[0]?.date.valueOf();
        const endValue = range?.end.valueOf() ?? result[result.length - 1]?.date.valueOf();

        return {
            tooltip: {
                trigger: 'axis',
                formatter: (params: unknown) => TotalHistoryChartComponent.tooltipFormatter(params as TooltipParam[])
            },
            legend: { data: ['Aktien', 'Konten', 'Investment'], bottom: 0 },
            grid: { left: 10, right: 20, top: 20, bottom: 60, containLabel: true },
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
                scale: !startFromZero,
                min: startFromZero ? 0 : undefined,
                axisLabel: {
                    fontSize: 14,
                    formatter: (value: number) => `${value.toLocaleString('de-DE')} €`
                }
            },
            series: [
                {
                    name: 'Aktien',
                    type: 'line',
                    stack: 'total',
                    showSymbol: false,
                    areaStyle: { opacity: 0.15 },
                    data: stocks
                },
                {
                    name: 'Konten',
                    type: 'line',
                    stack: 'total',
                    showSymbol: false,
                    areaStyle: { opacity: 0.15 },
                    data: accounts
                },
                {
                    name: 'Investment',
                    type: 'line',
                    showSymbol: false,
                    data: investment
                }
            ]
        };
    });

    private static tooltipFormatter(params: TooltipParam[]): string {
        let html = `<b>Datum: ${formatDateDe(params[0].axisValue)}</b><br/><br/>`;
        html += `<table style="border-collapse: collapse; width: 100%;">`;

        let sum = 0;
        let stockValue: number | undefined;
        let investmentValue: number | undefined;

        for (const p of params) {
            if (p.seriesName === 'Konten' || p.seriesName === 'Aktien') {
                const v = p.value[1];
                sum += v;
                if (p.seriesName === 'Aktien') stockValue = v;
                html += `<tr>
                    <td style="padding: 2px 8px 2px 0;"><span style="color:${p.color}">●</span> ${p.seriesName}</td>
                    <td style="padding: 2px 0; text-align: right;"><b>${formatEur(v)}</b></td>
                </tr>`;
            }
        }

        html += `<tr style="border-top: 1px solid #ccc;">
            <td style="padding: 4px 8px 12px 0;"><b>Summe</b></td>
            <td style="padding: 4px 0 12px 0; text-align: right;"><b>${formatEur(sum)}</b></td>
        </tr>`;

        for (const p of params) {
            if (p.seriesName === 'Investment') {
                investmentValue = p.value[1];
                html += `<tr style="border-top: 2px solid #ccc;">
                    <td style="padding: 12px 8px 2px 0;"><span style="color:${p.color}">●</span> ${p.seriesName}</td>
                    <td style="padding: 12px 0 2px 0; text-align: right;"><b>${formatEur(p.value[1])}</b></td>
                </tr>`;
            }
        }

        if (stockValue !== undefined && investmentValue !== undefined) {
            const profit = stockValue - investmentValue;
            html += `<tr>
                <td style="padding: 2px 8px 2px 0;"><span style="color: black;">●</span> Gewinn</td>
                <td style="padding: 2px 0; text-align: right;"><b>${formatEur(profit)}</b></td>
            </tr>`;
        }

        html += `</table>`;
        return html;
    }
}
