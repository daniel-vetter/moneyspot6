import { Component, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AppStateService } from '../common/app-state.service';

@Component({
    selector: 'app-welcome-screen',
    imports: [ButtonModule, ProgressSpinnerModule],
    templateUrl: './welcome-screen.component.html',
    styleUrl: './welcome-screen.component.scss'
})
export class WelcomeScreenComponent {
    private appStateService = inject(AppStateService);

    busy = false;

    async startWithDemo() {
        this.busy = true;
        try {
            await this.appStateService.completeFirstSetup(true);
        } finally {
            this.busy = false;
        }
    }

    async startEmpty() {
        this.busy = true;
        try {
            await this.appStateService.completeFirstSetup(false);
        } finally {
            this.busy = false;
        }
    }
}
