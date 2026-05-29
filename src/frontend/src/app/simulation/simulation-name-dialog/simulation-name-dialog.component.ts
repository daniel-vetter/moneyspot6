import { AfterViewInit, Component, ElementRef, inject, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { NewSimulationRequest, RenameSimulationRequest, SimulationsClient, SimulationValidationErrorResponse } from '../../server';
import { lastValueFrom } from 'rxjs';

@Component({
    selector: 'app-simulation-name-dialog',
    imports: [ButtonModule, CheckboxModule, FormsModule, InputTextModule, MessageModule],
    templateUrl: './simulation-name-dialog.component.html',
    styleUrl: './simulation-name-dialog.component.scss'
})
export class SimulationNameDialogComponent implements AfterViewInit {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    private simulationsClient = inject(SimulationsClient);

    @ViewChild('nameInput') nameInput!: ElementRef<HTMLInputElement>;

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

    ngAfterViewInit() {
        setTimeout(() => this.nameInput.nativeElement.focus(), 0);
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
                await lastValueFrom(this.simulationsClient.rename(new RenameSimulationRequest({
                    id: this.id!,
                    name: this.name.trim()
                })));
                this.dialogRef.close(this.name.trim());
            } else {
                const newId = await lastValueFrom(this.simulationsClient.create(new NewSimulationRequest({
                    name: this.name.trim(),
                    includeSampleCode: this.includeSampleCode
                })));
                this.dialogRef.close(newId);
            }
        } catch (error) {
            if (error instanceof SimulationValidationErrorResponse) {
                if (error.missingName) {
                    this.errorMessage = 'Ein Name muss angegeben werden.';
                } else if (error.nameAlreadyInUse) {
                    this.errorMessage = 'Es existiert bereits eine Simulation mit diesem Namen.';
                }
            } else {
                this.errorMessage = 'Ein Fehler ist aufgetreten.';
                throw error;
            }
        } finally {
            this.isSaving = false;
        }
    }
}
