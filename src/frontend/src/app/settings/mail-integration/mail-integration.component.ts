import {Component, inject, OnInit, signal} from '@angular/core';
import {ButtonModule} from "primeng/button";
import {PanelModule} from "primeng/panel";
import {IntegrationStatusResponse, MailIntegrationClient, MonitoredAddressResponse} from "../../server";
import {firstValueFrom, lastValueFrom} from "rxjs";
import {DialogService} from "primeng/dynamicdialog";
import {MailAddressDialogComponent} from "./mail-address-dialog/mail-address-dialog.component";
import {TableModule} from "primeng/table";
import {ConfirmationService} from "primeng/api";

@Component({
    selector: 'app-mail-integration',
    imports: [PanelModule, ButtonModule, TableModule],
    providers: [DialogService],
    templateUrl: './mail-integration.component.html',
    styleUrl: './mail-integration.component.scss'
})
export class MailIntegrationComponent implements OnInit {
    mailIntegrationClient = inject(MailIntegrationClient)
    dialogService = inject(DialogService);
    confirmationService = inject(ConfirmationService);

    status: IntegrationStatusResponse | undefined;
    monitoredAddresses = signal<MonitoredAddressResponse[]>([]);

    async ngOnInit(): Promise<void> {
        this.status = await lastValueFrom(this.mailIntegrationClient.getStatus());
        this.monitoredAddresses.set(await lastValueFrom((this.mailIntegrationClient.getAllMonitoredAddresses())));
    }

    protected async onAddAddressClicked() {
        const dlg = this.dialogService.open(MailAddressDialogComponent, {
            modal: true
        });
        const success = await firstValueFrom(dlg.onClose);
        if (success) {
            this.monitoredAddresses.set(await lastValueFrom((this.mailIntegrationClient.getAllMonitoredAddresses())));
        }
    }

    protected async onEditMonitoredAddress(monitoredAddress: MonitoredAddressResponse) {
        const dlg = this.dialogService.open(MailAddressDialogComponent, {
            modal: true,
            data: {
                monitoredAddress: monitoredAddress
            }
        });
        const success = await firstValueFrom(dlg.onClose);
        if (success) {
            this.monitoredAddresses.set(await lastValueFrom((this.mailIntegrationClient.getAllMonitoredAddresses())));
        }
    }

    protected async onDeleteMonitoredAddress(monitoredAddress: MonitoredAddressResponse) {
        this.confirmationService.confirm({
            header: 'Adresse löschen',
            message: 'Möchten Sie die Adresse "' + monitoredAddress.address + '" wirklich löschen?',
            acceptLabel: 'Ja',
            rejectLabel: 'Nein',
            accept: async () => {
                await lastValueFrom(this.mailIntegrationClient.deleteMonitoredAddress(monitoredAddress.id));
                this.monitoredAddresses.set(await lastValueFrom((this.mailIntegrationClient.getAllMonitoredAddresses())));
            }
        });
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
