import { Component, OnInit, inject } from '@angular/core';
import { AppDetails, SystemClient, SetAutoUpdateRequest } from '../../server';
import { ButtonModule } from 'primeng/button';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { FormsModule } from '@angular/forms';
import { lastValueFrom } from 'rxjs';
import { PanelModule } from 'primeng/panel';
import { TooltipModule } from 'primeng/tooltip';
import { UpdateState } from '../../common/update-state';
import { ModalDialogService } from '../../common/modal-dialog.service';
import { UpdateLogsDialogComponent } from './update-logs-dialog/update-logs-dialog.component';
import { UpdateInProgressDialogComponent } from './update-in-progress-dialog/update-in-progress-dialog.component';
import { DatePipe } from '@angular/common';

@Component({
    selector: 'app-system',
    imports: [ButtonModule, PanelModule, TooltipModule, ToggleSwitchModule, FormsModule, DatePipe],
    templateUrl: './system.component.html',
    styleUrl: './system.component.scss'
})
export class SystemComponent implements OnInit {
    private systemClient = inject(SystemClient);
    protected updateState = inject(UpdateState);
    private modalDialogService = inject(ModalDialogService);

    appDetails?: AppDetails;
    isChecking = false;

    async onCheckForUpdateClicked() {
        this.isChecking = true;
        try {
            await lastValueFrom(this.systemClient.checkForUpdate());
        } catch {
            // Check may fail if pull fails - status still reflects local comparison
        } finally {
            await this.updateState.refresh();
            this.isChecking = false;
        }
    }

    onShowUpdateLogsClicked() {
        this.modalDialogService.open(UpdateLogsDialogComponent, {
            header: 'Update-Logs',
            width: '700px',
            closable: true,
            closeOnEscape: true
        });
    }

    async onAutoUpdateChanged(enabled: boolean) {
        await lastValueFrom(this.systemClient.setAutoUpdate(new SetAutoUpdateRequest({ enabled })));
        await this.updateState.refresh();
    }

    async onApplyUpdateClicked() {
        this.updateState.updateInProgress = true;
        const dialogRef = this.modalDialogService.open(UpdateInProgressDialogComponent, {
            closable: false,
            closeOnEscape: false,
            dismissableMask: false,
            showHeader: false,
            width: '500px',
            contentStyle: { padding: '1.5rem' }
        });
        try {
            await lastValueFrom(this.systemClient.applyUpdate());
            this.pollUntilRestarted();
        } catch {
            dialogRef.close();
            this.updateState.updateInProgress = false;
        }
    }

    private pollUntilRestarted() {
        const poll = setInterval(async () => {
            try {
                await lastValueFrom(this.systemClient.getUpdateStatus());
                clearInterval(poll);
                window.location.reload();
            } catch {
                // Server still down, keep polling
            }
        }, 2000);
    }

    async ngOnInit(): Promise<void> {
        this.appDetails = await lastValueFrom(this.systemClient.getAppDetails());
        await this.updateState.refresh();
    }
}
