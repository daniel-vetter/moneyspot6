import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule} from 'primeng/tooltip';
import { InflationDataClient, InflationDataEntryWithProjectionResponse } from '../../server';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { DecimalPipe } from '@angular/common';
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { ModalDialogService } from '../../common/modal-dialog.service';
import { DefaultRateDialogComponent } from './default-rate-dialog/default-rate-dialog.component';
import { InflationCalculatorComponent } from './inflation-calculator/inflation-calculator.component';
import { EChartsOption } from 'echarts';
import { EchartComponent } from '../../common/echart/echart.component';
import {SelectModule} from "primeng/select";
import {TabsModule} from "primeng/tabs";
import {ProgressSpinnerModule} from "primeng/progressspinner";

interface InflationTooltipParam {
    name: string;
    color: string;
    seriesName: string;
    value: number | undefined;
}

interface DisplayEntry {
    year: number;
    month?: number;
    indexValue: number;
    change: number | undefined;
    isProjected: boolean;
}

interface ProjectionOption {
    label: string;
    value: number;
}

@Component({
    selector: 'app-inflation-data',
    imports: [FormsModule, PanelModule, TableModule, DecimalPipe, ButtonModule, EchartComponent, TooltipModule, SelectModule, TabsModule, ProgressSpinnerModule],
    templateUrl: './inflation-data.component.html',
    styleUrl: './inflation-data.component.scss'
})
export class InflationDataComponent implements OnInit {
    private inflationDataClient = inject(InflationDataClient);
    private modalDialogService = inject(ModalDialogService);
    private dialogRef: DynamicDialogRef | undefined;

    entries: DisplayEntry[] = [];
    yearlyEntries: DisplayEntry[] = [];
    defaultRate: number = 0;
    loading: boolean = false;

    private rawEntries: InflationDataEntryWithProjectionResponse[] = [];
    private entryMap = new Map<string, InflationDataEntryWithProjectionResponse>();

    chartOptions: EChartsOption | undefined;
    yearlyChartOptions: EChartsOption | undefined;

    // Projection settings
    projectionOptions: ProjectionOption[] = [
        { label: 'Nur reale Werte', value: 0 },
        { label: '5 Jahre', value: 5 },
        { label: '10 Jahre', value: 10 },
        { label: '20 Jahre', value: 20 },
        { label: '50 Jahre', value: 50 }
    ];
    selectedProjectionYears: number = 5;

    async ngOnInit(): Promise<void> {
        await this.update();
    }

    async update() {
        this.loading = true;
        try {
            const response = await lastValueFrom(
                this.inflationDataClient.getAll(this.selectedProjectionYears)
            );
            this.rawEntries = response.entries;
            this.defaultRate = response.defaultRate;

            // Build a map for quick lookup
            this.entryMap.clear();
            for (const entry of this.rawEntries) {
                this.entryMap.set(`${entry.year}-${entry.month}`, entry);
            }

            this.calculateMonthlyEntries();
            this.calculateYearlyEntries();
            this.updateCharts();
        } finally {
            this.loading = false;
        }
    }

    async onProjectionYearsChange(years: number) {
        this.selectedProjectionYears = years;
        await this.update();
    }

    async onConfigureDefaultRateClicked() {
        this.dialogRef = this.modalDialogService.open(DefaultRateDialogComponent, {
            header: "Standard-Inflation konfigurieren",
            width: "500px",
            data: {
                defaultRate: this.defaultRate
            }
        });

        const result = await firstValueFrom(this.dialogRef.onClose);
        if (result) {
            await this.update();
        }
    }

    onCalculatorClicked() {
        this.dialogRef = this.modalDialogService.open(InflationCalculatorComponent, {
            header: "Inflationsrechner",
            width: "500px",
            closable: true,
            closeOnEscape: true
        });
    }

    private calculateMonthlyEntries() {
        this.entries = this.rawEntries.map(entry => {
            let previousMonth = entry.month - 1;
            let previousYear = entry.year;

            if (previousMonth === 0) {
                previousMonth = 12;
                previousYear = entry.year - 1;
            }

            const previousMonthEntry = this.entryMap.get(`${previousYear}-${previousMonth}`);
            const change = previousMonthEntry
                ? ((entry.indexValue - previousMonthEntry.indexValue) / previousMonthEntry.indexValue) * 100
                : undefined;

            return {
                year: entry.year,
                month: entry.month,
                indexValue: entry.indexValue,
                change,
                isProjected: entry.isProjected
            };
        }).sort((a, b) => {
            if (a.year !== b.year) return b.year - a.year;
            return (b.month || 0) - (a.month || 0);
        });
    }

    private calculateYearlyEntries() {
        // Group entries by year
        const yearMap = new Map<number, InflationDataEntryWithProjectionResponse[]>();

        for (const entry of this.rawEntries) {
            if (!yearMap.has(entry.year)) {
                yearMap.set(entry.year, []);
            }
            yearMap.get(entry.year)!.push(entry);
        }

        // Calculate yearly averages
        const years = Array.from(yearMap.keys()).sort((a, b) => b - a);
        const yearlyAverages = new Map<number, number>();
        const yearProjectedStatus = new Map<number, boolean>();

        for (const year of years) {
            const yearEntries = yearMap.get(year)!;
            const average = yearEntries.reduce((sum, e) => sum + e.indexValue, 0) / yearEntries.length;
            const isProjected = yearEntries.every(e => e.isProjected);
            yearlyAverages.set(year, average);
            yearProjectedStatus.set(year, isProjected);
        }

        // Build yearly entries with year-over-year change
        this.yearlyEntries = years.map(year => {
            const indexValue = yearlyAverages.get(year)!;
            const previousYearAverage = yearlyAverages.get(year - 1);

            const change = previousYearAverage
                ? ((indexValue - previousYearAverage) / previousYearAverage) * 100
                : undefined;

            return {
                year,
                indexValue,
                change,
                isProjected: yearProjectedStatus.get(year)!
            };
        });
    }

    getMonthName(month: number): string {
        const months = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni',
                       'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];
        return months[month - 1] || '';
    }

    private updateCharts() {
        this.updateMonthlyChart();
        this.updateYearlyChart();
    }

    private updateMonthlyChart() {
        const sortedEntries = [...this.entries].sort((a, b) => {
            if (a.year !== b.year) return a.year - b.year;
            return (a.month || 0) - (b.month || 0);
        });

        const categories = sortedEntries.map(e => `${this.getMonthName(e.month!)} ${e.year}`);
        const actualData = sortedEntries.map(e => e.isProjected ? undefined : e.indexValue);
        const projectedData = sortedEntries.map((e, i) => {
            if (!e.isProjected) {
                const isLastActual = i < sortedEntries.length - 1 && sortedEntries[i + 1].isProjected;
                return isLastActual ? e.indexValue : undefined;
            }
            return e.indexValue;
        });
        const changeData = sortedEntries.map(e => e.change);

        this.chartOptions = InflationDataComponent.buildChart(
            categories,
            actualData,
            projectedData,
            changeData,
            'Veränderung zum Vormonat'
        );
    }

    private updateYearlyChart() {
        const sortedEntries = [...this.yearlyEntries].sort((a, b) => a.year - b.year);

        const categories = sortedEntries.map(e => e.year.toString());
        const actualData = sortedEntries.map(e => e.isProjected ? undefined : e.indexValue);
        const projectedData = sortedEntries.map((e, i) => {
            if (!e.isProjected) {
                const isLastActual = i < sortedEntries.length - 1 && sortedEntries[i + 1].isProjected;
                return isLastActual ? e.indexValue : undefined;
            }
            return e.indexValue;
        });
        const changeData = sortedEntries.map(e => e.change);

        this.yearlyChartOptions = InflationDataComponent.buildChart(
            categories,
            actualData,
            projectedData,
            changeData,
            'Veränderung zum Vorjahr'
        );
    }

    private static buildChart(
        categories: string[],
        actual: (number | undefined)[],
        projected: (number | undefined)[],
        change: (number | undefined)[],
        changeSeriesName: string
    ): EChartsOption {
        const labelInterval = Math.max(0, Math.floor(categories.length / 20) - 1);
        return {
            animation: false,
            grid: { left: 10, right: 10, top: 50, bottom: 80, containLabel: true },
            legend: { top: 0 },
            tooltip: {
                trigger: 'axis',
                formatter: (params: unknown) => InflationDataComponent.tooltipFormatter(params as InflationTooltipParam[], changeSeriesName)
            },
            xAxis: {
                type: 'category',
                data: categories,
                axisLabel: { rotate: -45, interval: labelInterval }
            },
            yAxis: [
                { type: 'value', scale: true },
                { type: 'value', position: 'right', axisLabel: { formatter: (v: number) => `${v.toFixed(1)}%` } }
            ],
            series: [
                {
                    name: 'Index-Wert (Reale Daten)',
                    type: 'line',
                    yAxisIndex: 0,
                    color: '#2196F3',
                    showSymbol: false,
                    data: actual
                },
                {
                    name: 'Index-Wert (Geschätzt)',
                    type: 'line',
                    yAxisIndex: 0,
                    color: '#FF9800',
                    lineStyle: { type: 'dashed' },
                    showSymbol: false,
                    data: projected
                },
                {
                    name: changeSeriesName,
                    type: 'bar',
                    yAxisIndex: 1,
                    data: change
                }
            ]
        };
    }

    private static tooltipFormatter(params: InflationTooltipParam[], changeSeriesName: string): string {
        if (params.length === 0) return '';
        let html = `<b>${params[0].name}</b><br/>`;
        for (const p of params) {
            if (p.value === undefined) continue;
            const isPercent = p.seriesName === changeSeriesName;
            const formatted = isPercent ? `${p.value.toFixed(2)}%` : p.value.toFixed(2);
            html += `<span style="color:${p.color}">●</span> ${p.seriesName}: <b>${formatted}</b><br/>`;
        }
        return html;
    }
}
