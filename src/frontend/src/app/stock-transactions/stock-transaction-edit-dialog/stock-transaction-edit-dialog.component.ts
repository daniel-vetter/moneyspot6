import { Component, OnInit } from '@angular/core';
import { DropdownModule } from "primeng/dropdown";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { InputTextModule } from "primeng/inputtext";
import { InputNumberModule } from "primeng/inputnumber"
import { CalendarModule } from "primeng/calendar";
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { StockListEntryResponse, StockTransactionsPageClient } from '../../server'
import { lastValueFrom } from 'rxjs';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';

@Component({
  selector: 'app-stock-transaction-edit-dialog',
  imports: [DropdownModule, ReactiveFormsModule, InputTextModule, CalendarModule, ReactiveFormsModule, InputNumberModule, ButtonModule, DatePickerModule, ConfirmDialogModule],
  templateUrl: './stock-transaction-edit-dialog.component.html',
  styleUrl: './stock-transaction-edit-dialog.component.scss'
})
export class StockTransactionEditDialogComponent implements OnInit {


  constructor(private dynamicDialogRef: DynamicDialogRef, dialogConfig: DynamicDialogConfig, private client: StockTransactionsPageClient, private confirmationService: ConfirmationService) {
    dialogConfig.header = "Neue Transaktion";
    dialogConfig.width = "500px";
    dialogConfig.height = "620px";
    this.id = dialogConfig.data.id;
  }

  id: number | undefined;
  stocks: StockListEntryResponse[] = [];
  types: { id: number, name: string }[] = [{ id: 0, name: "Kauf" }, { id: 1, name: "Verkauf" }]

  form = new FormGroup({
    stock: new FormControl(0, { nonNullable: true, validators: Validators.required }),
    date: new FormControl<Date>(new Date(), { nonNullable: true, validators: Validators.required }),
    amount: new FormControl<number | undefined>(undefined, { nonNullable: true, validators: [Validators.min(0.001), Validators.required] }),
    price: new FormControl<number | undefined>(undefined, { nonNullable: true, validators: [Validators.min(0.001), Validators.required] }),
    type: new FormControl(0, { nonNullable: true, validators: Validators.required })
  })

  async ngOnInit(): Promise<void> {
    this.stocks = await lastValueFrom(this.client.getStocks());
    this.form.patchValue({
      stock: this.stocks[0].id
    });

    if (this.id !== undefined) {
      const response = await lastValueFrom(this.client.getStockTransaction(this.id));
      const amount = Math.abs(response.amount);
      const type = response.amount < 0 ? 1 : 0;
      this.form.patchValue({
        type: type,
        stock: response.stockId,
        amount: amount,
        price: response.price,
        date: response.date
      })
    }
  }

  async onSubmit() {

    let amount = this.form.value.amount || 0;
    if (this.form.value.type == 1) {
      amount = amount * -1;
    }
    if (this.id === undefined) {
      await lastValueFrom(this.client.createNewTransaction(this.form.value.stock, amount, this.form.value.price, this.toDateOnlyStr(this.form.value.date)));
    } else {
      await lastValueFrom(this.client.updateTransaction(this.id, this.form.value.stock, amount, this.form.value.price, this.toDateOnlyStr(this.form.value.date)));
    }

    this.dynamicDialogRef.close();
  }

  toDateOnlyStr(date: Date | undefined) {
    if (date === undefined) {
      throw Error("no date provided");
    }
    return date.getFullYear() + "-" + (date.getMonth() + 1) + "-" + date.getDate()
  }

  onCancelClicked() {
    this.dynamicDialogRef.close();
  }

  onDeleteClicked() {
    this.confirmationService.confirm({
      header: "Löschen bestätigen",
      message: "Sind sie sicher das Sie diesen Kauf/Verkauf löschen möchten?",
      acceptButtonProps: {
        label: "Ja"
      },
      accept: async () => {
        await lastValueFrom(this.client.deleteStockTransactions(this.id!))
        this.dynamicDialogRef.close();
      },
      rejectButtonProps: {
        label: "Nein",
        outlined: true,
      },
    });
  }
}
