import { Component, inject, OnInit, signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { PanelModule } from 'primeng/panel';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

import { SimulationsClient } from '../server';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { Router } from '@angular/router';
import { SimulationNameDialogComponent } from './simulation-name-dialog/simulation-name-dialog.component';
import { ModalDialogService } from '../common/modal-dialog.service';
import { pickLatestSimulationId } from './last-simulation';

@Component({
    selector: 'app-simulation',
    imports: [PanelModule, ButtonModule, ProgressSpinnerModule],
    templateUrl: './simulation.component.html',
    styleUrl: './simulation.component.scss'
})
export class SimulationComponent implements OnInit {
    private simulationsClient = inject(SimulationsClient);
    private router = inject(Router);
    private modalDialogService = inject(ModalDialogService);

    // Becomes false once we know there is no simulation to redirect to, so the empty state shows.
    redirecting = signal(true);

    async ngOnInit(): Promise<void> {
        const simulations = await lastValueFrom(this.simulationsClient.getAll());
        const latestId = pickLatestSimulationId(simulations.map(m => m.id));
        if (latestId !== undefined) {
            await this.router.navigate(['/simulation', latestId], { replaceUrl: true });
            return;
        }
        this.redirecting.set(false);
    }

    async onNewSimulationClicked() {
        const dlg = this.modalDialogService.open(SimulationNameDialogComponent, {
            focusOnShow: false,
            data: {},
        });

        const newId = await firstValueFrom(dlg.onClose);
        if (newId === undefined) return;

        await this.router.navigate(['/simulation', newId]);
    }
}
