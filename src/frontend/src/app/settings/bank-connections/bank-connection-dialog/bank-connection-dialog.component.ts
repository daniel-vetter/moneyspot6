import { Component, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { BankConnectionClient, CreateBankConnectionRequest, UpdateBankConnectionRequest, BankConnectionValidationErrorResponse } from '../../../server';
import { lastValueFrom } from 'rxjs';
import { MessageModule } from 'primeng/message';
import { CommonModule } from '@angular/common';
import { PasswordModule } from 'primeng/password';

@Component({
    selector: 'app-bank-connection-dialog',
    imports: [ButtonModule, ReactiveFormsModule, InputTextModule, MessageModule, CommonModule, PasswordModule, ProgressSpinnerModule],
    templateUrl: './bank-connection-dialog.component.html',
    styleUrl: './bank-connection-dialog.component.scss',
    standalone: true
})
export class BankConnectionDialogComponent implements OnInit {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    private bankConnectionClient = inject(BankConnectionClient);

    id: number | null;
    loading = false;
    form = new FormGroup({
        name: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
        hbciVersion: new FormControl<string>('300', { nonNullable: true, validators: [Validators.required] }),
        bankCode: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
        customerId: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
        userId: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
        pin: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] })
    });

    constructor() {
        this.id = this.dialogConfig.data.id;
        this.dialogConfig.header = this.id === null ? 'Neue Bankverbindung' : 'Bankverbindung bearbeiten';
        this.dialogConfig.width = '600px';
        this.dialogConfig.modal = true;
    }

    async ngOnInit() {
        if (this.id !== null) {
            this.loading = true;
            try {
                const connection = await lastValueFrom(this.bankConnectionClient.get(this.id));
                this.form.setValue({
                    name: connection.name,
                    hbciVersion: connection.hbciVersion,
                    bankCode: connection.bankCode,
                    customerId: connection.customerId,
                    userId: connection.userId,
                    pin: connection.pin
                });
            } finally {
                this.loading = false;
            }
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
            if (this.id === null) {
                await lastValueFrom(this.bankConnectionClient.create(new CreateBankConnectionRequest({
                    name: this.form.value.name!,
                    hbciVersion: this.form.value.hbciVersion!,
                    bankCode: this.form.value.bankCode!,
                    customerId: this.form.value.customerId!,
                    userId: this.form.value.userId!,
                    pin: this.form.value.pin!
                })));
            } else {
                console.log(this.form.value.bankCode!)
                await lastValueFrom(this.bankConnectionClient.update(new UpdateBankConnectionRequest({
                    id: this.id,
                    name: this.form.value.name!,
                    hbciVersion: this.form.value.hbciVersion!,
                    bankCode: this.form.value.bankCode!,
                    customerId: this.form.value.customerId!,
                    userId: this.form.value.userId!,
                    pin: this.form.value.pin!
                })));
            }

            this.dialogRef.close(true);
        } catch (error) {
            if (error instanceof BankConnectionValidationErrorResponse) {
                queueMicrotask(() => {
                    if (error.missingName)
                        this.form.controls.name.setErrors({ required: true });
                    if (error.missingHbciVersion)
                        this.form.controls.hbciVersion.setErrors({ required: true });
                    if (error.missingBankCode)
                        this.form.controls.bankCode.setErrors({ required: true });
                    if (error.missingCustomerId)
                        this.form.controls.customerId.setErrors({ required: true });
                    if (error.missingUserId)
                        this.form.controls.userId.setErrors({ required: true });
                    if (error.missingPin)
                        this.form.controls.pin.setErrors({ required: true });
                    if ((error as any).nameAlreadyExists)
                        this.form.controls.name.setErrors({ nameAlreadyExists: true });
                });
            }
        } finally {
            this.form.enable();
        }
    }
}
