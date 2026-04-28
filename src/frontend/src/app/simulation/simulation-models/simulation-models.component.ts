import { Component, inject, OnInit, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';

import { SimulationModelListItemResponse, SimulationModelsClient } from '../../server';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { ConfirmationService } from 'primeng/api';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { Router } from '@angular/router';
import { SimulationModelNameDialogComponent } from './simulation-model-name-dialog/simulation-model-name-dialog.component';
import { ModalDialogService } from '../../common/modal-dialog.service';

@Component({
    selector: 'app-simulation-models',
    imports: [PanelModule, ButtonModule, TableModule, TooltipModule, TagModule],
    templateUrl: './simulation-models.component.html',
    styleUrl: './simulation-models.component.scss'
})
export class SimulationModelsComponent implements OnInit {
    private simulationModelsClient = inject(SimulationModelsClient);
    private confirmationService = inject(ConfirmationService);
    private router = inject(Router);
    private modalDialogService = inject(ModalDialogService);

    models = signal(<SimulationModelListItemResponse[]>[]);

    async ngOnInit(): Promise<void> {
        await this.update();
    }

    async onNewModelClicked() {
        const dlg = this.modalDialogService.open(SimulationModelNameDialogComponent, {
            focusOnShow: false,
            data: {},
        });

        const newId = await firstValueFrom(dlg.onClose);
        if (newId === undefined) return;

        await this.router.navigate(['/simulation', newId]);
    }

    onEditModel(model: SimulationModelListItemResponse) {
        this.router.navigate(['/simulation', model.id]);
    }

    onDeleteModel(model: SimulationModelListItemResponse) {
        this.confirmationService.confirm({
            header: 'Modell löschen',
            message: 'Möchten Sie dieses Modell "' + model.name + '" wirklich löschen?',
            acceptLabel: 'Ja',
            rejectLabel: 'Nein',
            accept: async () => {
                await lastValueFrom(this.simulationModelsClient.delete(model.id));
                await this.update();
            },
        });
    }

    private async update() {
        this.models.set(await lastValueFrom(this.simulationModelsClient.getAll()));
    }
}
