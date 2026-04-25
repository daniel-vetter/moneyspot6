import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import {AppDetails, DebugClient, UpdateClient, SelfUpdateStatus, UpdateLogEntry} from '../../server';
import { ButtonModule } from 'primeng/button';
import { lastValueFrom } from 'rxjs';
import { PanelModule} from "primeng/panel";
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { DialogModule } from 'primeng/dialog';
import { UpdateState } from '../../common/update-state';

@Component({
    selector: 'app-system',
    imports: [ButtonModule, PanelModule, ProgressSpinnerModule, DialogModule, DatePipe],
    templateUrl: './system.component.html',
    styleUrl: './system.component.scss'
})
export class SystemComponent implements OnInit {
    private debugClient = inject(DebugClient);
    private updateClient = inject(UpdateClient);
    private updateState = inject(UpdateState);

    appDetails?: AppDetails;
    updateStatus?: SelfUpdateStatus;
    isChecking = false;
    isUpdating = false;
    showUpdateLogs = false;
    updateLogs: UpdateLogEntry[] = [];

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
        } catch {
            // Check may fail if pull fails - status still reflects local comparison
            this.updateStatus = await lastValueFrom(this.updateClient.getStatus());
        } finally {
            this.isChecking = false;
        }
    }

    async onShowUpdateLogsClicked() {
        this.updateLogs = await lastValueFrom(this.updateClient.getLogs());
        this.showUpdateLogs = true;
    }

    async onApplyUpdateClicked() {
        this.isUpdating = true;
        this.updateState.updateInProgress = true;
        try {
            await lastValueFrom(this.updateClient.applyUpdate());
            this.pollUntilRestarted();
        } catch {
            this.isUpdating = false;
            this.updateState.updateInProgress = false;
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
    }
}
