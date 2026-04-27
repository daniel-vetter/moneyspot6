import { Component, OnInit, inject, signal } from '@angular/core';
import { PanelModule } from 'primeng/panel';
import { SummaryPageClient } from '../../server';
import { lastValueFrom } from 'rxjs';
import { EChartsOption } from 'echarts';
import { EchartComponent } from '../../common/echart/echart.component';
import { formatDateDe, formatEur, formatTimeAxisLabelDe } from '../../common/echart/chart-format';

interface TooltipParam {
    axisValue: string;
    color: string;
    seriesName: string;
    value: number;
}

@Component({
    selector: 'app-stock-history',
    imports: [PanelModule, EchartComponent],
    templateUrl: './stock-history.component.html',
    styleUrl: './stock-history.component.scss'
})
export class StockHistoryComponent implements OnInit {
    private summaryPageClient = inject(SummaryPageClient);

    protected readonly options = signal<EChartsOption | undefined>(undefined);

    async ngOnInit(): Promise<void> {
        const r = await lastValueFrom(this.summaryPageClient.getStockValueHistory());
        const dates = r.entries.map(x => x.date.valueOf());
        const values = r.entries.map(x => Math.round(x.value * 100) / 100);
        const labelEvery = Math.max(1, Math.round(dates.length / 8));
        const monthBoundaries = dates.filter(d => new Date(d).getDate() === 1).map(String);

        this.options.set({
            animation: false,
            grid: { left: 10, right: 20, top: 20, bottom: 30, containLabel: true },
            legend: { show: false },
            tooltip: {
                trigger: 'axis',
                formatter: (params: unknown) => StockHistoryComponent.tooltipFormatter(params as TooltipParam[])
            },
            xAxis: {
                type: 'category',
                data: dates.map(String),
                axisTick: { alignWithLabel: true, interval: labelEvery - 1 },
                axisLabel: {
                    fontSize: 11,
                    interval: labelEvery - 1,
                    showMinLabel: false,
                    formatter: (value: string) => formatTimeAxisLabelDe(Number(value))
                }
            },
            yAxis: {
                type: 'value',
                scale: true,
                axisLabel: {
                    fontSize: 11,
                    formatter: (value: number) => `${value.toLocaleString('de-DE')} €`
                }
            },
            series: [
                {
                    name: 'Depotwert',
                    type: 'line',
                    showSymbol: false,
                    data: values,
                    markLine: {
                        silent: true,
                        symbol: 'none',
                        label: { show: false },
                        lineStyle: { color: '#888', type: 'dashed' },
                        data: monthBoundaries.map(v => ({ xAxis: v }))
                    }
                }
            ]
        });
    }

    private static tooltipFormatter(params: TooltipParam[]): string {
        let html = `<b>${formatDateDe(Number(params[0].axisValue))}</b><br/>`;
        for (const p of params) {
            html += `<span style="color:${p.color}">●</span> ${p.seriesName}: <b>${formatEur(p.value)}</b>`;
        }
        return html;
    }
}
