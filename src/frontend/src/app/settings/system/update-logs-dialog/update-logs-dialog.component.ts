import { Component, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { lastValueFrom } from 'rxjs';
import { SystemClient, UpdateLogEntry } from '../../../server';

@Component({
    selector: 'app-update-logs-dialog',
    imports: [DatePipe, ProgressSpinnerModule],
    templateUrl: './update-logs-dialog.component.html',
    styleUrl: './update-logs-dialog.component.scss'
})
export class UpdateLogsDialogComponent implements OnInit {
    private systemClient = inject(SystemClient);

    logs?: UpdateLogEntry[];

    async ngOnInit() {
        this.logs = await lastValueFrom(this.systemClient.getUpdateLogs());
    }
}
