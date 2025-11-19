import { Component } from '@angular/core';
import { ConnectedAccountsComponent } from "./connected-accounts/connected-accounts.component";
import { MonitoredAddressesComponent } from "./monitored-addresses/monitored-addresses.component";
import { ImportedEmailsComponent } from "./imported-emails/imported-emails.component";

@Component({
    selector: 'app-mail-integration',
    imports: [ConnectedAccountsComponent, MonitoredAddressesComponent, ImportedEmailsComponent],
    templateUrl: './mail-integration.component.html',
    styleUrl: './mail-integration.component.scss'
})
export class MailIntegrationComponent {
}
