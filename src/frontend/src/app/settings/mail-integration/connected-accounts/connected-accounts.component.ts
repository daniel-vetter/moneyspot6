import { Component, inject, OnInit } from '@angular/core';
import { PanelModule } from "primeng/panel";
import { ButtonModule } from "primeng/button";
import { ProgressSpinnerModule } from "primeng/progressspinner";
import { IntegrationStatusResponse, MailIntegrationClient } from "../../../server";
import { lastValueFrom } from "rxjs";
import { ConfirmationService } from "primeng/api";

@Component({
    selector: 'app-connected-accounts',
    imports: [PanelModule, ButtonModule, ProgressSpinnerModule],
    templateUrl: './connected-accounts.component.html',
    styleUrl: './connected-accounts.component.scss'
})
export class ConnectedAccountsComponent implements OnInit {
    mailIntegrationClient = inject(MailIntegrationClient);
    confirmationService = inject(ConfirmationService);

    status: IntegrationStatusResponse | undefined;

    async ngOnInit(): Promise<void> {
        this.status = await lastValueFrom(this.mailIntegrationClient.getStatus());
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
