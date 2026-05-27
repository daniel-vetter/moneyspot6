import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { TableModule } from 'primeng/table';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { lastValueFrom } from 'rxjs';
import { MailIntegrationClient, SyncJobResponse } from '../../../server';

@Component({
    selector: 'app-sync-jobs-dialog',
    imports: [TableModule, ProgressSpinnerModule, ButtonModule, TagModule, DatePipe],
    templateUrl: './sync-jobs-dialog.component.html',
    styleUrl: './sync-jobs-dialog.component.scss'
})
export class SyncJobsDialogComponent implements OnInit {
    private dialogConfig = inject(DynamicDialogConfig);
    private dialogRef = inject(DynamicDialogRef);
    private mailIntegrationClient = inject(MailIntegrationClient);

    jobs = signal<SyncJobResponse[] | undefined>(undefined);

    constructor() {
        this.dialogConfig.header = 'Sync-Verlauf';
        this.dialogConfig.width = '900px';
    }

    async ngOnInit(): Promise<void> {
        const result = await lastValueFrom(this.mailIntegrationClient.getSyncJobs());
        this.jobs.set(result);
    }

    durationSeconds(job: SyncJobResponse): number {
        return (job.finishedAt.getTime() - job.startedAt.getTime()) / 1000;
    }

    protected onCloseClicked(): void {
        this.dialogRef.close();
    }
}
