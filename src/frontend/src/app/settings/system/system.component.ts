import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import {AppDetails, DebugClient, RunningProcessResponse} from '../../server';
import { ButtonModule } from 'primeng/button';
import { lastValueFrom } from 'rxjs';
import { PanelModule} from "primeng/panel";

@Component({
    selector: 'app-system',
    imports: [ButtonModule, PanelModule],
    templateUrl: './system.component.html',
    styleUrl: './system.component.scss'
})
export class SystemComponent implements OnDestroy, OnInit {
    private debugClient = inject(DebugClient);

    runningProcesses: RunningProcessResponse[] = [];
    interval?: any;
    appDetails?: AppDetails;

    async OnReprocessTransactionpParsingClicked() {
        await lastValueFrom(this.debugClient.reprocessTransactionParsing());
    }

    async OnReimportStockDataLast30DaysClicked() {
        await lastValueFrom(this.debugClient.reimportLast30DayStocks());
    }

    async OnReseedDatabaseClicked() {
        await lastValueFrom(this.debugClient.reseedDatabase());
    }


    async ngOnInit(): Promise<void> {

        this.appDetails = await lastValueFrom(this.debugClient.getAppDetails())
        this.interval = setInterval(async () => {
            this.runningProcesses = await lastValueFrom(this.debugClient.getRunningAdapters());
        }, 1000);
    }

    ngOnDestroy(): void {
        if (this.interval !== undefined) {
            clearInterval(this.interval);
        }
    }
}
