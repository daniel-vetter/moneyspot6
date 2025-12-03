import {AfterViewInit, Component, inject, OnDestroy} from '@angular/core';
import {ViewChild, ElementRef} from '@angular/core';
import {ButtonModule} from 'primeng/button';
import {MessageModule} from 'primeng/message';
import {SimulationModelsClient, UpdateSimulationModelRequest, SimulationTransactionResponse} from '../../../server';
import {firstValueFrom, lastValueFrom} from 'rxjs';
import {CommonModule} from '@angular/common';
import {ProgressSpinnerModule} from 'primeng/progressspinner';
import {ActivatedRoute, Router} from '@angular/router';
import {PanelModule} from 'primeng/panel';
import {TabsModule} from 'primeng/tabs';
import {SplitterModule} from 'primeng/splitter';
import {TableModule} from 'primeng/table';
import {TooltipModule} from 'primeng/tooltip';
import * as Highcharts from 'highcharts';
import {HighchartsChartModule} from 'highcharts-angular';
import {DialogService} from 'primeng/dynamicdialog';
import {SimulationModelNameDialogComponent} from '../simulation-model-name-dialog/simulation-model-name-dialog.component';

import './monaco-setup';
import * as monaco from 'monaco-editor';


@Component({
    selector: 'app-edit-simulation-model',
    imports: [ButtonModule, MessageModule, CommonModule, ProgressSpinnerModule, PanelModule, TabsModule, SplitterModule, TableModule, TooltipModule, HighchartsChartModule],
    providers: [DialogService],
    templateUrl: './edit-simulation-model.component.html',
    styleUrl: './edit-simulation-model.component.scss'
})
export class EditSimulationModelComponent implements AfterViewInit, OnDestroy {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    @ViewChild('container') container!: ElementRef;
    private simulationModelsClient = inject(SimulationModelsClient);
    private dialogService = inject(DialogService);

    id: undefined | number;
    modelName: string = '';
    typeLib: monaco.IDisposable | undefined;
    editor: monaco.editor.IStandaloneCodeEditor | undefined;
    model: monaco.editor.ITextModel | undefined;
    codeError: string | undefined;
    codeErrorStillThinking = true;
    makerChangeSubscription: monaco.IDisposable | undefined;
    loading = true;
    logs: string[] = [];
    transactions: SimulationTransactionResponse[] = [];
    isRunning = false;
    Highcharts: typeof Highcharts = Highcharts;
    chartOptions: Highcharts.Options | undefined;

    get pageTitle(): string {
        return this.id === undefined ? "Neue Simulation" : `Simulation: ${this.modelName}`;
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
declare const start: DateOnly;
declare const end: DateOnly;
declare const balance: number;
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

declare interface InitialConfig {
    startDate: DateOnly;
    endDate: DateOnly;
    startBalance: number;
    stocks?: StockInitialConfig[];
}

declare interface StockInitialConfig {
    name: string;
    startAmount: number;
    pricePredictor: IPricePredictor;
}

declare interface PricePredictor {
    getValue(date: DateOnly): number;
}

declare class SPPLinearYearly implements PricePredictor {
    constructor(referenceDate: DateOnly, referenceValue: number, increasePerYear: number);
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
    addDays(count: number): DateOnly;
    addMonths(count: number): DateOnly;
    addYears(count: number): DateOnly;
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
            this.modelName = r.name;
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

        this.editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, () => {
            this.onRunClicked();
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
        try {
            await this.saveModel();
            await this.router.navigate(['/simulation']);
        } catch (error) {
            console.error(error);
        }
    }

    onSplitterResized() {
        this.editor?.layout();
    }

    async openNameDialog() {
        const dlg = this.dialogService.open(SimulationModelNameDialogComponent, {
            modal: true,
            focusOnShow: false,
            data: {
                id: this.id,
                name: this.modelName
            }
        });

        const newName = await firstValueFrom(dlg.onClose);
        if (newName) {
            this.modelName = newName;
        }
    }

    private async saveModel() {
        const worker = await monaco.languages.typescript.getTypeScriptWorker();
        const svc = await worker(this.model!.uri);
        const emit = await svc.getEmitOutput(this.model!.uri.toString());

        const jsOutput = emit.outputFiles.filter(x => x.name.endsWith('.js'))[0];
        const mapOutput = emit.outputFiles.filter(x => x.name.endsWith('.js.map'))[0];

        await lastValueFrom(this.simulationModelsClient.update(new UpdateSimulationModelRequest({
            id: this.id!,
            originalCode: this.editor?.getModel()?.getValue() || "",
            compiledCode: jsOutput.text,
            sourceMap: mapOutput.text
        })));
    }

    async onRunClicked() {
        if (this.id === undefined) {
            return;
        }

        this.isRunning = true;
        this.logs = [];
        this.transactions = [];
        this.chartOptions = undefined;

        try {
            await this.saveModel();

            // Run the simulation
            const runId = await lastValueFrom(this.simulationModelsClient.run(this.id));

            // Get the result
            const result = await lastValueFrom(this.simulationModelsClient.getRunResult(runId));
            this.logs = result.logs;
            this.transactions = result.transactions;

            // Build chart from transactions
            if (result.transactions.length > 0) {
                const chartData = result.transactions.map(t => {
                    const date = new Date(t.date);
                    return [Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()), t.balance];
                });

                this.chartOptions = {
                    chart: {
                        type: 'line',
                        height: 300
                    },
                    title: {
                        text: undefined
                    },
                    xAxis: {
                        type: 'datetime',
                        title: { text: undefined }
                    },
                    yAxis: {
                        title: { text: 'Balance' }
                    },
                    legend: {
                        enabled: false
                    },
                    series: [{
                        type: 'line',
                        name: 'Balance',
                        data: chartData
                    }]
                };
            }
        } catch (error) {
            console.error(error);
        } finally {
            this.isRunning = false;
        }
    }
}
