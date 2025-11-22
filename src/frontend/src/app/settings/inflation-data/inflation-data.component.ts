import { Component, OnInit, inject } from '@angular/core';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { ButtonModule } from 'primeng/button';
import { InflationDataClient, InflationDataEntryResponse } from '../../server';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { DecimalPipe } from '@angular/common';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { DefaultRateDialogComponent } from './default-rate-dialog/default-rate-dialog.component';

interface DisplayEntry {
    year: number;
    month?: number;
    indexValue: number;
    change: number | null;
}

@Component({
    selector: 'app-inflation-data',
    imports: [PanelModule, TableModule, DecimalPipe, Tabs, TabList, Tab, TabPanels, TabPanel, ButtonModule],
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

    private rawEntries: InflationDataEntryResponse[] = [];
    private entryMap = new Map<string, InflationDataEntryResponse>();

    async ngOnInit(): Promise<void> {
        await this.update();
    }

    async update() {
        const response = await lastValueFrom(this.inflationDataClient.getAll());
        this.rawEntries = response.entries;
        this.defaultRate = response.defaultRate;

        // Build a map for quick lookup
        this.entryMap.clear();
        for (const entry of this.rawEntries) {
            this.entryMap.set(`${entry.year}-${entry.month}`, entry);
        }

        this.calculateMonthlyEntries();
        this.calculateYearlyEntries();
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
                change
            };
        });
    }

    private calculateYearlyEntries() {
        // Group entries by year
        const yearMap = new Map<number, InflationDataEntryResponse[]>();

        for (const entry of this.rawEntries) {
            if (!yearMap.has(entry.year)) {
                yearMap.set(entry.year, []);
            }
            yearMap.get(entry.year)!.push(entry);
        }

        // Calculate yearly averages
        const years = Array.from(yearMap.keys()).sort((a, b) => b - a);
        const yearlyAverages = new Map<number, number>();

        for (const year of years) {
            const yearEntries = yearMap.get(year)!;
            const average = yearEntries.reduce((sum, e) => sum + e.indexValue, 0) / yearEntries.length;
            yearlyAverages.set(year, average);
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
                change
            };
        });
    }

    getMonthName(month: number): string {
        const months = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni',
                       'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];
        return months[month - 1] || '';
    }
}
