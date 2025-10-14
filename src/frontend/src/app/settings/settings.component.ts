import { Component } from '@angular/core';
import { CategoriesComponent } from './categories/categories.component';
import { RulesComponent } from "./rules/rules.component";

@Component({
    selector: 'app-settings',
    imports: [CategoriesComponent, RulesComponent, RulesComponent],
    templateUrl: './settings.component.html',
    styleUrl: './settings.component.scss'
})
export class SettingsComponent {

}
