import { Component, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { CategoryConfigurationClient, CreateCategoryRequest, CreateCategoryValidationErrorResponse, UpdateCategoryRequest, UpdateCategoryValidationErrorResponse } from '../../../server';
import { lastValueFrom } from 'rxjs';
import { MessageModule } from 'primeng/message';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
    selector: 'app-new-category-dialog',
    imports: [ButtonModule, ReactiveFormsModule, InputTextModule, MessageModule, ProgressSpinnerModule],
    templateUrl: './new-category-dialog.component.html',
    styleUrl: './new-category-dialog.component.scss'
})
export class NewCategoryDialogComponent implements OnInit {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    private categoryConfigurationClient = inject(CategoryConfigurationClient);

    id: undefined | number;
    form = new FormGroup({
        name: new FormControl<string | undefined>(undefined, { nonNullable: true, validators: [Validators.required] })
    });
    parentId: number | undefined;
    loading = false;

    constructor() {
        this.id = this.dialogConfig.data.id;
        this.parentId = this.dialogConfig.data.parentId;
        this.dialogConfig.header = this.id === undefined ? "Neue Kategorie" : "Kategorie bearbeiten";
        this.dialogConfig.width = "500px";
        this.dialogConfig.height = "620px";
    }

    async ngOnInit() {
        if (this.id !== undefined) {
            this.loading = true;
            const cat = await lastValueFrom(this.categoryConfigurationClient.getCategory(this.id));
            this.form.setValue({
                name: cat.name
            });
            this.loading = false;
        }
    }

    onCancelClicked() {
        this.dialogRef.close();
    }
    async onSubmit() {
        this.form.disable()

        try {
            if (this.id === undefined) {
                await lastValueFrom(this.categoryConfigurationClient.create(new CreateCategoryRequest({
                    name: this.form.value.name,
                    parentId: this.parentId
                })));
            }
            else {
                await lastValueFrom(this.categoryConfigurationClient.update(new UpdateCategoryRequest({
                    id: this.id,
                    name: this.form.value.name
                })));
            }
        } catch (error) {
            if (error instanceof CreateCategoryValidationErrorResponse || error instanceof UpdateCategoryValidationErrorResponse) {
                queueMicrotask(() => {
                    if (error.missingName)
                        this.form.controls.name.setErrors({ missingName: true });
                    if (error.nameAlreadyInUse)
                        this.form.controls.name.setErrors({ nameAlreadyInUse: true });
                    if (error instanceof CreateCategoryValidationErrorResponse) {
                        if (error.invalidParent)
                            this.form.controls.name.setErrors({ invalidParent: true });
                    }
                });
            }
            return;
        }
        finally {
            this.form.enable();
        }
        this.dialogRef.close();
    }
}
