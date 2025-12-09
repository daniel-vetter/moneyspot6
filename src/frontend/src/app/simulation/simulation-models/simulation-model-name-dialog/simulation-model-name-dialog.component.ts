import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { CommonModule } from '@angular/common';
import { NewSimulationModelRequest, RenameSimulationModelRequest, SimulationModelsClient, SimulationModelValidationErrorResponse } from '../../../server';
import { lastValueFrom } from 'rxjs';

@Component({
    selector: 'app-simulation-model-name-dialog',
    imports: [ButtonModule, CheckboxModule, FormsModule, InputTextModule, MessageModule, CommonModule],
    templateUrl: './simulation-model-name-dialog.component.html',
    styleUrl: './simulation-model-name-dialog.component.scss'
})
export class SimulationModelNameDialogComponent {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    private simulationModelsClient = inject(SimulationModelsClient);

    name: string = '';
    id: number | undefined;
    isEditMode: boolean;
    isSaving = false;
    errorMessage: string | undefined;
    includeSampleCode = true;

    constructor() {
        this.id = this.dialogConfig.data?.id;
        this.isEditMode = this.id !== undefined;
        this.name = this.dialogConfig.data?.name || '';
        this.dialogConfig.header = this.isEditMode ? 'Name bearbeiten' : 'Neue Simulation';
        this.dialogConfig.width = '400px';
    }

    onCancel() {
        this.dialogRef.close();
    }

    async onConfirm() {
        if (!this.name.trim()) return;

        this.isSaving = true;
        this.errorMessage = undefined;

        try {
            if (this.isEditMode) {
                await lastValueFrom(this.simulationModelsClient.rename(new RenameSimulationModelRequest({
                    id: this.id!,
                    name: this.name.trim()
                })));
                this.dialogRef.close(this.name.trim());
            } else {
                const newId = await lastValueFrom(this.simulationModelsClient.create(new NewSimulationModelRequest({
                    name: this.name.trim(),
                    includeSampleCode: this.includeSampleCode
                })));
                this.dialogRef.close(newId);
            }
        } catch (error) {
            if (error instanceof SimulationModelValidationErrorResponse) {
                if (error.missingName) {
                    this.errorMessage = 'Ein Name muss angegeben werden.';
                } else if (error.nameAlreadyInUse) {
                    this.errorMessage = 'Es existiert bereits ein Modell mit diesem Namen.';
                }
            } else {
                this.errorMessage = 'Ein Fehler ist aufgetreten.';
                console.error(error);
            }
        } finally {
            this.isSaving = false;
        }
    }
}
