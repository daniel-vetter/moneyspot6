import { AfterViewInit, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EditorState } from '@codemirror/state';
import { EditorView, keymap } from '@codemirror/view';
import { defaultKeymap, indentWithTab } from '@codemirror/commands';
import { javascript } from '@codemirror/lang-javascript';
import { basicSetup } from 'codemirror';
import { ViewChild, ElementRef } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { CreateRuleRequest, RulesClient, RuleValidationErrorResponse, UpdateRuleRequest } from '../../../server';
import { lastValueFrom } from 'rxjs';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-edit-rule',
    imports: [FormsModule, ButtonModule, MessageModule, ReactiveFormsModule, InputTextModule, CommonModule],
    templateUrl: './edit-rule.component.html',
    styleUrl: './edit-rule.component.scss'
})
export class EditRuleComponent implements AfterViewInit {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    @ViewChild('container') container!: ElementRef;
    private ruleClient = inject(RulesClient);
    private editorView!: EditorView;

    id: undefined | number;
    form = new FormGroup({
        name: new FormControl<string | undefined>(undefined, { nonNullable: true, validators: [Validators.required] })
    });

    constructor() {
        this.id = this.dialogConfig.data.id;
        this.dialogConfig.header = this.id === undefined ? "Neue Regel" : "Regel bearbeiten";
        this.dialogConfig.width = "800px";
    }

    async ngAfterViewInit(): Promise<void> {
        let startState = EditorState.create({
            doc: "function run() {\n  \n}",
            extensions: [
                basicSetup,
                keymap.of([...defaultKeymap, indentWithTab]),
                javascript()
            ]
        });

        this.editorView = new EditorView({
            state: startState,
            parent: this.container.nativeElement,
        });

        // Disable auto-resize by setting a fixed height
        this.editorView.dom.style.height = "500px";
        this.editorView.dom.style.overflow = "auto";

        this.editorView.dispatch({
            selection: { anchor: 19, head: 19 }
        });

        if (this.id !== undefined) {
            var rule = await lastValueFrom(this.ruleClient.getById(this.id));
            this.form.patchValue({ name: rule.name });
            this.editorView.dispatch({
                changes: { from: 0, to: this.editorView.state.doc.length, insert: rule.script }
            });
        }
    }

    async onSubmit() {
        try {
            if (this.id === undefined) {
                await lastValueFrom(this.ruleClient.create(new CreateRuleRequest({
                    name: this.form.controls.name.value!,
                    script: this.editorView.state.doc.toString()
                })));
            } else {
                await lastValueFrom(this.ruleClient.update(new UpdateRuleRequest({
                    id: this.id,
                    name: this.form.controls.name.value!,
                    script: this.editorView.state.doc.toString()
                })));
            }

            this.dialogRef.close(true);
        } catch (error) {
            if (error instanceof RuleValidationErrorResponse) {
                queueMicrotask(() => {
                    if (error.missingName)
                        this.form.controls.name.setErrors({ missingName: true });
                    if (error.nameAlreadyInUse)
                        this.form.controls.name.setErrors({ nameAlreadyInUse: true });
                });
            }
        }
    }

    onCancelClicked() {
        this.dialogRef.close(true);
    }
}
