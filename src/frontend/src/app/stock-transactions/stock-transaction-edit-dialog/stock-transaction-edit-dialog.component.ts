import { Component } from '@angular/core';
import { DropdownModule } from "primeng/dropdown";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { InputTextModule } from "primeng/inputtext";
import { InputNumberModule } from "primeng/inputnumber"
import { CalendarModule } from "primeng/calendar";
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-stock-transaction-edit-dialog',
  imports: [DropdownModule, ReactiveFormsModule, InputTextModule, CalendarModule, ReactiveFormsModule, InputNumberModule, ButtonModule],
  templateUrl: './stock-transaction-edit-dialog.component.html',
  styleUrl: './stock-transaction-edit-dialog.component.scss'
})
export class StockTransactionEditDialogComponent {

  constructor(private dynamicDialogRef: DynamicDialogRef) {
  }

  types: { id: number, name: string }[] = [{ id: 0, name: "Kauf" }, { id: 1, name: "Verkauf" }]

  form = new FormGroup({
    timestamp: new FormControl(""),
    amount: new FormControl(0),
    price: new FormControl(0),
    selectedType: new FormControl(0, Validators.required)
  })

  onCancelClicked() {
    this.dynamicDialogRef.close();
  }
}
