import { Component } from '@angular/core';
import { CategoriesComponent } from './categories/categories.component';

@Component({
  selector: 'app-settings',
  imports: [CategoriesComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent {

}
