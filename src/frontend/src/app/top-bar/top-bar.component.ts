import { Component, OnInit } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MenuComponent } from '../menu/menu.component';
import { NavigationSkipped, NavigationStart, Router } from '@angular/router';
import { AccountSyncComponent } from "../account-sync/account-sync.component";

@Component({
    selector: 'app-top-bar',
    imports: [ButtonModule, AccountSyncComponent],
    providers: [DialogService],
    templateUrl: './top-bar.component.html',
    styleUrl: './top-bar.component.scss'
})
export class TopBarComponent implements OnInit {
    constructor(
        private dialogService: DialogService,
        private router: Router,
    ) { }

    _dlg: DynamicDialogRef | undefined;

    ngOnInit(): void {
        this.router.events.subscribe((x) => {
            if (x instanceof NavigationSkipped || x instanceof NavigationStart) {
                this.closeDialog();
            }
        });
    }

    onOpenMenuClicked() {
        this._dlg = this.dialogService.open(MenuComponent, {
            dismissableMask: true,
            position: 'left',
            modal: true,
            styleClass: 'sideDialog',
            transitionOptions: '0s',
            showHeader: false,
        });
    }

    closeDialog() {
        if (this._dlg !== undefined) {
            this._dlg.close();
            this._dlg = undefined;
        }
    }
}
