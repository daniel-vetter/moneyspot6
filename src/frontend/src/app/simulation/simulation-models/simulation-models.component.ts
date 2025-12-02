import { Component, inject, OnInit, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { PanelModule } from 'primeng/panel';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { SimulationModelResponse, SimulationModelsClient } from '../../server';
import { lastValueFrom } from 'rxjs';
import { ConfirmationService } from 'primeng/api';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { Router } from '@angular/router';


@Component({
    selector: 'app-simulation-models',
    imports: [PanelModule, ButtonModule, TableModule, CommonModule, TooltipModule, TagModule],
    templateUrl: './simulation-models.component.html',
    styleUrl: './simulation-models.component.scss'
})
export class SimulationModelsComponent implements OnInit {
    private simulationModelsClient = inject(SimulationModelsClient);
    private confirmationService = inject(ConfirmationService);
    private router = inject(Router);

    models = signal(<SimulationModelResponse[]>[]);

    async ngOnInit(): Promise<void> {
        await this.update();
    }


    onNewModelClicked() {
        this.router.navigate(['/simulation/new']);
    }

    onEditModel(model: SimulationModelResponse) {
        this.router.navigate(['/simulation', model.id]);
    }

    onDeleteModel(model: SimulationModelResponse) {
        this.confirmationService.confirm({
            header: 'Modell löschen',
            message: 'Möchten Sie dieses Modell "' + model.name + '" wirklich löschen?',
            acceptLabel: 'Ja',
            rejectLabel: 'Nein',
            accept: async () => {
                await lastValueFrom(this.simulationModelsClient.delete(model.id));
                await this.update();
            }
        });
    }

    private async update() {
        this.models.set(await lastValueFrom(this.simulationModelsClient.getAll()));
    }
}
