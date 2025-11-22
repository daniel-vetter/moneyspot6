import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule} from 'primeng/tooltip';
import { InflationDataClient, InflationDataEntryWithProjectionResponse } from '../../server';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { DecimalPipe } from '@angular/common';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { DefaultRateDialogComponent } from './default-rate-dialog/default-rate-dialog.component';
import { InflationCalculatorComponent } from './inflation-calculator/inflation-calculator.component';
import { HighchartsChartModule } from 'highcharts-angular';
import * as Highcharts from 'highcharts';
import {SelectModule} from "primeng/select";
import {TabsModule} from "primeng/tabs";
import {ProgressSpinnerModule} from "primeng/progressspinner";

interface DisplayEntry {
    year: number;
    month?: number;
    indexValue: number;
    change: number | null;
    isProjected: boolean;
}

interface ProjectionOption {
    label: string;
    value: number;
}

@Component({
    selector: 'app-inflation-data',
    imports: [FormsModule, PanelModule, TableModule, DecimalPipe, ButtonModule, HighchartsChartModule, TooltipModule, SelectModule, TabsModule, ProgressSpinnerModule],
    providers: [DialogService],
    templateUrl: './inflation-data.component.html',
    styleUrl: './inflation-data.component.scss'
})
export class InflationDataComponent implements OnInit {
    private inflationDataClient = inject(InflationDataClient);
    private dialogService = inject(DialogService);
    private dialogRef: DynamicDialogRef | undefined;

    entries: DisplayEntry[] = [];
    yearlyEntries: DisplayEntry[] = [];
    defaultRate: number = 0;
    loading: boolean = false;

    private rawEntries: InflationDataEntryWithProjectionResponse[] = [];
    private entryMap = new Map<string, InflationDataEntryWithProjectionResponse>();

    // Highcharts
    Highcharts: typeof Highcharts = Highcharts;
    chartOptions: Highcharts.Options = {};
    yearlyChartOptions: Highcharts.Options = {};

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
        this.dialogRef = this.dialogService.open(DefaultRateDialogComponent, {
            modal: true,
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
        this.dialogRef = this.dialogService.open(InflationCalculatorComponent, {
            modal: true,
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
                : null;

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
                : null;

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
        // Sort entries chronologically
        const sortedEntries = [...this.entries].sort((a, b) => {
            if (a.year !== b.year) return a.year - b.year;
            return (a.month || 0) - (b.month || 0);
        });

        // Split into actual and projected
        const actualEntries = sortedEntries.filter(e => !e.isProjected);
        const projectedEntries = sortedEntries.filter(e => e.isProjected);

        const allCategories = sortedEntries.map(e => `${this.getMonthName(e.month!)} ${e.year}`);

        // Create data arrays with nulls for missing values
        // To connect the lines, include the last actual value as the first projected value
        const actualIndexData = sortedEntries.map((e, i) => {
            if (e.isProjected) return null;
            // If this is the last actual entry and there are projected entries, don't return the value
            // as it will be included in the projected series
            return e.indexValue;
        });
        const projectedIndexData = sortedEntries.map((e, i) => {
            if (!e.isProjected) {
                // Include the last actual value as first point in projected series to connect the lines
                const isLastActual = i < sortedEntries.length - 1 && sortedEntries[i + 1].isProjected;
                return isLastActual ? e.indexValue : null;
            }
            return e.indexValue;
        });
        const changeData = sortedEntries.map(e => e.change);

        this.chartOptions = {
            chart: {
                type: 'line'
            },
            title: {
                text: 'Verbraucherpreisindex - Monatlich'
            },
            xAxis: {
                categories: allCategories,
                labels: {
                    rotation: -45,
                    step: Math.max(1, Math.floor(allCategories.length / 20))
                }
            },
            yAxis: [{
                title: {
                    text: 'Index-Wert'
                }
            }, {
                title: {
                    text: 'Veränderung zum Vormonat (%)'
                },
                opposite: true
            }],
            series: [{
                name: 'Index-Wert (Reale Daten)',
                type: 'line',
                data: actualIndexData,
                yAxis: 0,
                color: '#2196F3',
                tooltip: {
                    valueSuffix: ''
                }
            }, {
                name: 'Index-Wert (Geschätzt)',
                type: 'line',
                data: projectedIndexData,
                yAxis: 0,
                color: '#FF9800',
                dashStyle: 'Dash',
                tooltip: {
                    valueSuffix: ''
                }
            }, {
                name: 'Veränderung zum Vormonat',
                type: 'column',
                data: changeData,
                yAxis: 1,
                tooltip: {
                    valueSuffix: '%'
                }
            }],
            credits: {
                enabled: false
            }
        };
    }

    private updateYearlyChart() {
        // Sort entries chronologically
        const sortedEntries = [...this.yearlyEntries].sort((a, b) => a.year - b.year);

        const categories = sortedEntries.map(e => e.year.toString());
        // To connect the lines, include the last actual value as the first projected value
        const actualIndexData = sortedEntries.map(e => e.isProjected ? null : e.indexValue);
        const projectedIndexData = sortedEntries.map((e, i) => {
            if (!e.isProjected) {
                // Include the last actual value as first point in projected series to connect the lines
                const isLastActual = i < sortedEntries.length - 1 && sortedEntries[i + 1].isProjected;
                return isLastActual ? e.indexValue : null;
            }
            return e.indexValue;
        });
        const changeData = sortedEntries.map(e => e.change);

        this.yearlyChartOptions = {
            chart: {
                type: 'line'
            },
            title: {
                text: 'Verbraucherpreisindex - Jährlich'
            },
            xAxis: {
                categories: categories,
                title: {
                    text: 'Jahr'
                }
            },
            yAxis: [{
                title: {
                    text: 'Index-Wert (Jahresdurchschnitt)'
                }
            }, {
                title: {
                    text: 'Veränderung zum Vorjahr (%)'
                },
                opposite: true
            }],
            series: [{
                name: 'Index-Wert (Reale Daten)',
                type: 'line',
                data: actualIndexData,
                yAxis: 0,
                color: '#2196F3',
                tooltip: {
                    valueSuffix: ''
                }
            }, {
                name: 'Index-Wert (Geschätzt)',
                type: 'line',
                data: projectedIndexData,
                yAxis: 0,
                color: '#FF9800',
                dashStyle: 'Dash',
                tooltip: {
                    valueSuffix: ''
                }
            }, {
                name: 'Veränderung zum Vorjahr',
                type: 'column',
                data: changeData,
                yAxis: 1,
                tooltip: {
                    valueSuffix: '%'
                }
            }],
            credits: {
                enabled: false
            }
        };
    }
}
