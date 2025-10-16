import { Component, OnInit, inject } from '@angular/core';
import { ConfirmationService, TreeNode } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { TreeTableModule } from 'primeng/treetable';
import { NewCategoryDialogComponent } from './new-category-dialog/new-category-dialog.component';
import { DialogService } from "primeng/dynamicdialog";
import { CategoryConfigurationClient, CategoryResponse } from '../../server';
import { firstValueFrom, lastValueFrom } from 'rxjs';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PanelModule } from 'primeng/panel';

@Component({
    selector: 'app-categories',
    imports: [TreeTableModule, ButtonModule, ConfirmDialogModule, PanelModule],
    providers: [DialogService],
    templateUrl: './categories.component.html',
    styleUrl: './categories.component.scss'
})
export class CategoriesComponent implements OnInit {
    private dialogService = inject(DialogService);
    private categoryConfigurationClient = inject(CategoryConfigurationClient);
    private confirmationService = inject(ConfirmationService);


    entries: TreeNode[] = [];

    async onNewCategoryClicked(parentId: number | undefined) {
        const dlg = this.dialogService.open(NewCategoryDialogComponent, {
            modal: true,
            data: {
                parentId: parentId
            }
        });

        await firstValueFrom(dlg.onClose);
        await this.update();
    }

    async onEditCategoryClicked(id: any) {
        const dlg = this.dialogService.open(NewCategoryDialogComponent, {
            modal: true,
            data: {
                id: id
            }
        });

        await firstValueFrom(dlg.onClose);
        await this.update();
    }

    onDeleteCategoryClicked(id: any) {
        const dlg = this.confirmationService.confirm({
            header: 'Kategorie löschen',
            message: 'Möchten Sie diese Kategorie und alle ihre Unterkategorien wirklich löschen?',
            acceptLabel: 'Ja',
            rejectLabel: 'Nein',
            accept: async () => {
                await lastValueFrom(this.categoryConfigurationClient.delete(id));
                await this.update();
            }
        });
    }


    async ngOnInit(): Promise<void> {
        await this.update();
    }

    async update() {
        const tree = await lastValueFrom(this.categoryConfigurationClient.getCategoryTree());
        const openNodes = this.getOpenNodes(this.entries, new Set<number>());
        this.entries = this.map(tree);
        this.restoreOpenNodes(this.entries, openNodes);
    }

    restoreOpenNodes(entries: TreeNode<any>[], openNodes: Set<number>) {
        for (const entry of entries) {
            if (openNodes.has(entry.data.id)) {
                entry.expanded = true;
            }
            this.restoreOpenNodes(entry.children || [], openNodes);
        }
    }

    private getOpenNodes(entries: TreeNode[], result: Set<number>): Set<number> {
        for (const entry of entries) {
            if (entry.expanded) {
                result.add(entry.data.id);
            }
            this.getOpenNodes(entry.children || [], result);
        }
        return result;
    }

    private map(categories: CategoryResponse[]): TreeNode[] {
        const r: TreeNode[] = [];
        for (const entry of categories) {
            r.push({
                key: entry.id.toString(),
                data: {
                    id: entry.id,
                    name: entry.name,
                },
                children: this.map(entry.children || [])
            })
        }
        return r;
    }
}
