import { AfterViewInit, Component, inject, makeEnvironmentProviders, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ViewChild, ElementRef } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { NewRuleRequest, RulesClient, RuleValidationErrorResponse, UpdateRuleRequest } from '../../../server';
import { lastValueFrom } from 'rxjs';
import { CommonModule } from '@angular/common';

import './monaco-setup';
import * as monaco from 'monaco-editor';



@Component({
    selector: 'app-edit-rule',
    imports: [FormsModule, ButtonModule, MessageModule, ReactiveFormsModule, InputTextModule, CommonModule, MessageModule],
    templateUrl: './edit-rule.component.html',
    styleUrl: './edit-rule.component.scss'
})
export class EditRuleComponent implements AfterViewInit, OnDestroy {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    @ViewChild('container') container!: ElementRef;
    private ruleClient = inject(RulesClient);

    id: undefined | number;
    form = new FormGroup({
        name: new FormControl<string | undefined>(undefined, { nonNullable: true, validators: [Validators.required] })
    });
    typeLib: monaco.IDisposable | undefined;
    editor: monaco.editor.IStandaloneCodeEditor | undefined;
    model: monaco.editor.ITextModel | undefined;
    codeError: string | undefined;
    codeErrorStillThinking = true;
    makerChangeSubscription: monaco.IDisposable | undefined;


    constructor() {
        this.id = this.dialogConfig.data.id;
        this.dialogConfig.header = this.id === undefined ? "Neue Regel" : "Regel bearbeiten";
        this.dialogConfig.width = "800px";
    }
    ngOnDestroy(): void {
        if (this.typeLib) {
            this.typeLib.dispose();
        }
        if (this.model) {
            this.model.dispose();
        }
        if (this.makerChangeSubscription) {
            this.makerChangeSubscription.dispose();
        }
    }

    async ngAfterViewInit(): Promise<void> {
        monaco.languages.typescript.typescriptDefaults.setCompilerOptions({
            target: monaco.languages.typescript.ScriptTarget.ES2020,
            module: monaco.languages.typescript.ModuleKind.ES2015,
            allowNonTsExtensions: true,
            lib: ["es2020"],
            sourceMap: true,
            allowJs: true,
            checkJs: true,
            strict: true
        });

        monaco.languages.typescript.typescriptDefaults.setDiagnosticsOptions({
            noSemanticValidation: false,
            noSyntaxValidation: false,
        });

        var categoryKeys = await lastValueFrom(this.ruleClient.getCategoryKeys());
        var categoryKeysString = categoryKeys.map(x => `${x.name} = ${x.id}`).join('\n');
        this.typeLib = monaco.languages.typescript.typescriptDefaults.addExtraLib(
            `
                declare interface Transaction {
                    purpose: string;
                    name: string;
                    bankCode: string;
                    accountNumber: string;
                    iban: string;
                    bic: string;
                    category: Category;
                    amount: number;
                    endToEndReference: string;
                    customerReference: string;
                    mandateReference: string;
                    creditorIdentifier: string;
                    originatorIdentifier: string;
                    alternateInitiator: string;
                    alternateReceiver: string;
                }

                declare enum Category {
                    ${categoryKeysString}
                }

                declare interface ExtractedEmailItem {
                    FullName: string | null;
                    ShortName: string | null;
                    SubTotal: number | null;
                }

                declare interface ExtractedEmailData {
                    RecipientName: string | null;
                    Merchant: string | null;
                    TransactionTimestamp: string | null;
                    OrderNumber: string | null;
                    Tax: number | null;
                    TotalAmount: number | null;
                    PaymentMethod: string | null;
                    AccountNumber: string | null;
                    TransactionCode: string | null;
                    Items: ExtractedEmailItem[];
                }

                declare interface EmailFilter {
                    recipientName?: string;
                    merchant?: string;
                    transactionTimestamp?: string;
                    orderNumber?: string;
                    tax?: number;
                    totalAmount?: number;
                    paymentMethod?: string;
                    accountNumber?: string;
                    transactionCode?: string;
                }

                declare function findMail(filter: EmailFilter): ExtractedEmailData | null;
            `,
            'file:///types/server/index.d.ts'
        );

        console.log(categoryKeysString);

        let code = 'export function run(t: Transaction) {\n    \n}';
        if (this.id !== undefined) {
            const r = await lastValueFrom(this.ruleClient.getById(this.id));
            this.form.get('name')?.setValue(r.name);
            code = r.originalCode || "";
        }

        this.model = monaco.editor.createModel(code, 'typescript', monaco.Uri.parse('file:///main.ts'));
        this.makerChangeSubscription = monaco.editor.onDidChangeMarkers(() => {
            this.updateMarkerInfo();
        });

        this.editor = monaco.editor.create(this.container.nativeElement, {
            model: this.model,
            scrollBeyondLastLine: false,
            quickSuggestions: {
                other: true,
                comments: true,
                strings: true
            },
            language: 'typescript',
            minimap: {
                enabled: false
            }
        });

        this.updateMarkerInfo();
    }

    private updateMarkerInfo() {
        queueMicrotask(() => {
            const markers = monaco.editor.getModelMarkers({ resource: this.model!.uri });
            const errors = markers.filter(m => m.severity >= monaco.MarkerSeverity.Error);
            if (errors.length === 0) {
                this.codeError = undefined;
            } else {
                this.codeError = `Zeile: ${errors[0].startLineNumber}: ${errors[0].message}`;
            }
            this.codeErrorStillThinking = false;
        });
    }

    get infoMessageSeverity() {
        if (this.codeErrorStillThinking === true) {
            return 'info';
        }
        if (this.codeError !== undefined) {
            return 'error';
        }
        return "success"
    }

    get infoMessage() {
        if (this.codeErrorStillThinking === true) {
            return "Prüfe Code...";
        }
        if (this.codeError !== undefined) {
            return this.codeError;
        }
        return "Keine Fehler gefunden.";
    }

    async onSubmit() {
        const worker = await monaco.languages.typescript.getTypeScriptWorker();
        const svc = await worker(this.model!.uri);
        const emit = await svc.getEmitOutput(this.model!.uri.toString());

        const jsOutput = emit.outputFiles.filter(x => x.name.endsWith('.js'))[0];
        const mapOutput = emit.outputFiles.filter(x => x.name.endsWith('.js.map'))[0];

        try {
            if (this.id === undefined) {
                await lastValueFrom(this.ruleClient.create(new NewRuleRequest({
                    name: this.form.controls.name.value!,
                    originalCode: this.editor?.getModel()?.getValue() || "",
                    compiledCode: jsOutput.text,
                    sourceMap: mapOutput.text
                })));
            } else {
                await lastValueFrom(this.ruleClient.update(new UpdateRuleRequest({
                    id: this.id,
                    name: this.form.controls.name.value!,
                    originalCode: this.editor?.getModel()?.getValue() || "",
                    compiledCode: jsOutput.text,
                    sourceMap: mapOutput.text
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
            } else {
                console.error(error);
            }
        }
    }

    onCancelClicked() {
        this.dialogRef.close(true);
    }
}
