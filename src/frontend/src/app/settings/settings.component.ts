import { Component } from '@angular/core';
import {Panel, PanelModule} from "primeng/panel";
import {PrimeTemplate} from "primeng/api";
import {RouterLink} from "@angular/router";

@Component({
    selector: 'app-settings',
    imports: [Panel, PrimeTemplate, PanelModule, RouterLink],
    templateUrl: './settings.component.html',
    styleUrl: './settings.component.scss'
})
export class SettingsComponent {

}
