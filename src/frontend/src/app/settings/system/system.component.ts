import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import {AppDetails, DebugClient, RunningProcessResponse, UpdateClient, SelfUpdateStatus} from '../../server';
import { ButtonModule } from 'primeng/button';
import { lastValueFrom } from 'rxjs';
import { PanelModule} from "primeng/panel";
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
    selector: 'app-system',
    imports: [ButtonModule, PanelModule, ProgressSpinnerModule, DatePipe],
    templateUrl: './system.component.html',
    styleUrl: './system.component.scss'
})
export class SystemComponent implements OnDestroy, OnInit {
    private debugClient = inject(DebugClient);
    private updateClient = inject(UpdateClient);

    runningProcesses: RunningProcessResponse[] = [];
    interval?: any;
    appDetails?: AppDetails;
    updateStatus?: SelfUpdateStatus;
    isChecking = false;
    isUpdating = false;

    async OnReprocessTransactionpParsingClicked() {
        await lastValueFrom(this.debugClient.reprocessTransactionParsing());
    }

    async OnReimportStockDataLast30DaysClicked() {
        await lastValueFrom(this.debugClient.reimportLast30DayStocks());
    }

    async OnReseedDatabaseClicked() {
        await lastValueFrom(this.debugClient.reseedDatabase());
    }

    async onCheckForUpdateClicked() {
        this.isChecking = true;
        try {
            await lastValueFrom(this.updateClient.checkNow());
            this.updateStatus = await lastValueFrom(this.updateClient.getStatus());
        } finally {
            this.isChecking = false;
        }
    }

    async onApplyUpdateClicked() {
        this.isUpdating = true;
        try {
            await lastValueFrom(this.updateClient.applyUpdate());
            this.pollUntilRestarted();
        } catch {
            this.isUpdating = false;
        }
    }

    private pollUntilRestarted() {
        const poll = setInterval(async () => {
            try {
                await lastValueFrom(this.updateClient.getStatus());
                clearInterval(poll);
                window.location.reload();
            } catch {
                // Server still down, keep polling
            }
        }, 2000);
    }

    async ngOnInit(): Promise<void> {
        this.appDetails = await lastValueFrom(this.debugClient.getAppDetails())
        this.updateStatus = await lastValueFrom(this.updateClient.getStatus());
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
