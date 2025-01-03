import { Component, OnDestroy, OnInit } from '@angular/core';
import { DebugClient, RunningProcessResponse } from '../server';
import { ButtonModule } from 'primeng/button';
import { lastValueFrom } from 'rxjs';

@Component({
    selector: 'app-debug',
    imports: [ButtonModule],
    templateUrl: './debug.component.html',
    styleUrl: './debug.component.scss'
})
export class DebugComponent implements OnDestroy, OnInit {
    runningProcesses: RunningProcessResponse[] = [];
    intervall?: any;

    constructor(private debugClient: DebugClient) { }

    async OnReprocessTransactionpParsingClicked() {
        await lastValueFrom(this.debugClient.reprocessTransactionParsing());
    }

    async OnReimportStockDataLast30DaysClicked() {
        await lastValueFrom(this.debugClient.reimportLast30DayStocks());
    }

    ngOnInit(): void {
        this.intervall = setInterval(async () => {
            this.runningProcesses = await lastValueFrom(this.debugClient.getRunningAdapters());
        }, 1000);
    }

    ngOnDestroy(): void {
        if (this.intervall !== undefined) {
            clearInterval(this.intervall);
        }
    }
}
