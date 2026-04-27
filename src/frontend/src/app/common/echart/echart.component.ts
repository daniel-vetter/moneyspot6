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
    readonly options = input<EChartsOption | undefined>(undefined);
    private readonly hostEl = viewChild.required<ElementRef<HTMLDivElement>>('host');

    private themeService = inject(ThemeService);
    private chart?: echarts.ECharts;
    private resizeObserver?: ResizeObserver;
    private static themeRegistered = false;

    constructor() {
        effect(() => {
            const opts = this.options();
            if (this.chart && opts) {
                this.chart.setOption(opts);
            }
        });
    }

    ngAfterViewInit(): void {
        EchartComponent.ensureThemeRegistered(this.themeService);
        const el = this.hostEl().nativeElement;
        this.chart = echarts.init(el, 'moneyspot');
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

    private static ensureThemeRegistered(themeService: ThemeService): void {
        if (EchartComponent.themeRegistered) return;
        const dark = themeService.isDark;
        const themeId = themeService.current;
        const labelColor = dark ? '#94a3b8' : '#64748b';
        const splitLineColor = dark ? '#1e293b' : '#e2e8f0';
        const axisLineColor = dark ? '#0f172a' : '#cbd5e1';
        const tooltipBg = dark ? '#1e293b' : '#ffffff';
        const tooltipBorder = dark ? '#334155' : '#cbd5e1';
        const tooltipText = dark ? '#e2e8f0' : '#1e293b';
        const axis = {
            axisLine: {lineStyle: {color: axisLineColor}},
            axisTick: {lineStyle: {color: axisLineColor}},
            splitLine: {lineStyle: {color: splitLineColor}},
            axisLabel: {color: labelColor},
        };
        echarts.registerTheme('moneyspot', {
            backgroundColor: 'transparent',
            color: colorsByTheme[themeId] ?? colorsByTheme['emerald'],
            textStyle: {color: labelColor},
            title: {textStyle: {color: dark ? '#e2e8f0' : '#1e293b'}},
            categoryAxis: axis,
            valueAxis: axis,
            timeAxis: axis,
            logAxis: axis,
            legend: {textStyle: {color: labelColor}},
            tooltip: {
                backgroundColor: tooltipBg,
                borderColor: tooltipBorder,
                textStyle: {color: tooltipText},
            },
            sankey: {
                label: {
                    color: dark ? '#e2e8f0' : '#1e293b',
                    textBorderWidth: 0,
                    textBorderColor: 'transparent',
                },
                itemStyle: {borderWidth: 0, borderColor: 'transparent'},
            },
        });
        EchartComponent.themeRegistered = true;
    }
}
