import { Component } from '@angular/core';
import { ConnectedAccountsComponent } from "./connected-accounts/connected-accounts.component";
import { MonitoredAddressesComponent } from "./monitored-addresses/monitored-addresses.component";

@Component({
    selector: 'app-mail-integration',
    imports: [ConnectedAccountsComponent, MonitoredAddressesComponent],
    templateUrl: './mail-integration.component.html',
    styleUrl: './mail-integration.component.scss'
})
export class MailIntegrationComponent {
}
