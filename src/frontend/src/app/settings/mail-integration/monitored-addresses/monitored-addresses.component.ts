import { Component, inject, OnInit, signal } from '@angular/core';
import { PanelModule } from "primeng/panel";
import { ButtonModule } from "primeng/button";
import { TableModule } from "primeng/table";
import { ProgressSpinnerModule } from "primeng/progressspinner";
import { MailIntegrationClient, MonitoredAddressResponse } from "../../../server";
import { DialogService } from "primeng/dynamicdialog";
import { ConfirmationService } from "primeng/api";
import { firstValueFrom, lastValueFrom } from "rxjs";
import { MailAddressDialogComponent } from "./mail-address-dialog/mail-address-dialog.component";

@Component({
    selector: 'app-monitored-addresses',
    imports: [PanelModule, ButtonModule, TableModule, ProgressSpinnerModule],
    providers: [DialogService],
    templateUrl: './monitored-addresses.component.html',
    styleUrl: './monitored-addresses.component.scss'
})
export class MonitoredAddressesComponent implements OnInit {
    mailIntegrationClient = inject(MailIntegrationClient);
    dialogService = inject(DialogService);
    confirmationService = inject(ConfirmationService);

    monitoredAddresses = signal<MonitoredAddressResponse[] | undefined>(undefined);

    async ngOnInit(): Promise<void> {
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
}
