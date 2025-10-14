import { Component, inject, OnInit, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { PanelModule } from 'primeng/panel';
import { TableModule, TableRowReorderEvent } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { TreeTableModule } from 'primeng/treetable';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { EditRuleComponent } from './edit-rule/edit-rule.component';
import { ReorderRulesRequest, RuleResponse, RulesClient } from '../../server';
import { firstValueFrom, lastValueFrom, Observable } from 'rxjs';
import { ConfirmationService } from 'primeng/api';


@Component({
    selector: 'app-rules',
    imports: [PanelModule, ButtonModule, TableModule, CommonModule],
    providers: [DialogService],
    templateUrl: './rules.component.html',
    styleUrl: './rules.component.scss'
})
export class RulesComponent implements OnInit {
    private dialogRef: DynamicDialogRef | undefined;
    private ruleClient = inject(RulesClient);
    private dialogService = inject(DialogService);
    private confirmationService = inject(ConfirmationService);

    rules = signal(<RuleResponse[]>[]);

    async ngOnInit(): Promise<void> {
        await this.update();
    }


    async onNewRuleClicked() {
        this.dialogRef = this.dialogService.open(EditRuleComponent, {
            modal: true,
            data: {}
        });

        await firstValueFrom(this.dialogRef.onClose);
        await this.update();
    }

    async onEditRule(rule: RuleResponse) {
        this.dialogRef = this.dialogService.open(EditRuleComponent, {
            modal: true,
            data: {
                id: rule.id
            }
        });

        await firstValueFrom(this.dialogRef.onClose);
        await this.update();
    }

    onDeleteRule(rule: RuleResponse) {
        const dlg = this.confirmationService.confirm({
            header: 'Regel löschen',
            message: 'Möchten Sie dies Regel "' + rule.name + '" wirklich löschen?',
            acceptLabel: 'Ja',
            rejectLabel: 'Nein',
            accept: async () => {
                await lastValueFrom(this.ruleClient.delete(rule.id));
                await this.update();
            }
        });
    }

    async onRowReorder(e: TableRowReorderEvent) {
        const rules = this.rules();
        const ids = rules.map(r => r.id!);

        await lastValueFrom(this.ruleClient.reorder(new ReorderRulesRequest({ ids: ids })));
    }


    private async update() {
        this.rules.set(await lastValueFrom(this.ruleClient.getAll()));
    }
}

