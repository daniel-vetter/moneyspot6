import { Component } from '@angular/core';
import {DropdownModule} from "primeng/dropdown";
import {FormsModule} from "@angular/forms";
import {InputTextModule} from "primeng/inputtext";
import {CalendarModule} from "primeng/calendar";

@Component({
  selector: 'app-stock-transaction-edit-dialog',
  standalone: true,
  imports: [DropdownModule, FormsModule, InputTextModule, CalendarModule],
  templateUrl: './stock-transaction-edit-dialog.component.html',
  styleUrl: './stock-transaction-edit-dialog.component.scss'
})
export class StockTransactionEditDialogComponent {
    types: {id: number, name: string}[] = [{id: 0, name: "Kauf"}, {id: 1, name: "Verkauf"}]
    selectedType = 0;
    amount = "";
    price = "";
    timestamp = "";
}
