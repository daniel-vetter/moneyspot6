import {AfterViewInit, Component, inject, OnDestroy} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {ViewChild, ElementRef} from '@angular/core';
import {ButtonModule} from 'primeng/button';
import {MessageModule} from 'primeng/message';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {InputTextModule} from 'primeng/inputtext';
import {NewSimulationModelRequest, SimulationModelsClient, SimulationModelValidationErrorResponse, UpdateSimulationModelRequest} from '../../../server';
import {lastValueFrom} from 'rxjs';
import {CommonModule} from '@angular/common';
import {ProgressSpinnerModule} from 'primeng/progressspinner';
import {ActivatedRoute, Router} from '@angular/router';
import {PanelModule} from 'primeng/panel';
import {DatePickerModule} from 'primeng/datepicker';

import './monaco-setup';
import * as monaco from 'monaco-editor';


@Component({
    selector: 'app-edit-simulation-model',
    imports: [FormsModule, ButtonModule, MessageModule, ReactiveFormsModule, InputTextModule, CommonModule, MessageModule, ProgressSpinnerModule, PanelModule, DatePickerModule],
    templateUrl: './edit-simulation-model.component.html',
    styleUrl: './edit-simulation-model.component.scss'
})
export class EditSimulationModelComponent implements AfterViewInit, OnDestroy {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    @ViewChild('container') container!: ElementRef;
    private simulationModelsClient = inject(SimulationModelsClient);

    id: undefined | number;
    form = new FormGroup({
        name: new FormControl<string | undefined>(undefined, {nonNullable: true, validators: [Validators.required]}),
        startDate: new FormControl<Date | undefined>(undefined, {nonNullable: true, validators: [Validators.required]}),
        endDate: new FormControl<Date | undefined>(undefined, {nonNullable: true, validators: [Validators.required]})
    });
    typeLib: monaco.IDisposable | undefined;
    editor: monaco.editor.IStandaloneCodeEditor | undefined;
    model: monaco.editor.ITextModel | undefined;
    codeError: string | undefined;
    codeErrorStillThinking = true;
    makerChangeSubscription: monaco.IDisposable | undefined;
    loading = true;

    get pageTitle(): string {
        return this.id === undefined ? "Neues Modell" : "Modell bearbeiten";
    }

    constructor() {
        const idParam = this.route.snapshot.paramMap.get('id');
        this.id = idParam ? parseInt(idParam, 10) : undefined;
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
        await Promise.resolve();

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

        this.typeLib = monaco.languages.typescript.typescriptDefaults.addExtraLib(
            `
declare const today: DateOnly;
declare function addTransaction(purpose: string, amount: number): void;
declare function adjust(amount: number): Adjustment;

declare class Adjustment {
    from(date: DateOnly): AdjustmentWithStartDate;
    from(year: number, month: number, day: number): AdjustmentWithStartDate;
}

declare class AdjustmentWithStartDate {
    to(date: DateOnly): number;
    to(year: number, month: number, day: number): number;
}


declare class DateOnly {
    constructor(year: number, month: number, day: number);
    toString(): string;
    is(year: number, month: number, day: number): boolean;
    is(date: DateOnly): boolean;
    isNot(year: number, month: number, day: number): boolean;
    isNot(date: DateOnly): boolean;
    isBefore(year: number, month: number, day: number): boolean;
    isBefore(date: DateOnly): boolean;
    isAfter(year: number, month: number, day: number): boolean;
    isAfter(date: DateOnly): boolean;
    isBeforeOrEqual(year: number, month: number, day: number): boolean;
    isBeforeOrEqual(date: DateOnly): boolean;
    isAfterOrEqual(year: number, month: number, day: number): boolean;
    isAfterOrEqual(date: DateOnly): boolean;
    isBetween(start: DateOnly, end: DateOnly);
    readonly year: number;
    readonly month: number;
    readonly day: number;
}
            `,
            'file:///types/simulation/index.d.ts'
        );

        let code = 'export function onTick() {\n    \n}';
        if (this.id !== undefined) {
            const r = await lastValueFrom(this.simulationModelsClient.getById(this.id));
            this.form.get('name')?.setValue(r.name);
            this.form.get('startDate')?.setValue(new Date(r.startDate));
            this.form.get('endDate')?.setValue(new Date(r.endDate));
            code = r.originalCode || "";
        }

        this.model = monaco.editor.createModel(code, 'typescript', monaco.Uri.parse('file:///simulation-model.ts'));
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
        this.loading = false;

    }

    private updateMarkerInfo() {
        const markers = monaco.editor.getModelMarkers({resource: this.model!.uri});
        const errors = markers.filter(m => m.severity >= monaco.MarkerSeverity.Error);
        if (errors.length === 0) {
            this.codeError = undefined;
        } else {
            this.codeError = `Zeile: ${errors[0].startLineNumber}: ${errors[0].message}`;
        }
        this.codeErrorStillThinking = false;
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
                await lastValueFrom(this.simulationModelsClient.create(new NewSimulationModelRequest({
                    name: this.form.controls.name.value!,
                    startDate: this.form.controls.startDate.value!,
                    endDate: this.form.controls.endDate.value!,
                    originalCode: this.editor?.getModel()?.getValue() || "",
                    compiledCode: jsOutput.text,
                    sourceMap: mapOutput.text
                })));
            } else {
                await lastValueFrom(this.simulationModelsClient.update(new UpdateSimulationModelRequest({
                    id: this.id,
                    name: this.form.controls.name.value!,
                    startDate: this.form.controls.startDate.value!,
                    endDate: this.form.controls.endDate.value!,
                    originalCode: this.editor?.getModel()?.getValue() || "",
                    compiledCode: jsOutput.text,
                    sourceMap: mapOutput.text
                })));
            }

            await this.router.navigate(['/simulation']);
        } catch (error) {
            if (error instanceof SimulationModelValidationErrorResponse) {
                queueMicrotask(() => {
                    if (error.missingName)
                        this.form.controls.name.setErrors({missingName: true});
                    if (error.nameAlreadyInUse)
                        this.form.controls.name.setErrors({nameAlreadyInUse: true});
                });
            } else {
                console.error(error);
            }
        }
    }

    onCancelClicked() {
        this.router.navigate(['/simulation']);
    }
}
