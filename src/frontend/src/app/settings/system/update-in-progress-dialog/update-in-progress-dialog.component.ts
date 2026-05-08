import { Component } from '@angular/core';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
    selector: 'app-update-in-progress-dialog',
    imports: [ProgressSpinnerModule],
    templateUrl: './update-in-progress-dialog.component.html',
    styleUrl: './update-in-progress-dialog.component.scss'
})
export class UpdateInProgressDialogComponent {
}
