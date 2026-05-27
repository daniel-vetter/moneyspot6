import { Component, inject } from '@angular/core';
import {Panel, PanelModule} from "primeng/panel";
import {PrimeTemplate} from "primeng/api";
import {RouterLink} from "@angular/router";
import { BadgeModule } from "primeng/badge";
import { UpdateState } from '../common/update-state';
import { EmailSyncState } from '../common/email-sync-state';

@Component({
    selector: 'app-settings',
    imports: [Panel, PrimeTemplate, PanelModule, RouterLink, BadgeModule],
    templateUrl: './settings.component.html',
    styleUrl: './settings.component.scss'
})
export class SettingsComponent {
    updateState = inject(UpdateState);
    emailSyncState = inject(EmailSyncState);
}
