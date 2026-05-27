import { Component, inject, OnInit } from '@angular/core';
import { PanelModule } from "primeng/panel";
import { ButtonModule } from "primeng/button";
import { ProgressSpinnerModule } from "primeng/progressspinner";
import { TooltipModule } from "primeng/tooltip";
import { IntegrationStatusResponse, MailIntegrationClient } from "../../../server";
import { lastValueFrom } from "rxjs";
import { ConfirmationService } from "primeng/api";
import { SyncJobsDialogComponent } from "../sync-jobs-dialog/sync-jobs-dialog.component";
import { ModalDialogService } from "../../../common/modal-dialog.service";
import { EmailSyncState } from "../../../common/email-sync-state";

@Component({
    selector: 'app-connected-accounts',
    imports: [PanelModule, ButtonModule, ProgressSpinnerModule, TooltipModule],
    templateUrl: './connected-accounts.component.html',
    styleUrl: './connected-accounts.component.scss'
})
export class ConnectedAccountsComponent implements OnInit {
    mailIntegrationClient = inject(MailIntegrationClient);
    confirmationService = inject(ConfirmationService);
    modalDialogService = inject(ModalDialogService);
    emailSyncState = inject(EmailSyncState);

    status: IntegrationStatusResponse | undefined;

    async ngOnInit(): Promise<void> {
        this.status = await lastValueFrom(this.mailIntegrationClient.getStatus());
        await this.emailSyncState.refresh();
    }

    protected onShowSyncJobsClicked() {
        this.modalDialogService.open(SyncJobsDialogComponent, { focusOnShow: false });
    }

    protected onDeleteConnectedAccount(connectedAccount: string) {
        this.confirmationService.confirm({
            header: 'Account löschen',
            message: 'Möchten Sie den Account "' + connectedAccount + '" wirklich löschen?',
            acceptLabel: 'Ja',
            rejectLabel: 'Nein',
            accept: async () => {
                await lastValueFrom(this.mailIntegrationClient.disconnectGMailAccount(connectedAccount));
                this.status = await lastValueFrom(this.mailIntegrationClient.getStatus());
            }
        });
    }
}
