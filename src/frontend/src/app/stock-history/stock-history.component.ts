import { Component, OnInit, inject, signal } from '@angular/core';
import { StockChartPageClient, StockPriceInterval, StockResponse } from '../server';
import { lastValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { PanelModule } from 'primeng/panel';
import { SelectModule } from 'primeng/select';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ButtonModule } from 'primeng/button';
import { EChartsOption } from 'echarts';
import { EchartComponent } from '../common/echart/echart.component';
import { formatDateDe, formatEur } from '../common/echart/chart-format';

interface CandleTooltipParam {
    axisValue: string;
    color: string;
    seriesName: string;
    data: [number, number, number, number, number];
}

type RangePreset = '1d' | '7d' | '1m' | '3m' | '6m' | '1y' | 'all';

@Component({
    selector: 'app-stock-history',
    imports: [SelectModule, FormsModule, PanelModule, ProgressSpinnerModule, ButtonModule, EchartComponent],
    templateUrl: './stock-history.component.html',
    styleUrl: './stock-history.component.scss'
})
export class StockHistoryComponent implements OnInit {
    private stockChartPageClient = inject(StockChartPageClient);

    possibleStocks: StockResponse[] | undefined = undefined;
    selectedStockId?: number;

    possibleIntervals = ['Täglich', '5 Minuten'];
    selectedInterval = '5 Minuten';

    protected readonly options = signal<EChartsOption | null>(null);
    protected readonly loading = signal(false);
    protected readonly activePreset = signal<RangePreset>('7d');

    private timestamps: number[] = [];
    private firstTs = 0;
    private lastTs = 0;

    async ngOnInit(): Promise<void> {
        await this.update();
    }

    async update(): Promise<void> {
        this.loading.set(true);
        this.options.set(null);

        this.possibleStocks = await lastValueFrom(this.stockChartPageClient.getStocks());
        if (this.possibleStocks.length > 0 && this.selectedStockId === undefined) {
            this.selectedStockId = this.possibleStocks[0].id;
        }

        const interval = this.selectedInterval === '5 Minuten' ? StockPriceInterval.FiveMinutes : StockPriceInterval.Daily;
        const data = await lastValueFrom(this.stockChartPageClient.getHistory(this.selectedStockId, undefined, undefined, interval));

        const timestamps = data.map(x => x.timestamp.valueOf());
        const candleData = data.map(x => [
            Math.round(x.open * 100) / 100,
            Math.round(x.close * 100) / 100,
            Math.round(x.low * 100) / 100,
            Math.round(x.high * 100) / 100,
        ]);

        this.timestamps = timestamps;
        this.firstTs = timestamps[0] ?? Date.now();
        this.lastTs = timestamps[timestamps.length - 1] ?? Date.now();

        const [startIdx, endIdx] = this.computeRangeIndices(this.activePreset());

        this.options.set({
            animation: false,
            grid: { left: 10, right: 20, top: 20, bottom: 70, containLabel: true },
            tooltip: {
                trigger: 'axis',
                axisPointer: { type: 'cross' },
                formatter: (params: unknown) => this.tooltipFormatter(params as CandleTooltipParam[])
            },
            xAxis: {
                type: 'category',
                data: timestamps.map(String),
                axisLabel: {
                    formatter: (value: string) => StockHistoryComponent.formatAxisLabel(Number(value), this.selectedInterval)
                }
            },
            yAxis: {
                type: 'value',
                scale: true,
                axisLabel: { formatter: (v: number) => `${v.toLocaleString('de-DE')} €` }
            },
            dataZoom: [
                { type: 'inside', startValue: startIdx, endValue: endIdx },
                { type: 'slider', startValue: startIdx, endValue: endIdx, height: 30, bottom: 10 }
            ],
            series: [{
                type: 'candlestick',
                name: 'Preis',
                data: candleData,
                itemStyle: {
                    color: '#22c55e',
                    color0: '#ef4444',
                    borderColor: '#16a34a',
                    borderColor0: '#dc2626'
                }
            }]
        });

        this.loading.set(false);
    }

    setRange(preset: RangePreset): void {
        this.activePreset.set(preset);
        const opts = this.options();
        if (!opts) return;
        const [startIdx, endIdx] = this.computeRangeIndices(preset);
        this.options.set({
            ...opts,
            dataZoom: [
                { type: 'inside', startValue: startIdx, endValue: endIdx },
                { type: 'slider', startValue: startIdx, endValue: endIdx, height: 30, bottom: 10 }
            ]
        });
    }

    private computeRangeIndices(preset: RangePreset): [number, number] {
        const lastIdx = this.timestamps.length - 1;
        if (lastIdx < 0) return [0, 0];
        const day = 86400000;
        const end = this.lastTs;
        let cutoff: number;
        switch (preset) {
            case '1d': cutoff = end - day; break;
            case '7d': cutoff = end - 7 * day; break;
            case '1m': cutoff = end - 30 * day; break;
            case '3m': cutoff = end - 90 * day; break;
            case '6m': cutoff = end - 180 * day; break;
            case '1y': cutoff = end - 365 * day; break;
            case 'all': return [0, lastIdx];
        }
        const startIdx = this.timestamps.findIndex(t => t >= cutoff);
        return [startIdx === -1 ? lastIdx : startIdx, lastIdx];
    }

    private tooltipFormatter(params: CandleTooltipParam[]): string {
        const p = params[0];
        if (!p) return '';
        const [open, close, low, high] = p.data;
        const ts = Number(p.axisValue);
        const label = StockHistoryComponent.formatTooltipDate(ts, this.selectedInterval);
        return `<b>${label}</b><br/>` +
            `Open: <b>${formatEur(open)}</b><br/>` +
            `High: <b>${formatEur(high)}</b><br/>` +
            `Low: <b>${formatEur(low)}</b><br/>` +
            `Close: <b>${formatEur(close)}</b>`;
    }

    private static formatAxisLabel(timestamp: number, interval: string): string {
        const d = new Date(timestamp);
        if (interval === '5 Minuten') {
            return `${d.getDate()}.${(d.getMonth() + 1).toString().padStart(2, '0')}. ${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
        }
        return formatDateDe(timestamp);
    }

    private static formatTooltipDate(timestamp: number, interval: string): string {
        const d = new Date(timestamp);
        if (interval === '5 Minuten') {
            return `${formatDateDe(timestamp)} ${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
        }
        return formatDateDe(timestamp);
    }
}
