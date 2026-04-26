import {AfterViewInit, Component, ElementRef, OnDestroy, effect, inject, input, viewChild} from '@angular/core';
import * as echarts from 'echarts';
import {EChartsOption} from 'echarts';
import {ThemeService} from '../theme.service';

const colorsByTheme: Record<string, string[]> = {
    emerald: ['#10b981', '#06b6d4', '#8b5cf6', '#f59e0b', '#ef4444', '#ec4899', '#6366f1', '#14b8a6', '#f97316'],
    neon: ['#80ff04', '#00ddfa', '#a855f7', '#ff7407', '#f43f5e', '#ec4899', '#6366f1', '#21dc52', '#facc15'],
    light: ['#3b82f6', '#10b981', '#f59e0b', '#8b5cf6', '#ef4444', '#06b6d4', '#ec4899', '#6366f1', '#f97316'],
};

@Component({
    selector: 'app-echart',
    template: '<div #host class="echart-host"></div>',
    styles: [`
        :host { display: block; width: 100%; height: 100%; }
        .echart-host { width: 100%; height: 100%; }
    `]
})
export class EchartComponent implements AfterViewInit, OnDestroy {
    readonly options = input<EChartsOption | null>(null);
    private readonly hostEl = viewChild.required<ElementRef<HTMLDivElement>>('host');

    private themeService = inject(ThemeService);
    private chart?: echarts.ECharts;
    private resizeObserver?: ResizeObserver;

    constructor() {
        effect(() => {
            const opts = this.options();
            if (this.chart && opts) {
                this.chart.setOption(opts);
            }
        });
    }

    ngAfterViewInit(): void {
        const dark = this.themeService.isDark;
        const themeId = this.themeService.current;
        const el = this.hostEl().nativeElement;
        this.chart = echarts.init(el, dark ? 'dark' : undefined);
        this.chart.setOption(EchartComponent.themeOptions(dark, themeId));
        this.resizeObserver = new ResizeObserver(() => this.chart?.resize());
        this.resizeObserver.observe(el);
        const opts = this.options();
        if (opts) {
            this.chart.setOption(opts);
        }
    }

    ngOnDestroy(): void {
        this.chart?.dispose();
        this.resizeObserver?.disconnect();
    }

    private static themeOptions(dark: boolean, themeId: string): EChartsOption {
        const labelColor = dark ? '#94a3b8' : '#64748b';
        const lineColor = dark ? '#334155' : '#cbd5e1';
        const splitLineColor = dark ? '#1e293b' : '#e2e8f0';
        const tooltipBg = dark ? '#1e293b' : '#ffffff';
        const tooltipText = dark ? '#e2e8f0' : '#1e293b';
        return {
            backgroundColor: 'transparent',
            color: colorsByTheme[themeId] ?? colorsByTheme['emerald'],
            textStyle: {color: labelColor},
            xAxis: {
                axisLine: {lineStyle: {color: lineColor}},
                axisTick: {lineStyle: {color: lineColor}},
                splitLine: {lineStyle: {color: splitLineColor}},
                axisLabel: {color: labelColor},
            },
            yAxis: {
                axisLine: {lineStyle: {color: lineColor}},
                axisTick: {lineStyle: {color: lineColor}},
                splitLine: {lineStyle: {color: splitLineColor}},
                axisLabel: {color: labelColor},
            },
            legend: {
                textStyle: {color: labelColor},
            },
            tooltip: {
                backgroundColor: tooltipBg,
                borderColor: lineColor,
                textStyle: {color: tooltipText},
            },
        };
    }
}
