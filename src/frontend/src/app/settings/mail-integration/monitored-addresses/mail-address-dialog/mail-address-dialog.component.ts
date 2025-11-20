import { Component, inject, OnInit } from '@angular/core';
import { ButtonModule } from "primeng/button";
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { InputText } from "primeng/inputtext";
import { TextareaModule } from 'primeng/textarea';
import { MessageModule } from "primeng/message";
import { DynamicDialogConfig, DynamicDialogRef } from "primeng/dynamicdialog";
import { CreateMonitoredAddressRequest, CreateMonitoredAddressValidationErrorResponse, MailIntegrationClient, MonitoredAddressResponse, UpdateMonitoredAddressRequest, UpdateMonitoredAddressValidationErrorResponse } from "../../../../server";
import { lastValueFrom } from "rxjs";

@Component({
    selector: 'app-mail-address-dialog',
    imports: [ButtonModule, FormsModule, InputText, ReactiveFormsModule, TextareaModule, MessageModule],
    templateUrl: './mail-address-dialog.component.html',
    styleUrl: './mail-address-dialog.component.scss'
})
export class MailAddressDialogComponent implements OnInit {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    private mailIntegrationClient = inject(MailIntegrationClient);

    monitoredAddress: undefined | MonitoredAddressResponse;
    form = new FormGroup({
        address: new FormControl<string>("", { nonNullable: true, validators: [Validators.required] })
    });

    constructor() {
        const dialogConfig = inject(DynamicDialogConfig);
        this.monitoredAddress = dialogConfig.data?.monitoredAddress;
        dialogConfig.modal = true;
        dialogConfig.width = "700px";
        dialogConfig.header = "Adresse";
    }

    ngOnInit(): void {
        if (this.monitoredAddress) {
            this.form.patchValue({
                address: this.monitoredAddress?.address
            })
        }
    }

    protected onCancelClicked() {
        this.dialogRef.close();
    }

    protected async onSubmit() {

        try {
            if (this.monitoredAddress) {
                await lastValueFrom(this.mailIntegrationClient.updateMonitoredAddress(new UpdateMonitoredAddressRequest({
                    id: this.monitoredAddress.id,
                    address: this.form.value.address!
                })));
            } else {
                await lastValueFrom(this.mailIntegrationClient.createMonitoredAddress(new CreateMonitoredAddressRequest({
                    address: this.form.value.address!
                })));
            }
            this.dialogRef.close(true);
        } catch (error) {
            if (error instanceof CreateMonitoredAddressValidationErrorResponse || error instanceof UpdateMonitoredAddressValidationErrorResponse) {
                queueMicrotask(() => {
                    if (error.missingAddress)
                        this.form.controls.address.setErrors({ missingAddress: true });
                    if (error.alreadyConfigured)
                        this.form.controls.address.setErrors({ alreadyConfigured: true });
                });
            }
        }
    }
}
