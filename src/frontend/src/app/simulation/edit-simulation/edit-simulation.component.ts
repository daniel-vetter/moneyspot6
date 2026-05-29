import {AfterViewInit, Component, inject, OnDestroy} from '@angular/core';
import {ViewChild, ElementRef} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {ButtonModule} from 'primeng/button';
import {MessageModule} from 'primeng/message';
import {SelectModule} from 'primeng/select';
import {SimulationsClient, UpdateSimulationRequest, SimulationTransactionResponse, SimulationLogResponse, SimulationListItemResponse} from '../../server';
import {firstValueFrom, lastValueFrom, Subscription} from 'rxjs';
import {CommonModule} from '@angular/common';
import {ProgressSpinnerModule} from 'primeng/progressspinner';
import {ActivatedRoute, Router} from '@angular/router';
import {ConfirmationService} from 'primeng/api';
import {PanelModule} from 'primeng/panel';
import {TabsModule} from 'primeng/tabs';
import {SplitterModule} from 'primeng/splitter';
import {TableModule} from 'primeng/table';
import {TooltipModule} from 'primeng/tooltip';
import {EChartsOption} from 'echarts';
import {EchartComponent} from '../../common/echart/echart.component';
import {formatDateDe, formatEur} from '../../common/echart/chart-format';
import {SimulationNameDialogComponent} from '../simulation-name-dialog/simulation-name-dialog.component';
import {ModalDialogService} from '../../common/modal-dialog.service';
import {ThemeService} from '../../common/theme.service';
import {pickLatestSimulationId, rememberSimulationId} from '../last-simulation';

import './monaco-setup';
import * as monaco from 'monaco-editor';

interface SimTooltipParam {
    color: string;
    seriesName: string;
    value: [number, number];
}


@Component({
    selector: 'app-edit-simulation',
    imports: [ButtonModule, MessageModule, CommonModule, FormsModule, ProgressSpinnerModule, PanelModule, SelectModule, TabsModule, SplitterModule, TableModule, TooltipModule, EchartComponent],
    templateUrl: './edit-simulation.component.html',
    styleUrl: './edit-simulation.component.scss'
})
export class EditSimulationComponent implements AfterViewInit, OnDestroy {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    @ViewChild('container') container!: ElementRef;
    private simulationsClient = inject(SimulationsClient);
    private modalDialogService = inject(ModalDialogService);
    private themeService = inject(ThemeService);
    private confirmationService = inject(ConfirmationService);

    id: undefined | number;
    simulations: SimulationListItemResponse[] = [];
    private routeSub: Subscription | undefined;
    private modelUriCounter = 0;
    currentRevisionId: undefined | number;
    simulationName: string = '';
    typeLib: monaco.IDisposable | undefined;
    editor: monaco.editor.IStandaloneCodeEditor | undefined;
    model: monaco.editor.ITextModel | undefined;
    codeError: string | undefined;
    codeErrorStillThinking = true;
    makerChangeSubscription: monaco.IDisposable | undefined;
    loading = true;
    logs: SimulationLogResponse[] = [];
    transactions: SimulationTransactionResponse[] = [];
    activeTab = '0';
    @ViewChild('logContent') logContent?: ElementRef<HTMLDivElement>;
    isRunning = false;
    totalChartOptions: EChartsOption | undefined;
    chartOptions: EChartsOption | undefined;
    stockChartOptions: EChartsOption | undefined;
    maximizedChart: 'total' | 'balance' | 'stock' | null = null;

    ngOnDestroy(): void {
        this.routeSub?.unsubscribe();
        if (this.typeLib) {
            this.typeLib.dispose();
        }
        if (this.editor) {
            this.editor.dispose();
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

        monaco.typescript.typescriptDefaults.setCompilerOptions({
            target: monaco.typescript.ScriptTarget.ES2020,
            module: monaco.typescript.ModuleKind.ES2015,
            allowNonTsExtensions: true,
            lib: ["es2020"],
            sourceMap: true,
            allowJs: true,
            checkJs: true,
            strict: true
        });

        monaco.typescript.typescriptDefaults.setDiagnosticsOptions({
            noSemanticValidation: false,
            noSyntaxValidation: false,
        });

        this.typeLib = monaco.typescript.typescriptDefaults.addExtraLib(
            `
declare const today: DateOnly;
declare const start: DateOnly;
declare const end: DateOnly;
declare const balance: number;
declare function addTransaction(purpose: string, amount: number): void;
declare function buyStocksFor(stockName: string, amount: number): void;
declare function sellStocksFor(stockName: string, netAmount: number): void;
declare function getTotalStockValue(): number;
declare function adjust(amount: number): Adjustment;
declare function log(message: any): void;

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

        // Create the editor once with an empty model. The actual model is swapped in by loadSimulation()
        // whenever the route id changes, so switching simulations reuses the same editor instance.
        this.model = this.createModel('export function onTick() {\n    \n}');
        this.makerChangeSubscription = monaco.editor.onDidChangeMarkers(() => {
            this.updateMarkerInfo();
        });

        this.editor = monaco.editor.create(this.container.nativeElement, {
            model: this.model,
            theme: this.themeService.isDark ? 'vs-dark' : 'vs',
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

        await this.refreshSimulations();

        this.routeSub = this.route.paramMap.subscribe(params => {
            const idParam = params.get('id');
            void this.loadSimulation(idParam ? parseInt(idParam, 10) : undefined);
        });
    }

    private createModel(code: string): monaco.editor.ITextModel {
        // Unique uri per load so a freshly created model never collides with the one being replaced.
        return monaco.editor.createModel(code, 'typescript', monaco.Uri.parse(`file:///simulation-model-${this.modelUriCounter++}.ts`));
    }

    private async refreshSimulations(): Promise<void> {
        this.simulations = [...await lastValueFrom(this.simulationsClient.getAll())];
    }

    private async loadSimulation(id: number | undefined): Promise<void> {
        this.id = id;

        // Reset per-simulation state so the previous simulation's results don't leak into the new one.
        this.logs = [];
        this.transactions = [];
        this.totalChartOptions = undefined;
        this.chartOptions = undefined;
        this.stockChartOptions = undefined;
        this.maximizedChart = null;
        this.maximizedChartOptions = undefined;
        this.activeTab = '0';
        this.currentRevisionId = undefined;
        this.simulationName = '';

        let code = 'export function onTick() {\n    \n}';
        if (id !== undefined) {
            const r = await lastValueFrom(this.simulationsClient.getById(id));
            this.simulationName = r.name;
            this.currentRevisionId = r.latestRevisionId ?? undefined;
            code = r.originalCode || "";
            rememberSimulationId(id);

            if (this.currentRevisionId !== undefined) {
                await this.loadRunResult(this.currentRevisionId);
            }
        }

        const previous = this.model;
        this.model = this.createModel(code);
        this.editor?.setModel(this.model);
        previous?.dispose();

        this.updateMarkerInfo();
        this.loading = false;
    }

    async onSimulationSwitch(id: number): Promise<void> {
        if (id === this.id) {
            return;
        }
        // Persist any edits to the current simulation before navigating away from it.
        if (this.id !== undefined && this.currentRevisionId !== undefined) {
            await this.saveSimulation();
        }
        await this.router.navigate(['/simulation', id]);
    }

    async onNewSimulationClicked(): Promise<void> {
        const dlg = this.modalDialogService.open(SimulationNameDialogComponent, {
            focusOnShow: false,
            data: {},
        });

        const newId = await firstValueFrom(dlg.onClose);
        if (newId === undefined) return;

        await this.refreshSimulations();
        await this.router.navigate(['/simulation', newId]);
    }

    onDeleteClicked(): void {
        if (this.id === undefined) return;
        const id = this.id;
        this.confirmationService.confirm({
            header: 'Simulation löschen',
            message: 'Möchten Sie die Simulation "' + this.simulationName + '" wirklich löschen?',
            acceptLabel: 'Ja',
            rejectLabel: 'Nein',
            accept: async () => {
                await lastValueFrom(this.simulationsClient.delete(id));
                // Back to /simulation, which redirects to the next available simulation or the empty state.
                await this.router.navigate(['/simulation']);
            },
        });
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

    async onSaveClicked() {
        await this.saveSimulation();
    }

    onSplitterResized() {
        this.editor?.layout();
    }

    async openNameDialog() {
        const dlg = this.modalDialogService.open(SimulationNameDialogComponent, {
            focusOnShow: false,
            data: {
                id: this.id,
                name: this.simulationName
            }
        });

        const newName = await firstValueFrom(dlg.onClose);
        if (newName) {
            this.simulationName = newName;
            await this.refreshSimulations();
        }
    }

    maximizedChartOptions: EChartsOption | undefined;

    toggleMaximizeChart(chart: 'total' | 'balance' | 'stock') {
        if (this.maximizedChart === chart) {
            this.maximizedChart = null;
            this.maximizedChartOptions = undefined;
        } else {
            this.maximizedChart = chart;
            this.maximizedChartOptions = chart === 'total' ? this.totalChartOptions
                : chart === 'balance' ? this.chartOptions
                    : this.stockChartOptions;
        }
    }

    private async saveSimulation() {
        const worker = await monaco.typescript.getTypeScriptWorker();
        const svc = await worker(this.model!.uri);
        const emit = await svc.getEmitOutput(this.model!.uri.toString());

        const jsOutput = emit.outputFiles.filter(x => x.name.endsWith('.js'))[0];
        const mapOutput = emit.outputFiles.filter(x => x.name.endsWith('.js.map'))[0];

        this.currentRevisionId = await lastValueFrom(this.simulationsClient.update(new UpdateSimulationRequest({
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
        try {
            this.logs = [];
            this.transactions = [];
            this.totalChartOptions = undefined;
            this.chartOptions = undefined;
            this.stockChartOptions = undefined;

            await this.saveSimulation();

            // Run the simulation
            await lastValueFrom(this.simulationsClient.run(this.currentRevisionId!));

            // Load the result
            await this.loadRunResult(this.currentRevisionId!);

            // If there are error logs, switch to log tab and scroll to bottom
            if (this.logs.some(l => l.isError)) {
                this.activeTab = '2';
                setTimeout(() => {
                    if (this.logContent) {
                        this.logContent.nativeElement.scrollTop = this.logContent.nativeElement.scrollHeight;
                    }
                }, 0);
            }
        } finally {
            this.isRunning = false;
        }
    }

    private async loadRunResult(revisionId: number) {
        const result = await lastValueFrom(this.simulationsClient.getRunResult(revisionId));
        this.logs = result.logs;
        this.transactions = result.transactions;

        if (result.daySummaries.length === 0) return;

        const toUtc = (d: Date) => Date.UTC(d.getFullYear(), d.getMonth(), d.getDate());

        const totalSim = result.daySummaries.map(d => [toUtc(new Date(d.date)), d.balance + d.totalStockValue] as [number, number]);
        const balanceSim = result.daySummaries.map(d => [toUtc(new Date(d.date)), d.balance] as [number, number]);
        const stockSim = result.daySummaries.map(d => [toUtc(new Date(d.date)), d.totalStockValue] as [number, number]);

        const balanceActual = result.actualBalances.map(b => [toUtc(new Date(b.date)), b.balance] as [number, number]);
        const stockActual = result.actualStockValues.map(s => [toUtc(new Date(s.date)), s.value] as [number, number]);

        const totalActualMap = new Map<number, number>();
        for (const [k, v] of balanceActual) totalActualMap.set(k, (totalActualMap.get(k) ?? 0) + v);
        for (const [k, v] of stockActual) totalActualMap.set(k, (totalActualMap.get(k) ?? 0) + v);
        const totalActual = Array.from(totalActualMap.entries()).sort((a, b) => a[0] - b[0]) as [number, number][];

        this.totalChartOptions = EditSimulationComponent.buildLineChart(totalSim, totalActual);
        this.chartOptions = EditSimulationComponent.buildLineChart(balanceSim, balanceActual);
        this.stockChartOptions = EditSimulationComponent.buildLineChart(stockSim, stockActual);
    }

    private static buildLineChart(simulated: [number, number][], actual: [number, number][]): EChartsOption {
        return {
            animation: false,
            grid: {left: 10, right: 20, top: 30, bottom: 30, containLabel: true},
            legend: {show: true, top: 0},
            tooltip: {
                trigger: 'axis',
                formatter: (params: unknown) => EditSimulationComponent.tooltipFormatter(params as SimTooltipParam[])
            },
            xAxis: {type: 'time'},
            yAxis: {
                type: 'value',
                scale: true,
                axisLabel: {formatter: (v: number) => `${v.toLocaleString('de-DE')} €`}
            },
            series: [
                {name: 'Simuliert', type: 'line', showSymbol: false, data: simulated},
                {name: 'Echt', type: 'line', showSymbol: false, data: actual}
            ]
        };
    }

    private static tooltipFormatter(params: SimTooltipParam[]): string {
        if (params.length === 0) return '';
        let html = `<b>${formatDateDe(params[0].value[0])}</b><br/>`;
        for (const p of params) {
            html += `<span style="color:${p.color}">●</span> ${p.seriesName}: <b>${formatEur(p.value[1])}</b><br/>`;
        }
        return html;
    }
}
