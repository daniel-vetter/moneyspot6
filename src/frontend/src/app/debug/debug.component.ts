import { Component, OnDestroy, OnInit, inject } from '@angular/core';
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
    private debugClient = inject(DebugClient);

    runningProcesses: RunningProcessResponse[] = [];
    intervall?: any;

    async OnReprocessTransactionpParsingClicked() {
        await lastValueFrom(this.debugClient.reprocessTransactionParsing());
    }

    async OnReimportStockDataLast30DaysClicked() {
        await lastValueFrom(this.debugClient.reimportLast30DayStocks());
    }

    async OnReseedDatabaseClicked() {
        await lastValueFrom(this.debugClient.reseedDatabase());
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
