import { Component, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { StockClient, CreateStockRequest, UpdateStockRequest, StockValidationErrorResponse } from '../../../server';
import { lastValueFrom } from 'rxjs';
import { MessageModule } from 'primeng/message';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-stock-dialog',
    imports: [ButtonModule, ReactiveFormsModule, InputTextModule, MessageModule, CommonModule],
    templateUrl: './stock-dialog.component.html',
    styleUrl: './stock-dialog.component.scss',
    standalone: true
})
export class StockDialogComponent implements OnInit {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    private stockClient = inject(StockClient);

    id: number | null;
    form = new FormGroup({
        name: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
        symbol: new FormControl<string>('', { nonNullable: true })
    });

    constructor() {
        this.id = this.dialogConfig.data.id;
        this.dialogConfig.header = this.id === null ? 'Neue Aktie' : 'Aktie bearbeiten';
        this.dialogConfig.width = '600px';
        this.dialogConfig.modal = true;
    }

    async ngOnInit() {
        if (this.id !== null) {
            const stock = await lastValueFrom(this.stockClient.get(this.id));
            this.form.setValue({
                name: stock.name,
                symbol: stock.symbol ?? ''
            });
        }
    }

    onCancelClicked() {
        this.dialogRef.close();
    }

    async onSubmit() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.form.disable();

        try {
            const symbol = this.form.value.symbol && this.form.value.symbol.trim() !== ''
                ? this.form.value.symbol
                : undefined;

            if (this.id === null) {
                await lastValueFrom(this.stockClient.create(new CreateStockRequest({
                    name: this.form.value.name!,
                    symbol: symbol
                })));
            } else {
                await lastValueFrom(this.stockClient.update(new UpdateStockRequest({
                    id: this.id,
                    name: this.form.value.name!,
                    symbol: symbol
                })));
            }

            this.dialogRef.close(true);
        } catch (error) {
            if (error instanceof StockValidationErrorResponse) {
                queueMicrotask(() => {
                    if (error.missingName)
                        this.form.controls.name.setErrors({ required: true });
                    if ((error as any).nameAlreadyExists)
                        this.form.controls.name.setErrors({ nameAlreadyExists: true });
                });
            }
        } finally {
            this.form.enable();
        }
    }
}
