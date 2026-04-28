import { Component, OnInit, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { MenuComponent } from '../menu/menu.component';
import { NavigationSkipped, NavigationStart, Router } from '@angular/router';
import { AccountSyncComponent } from "../account-sync/account-sync.component";
import { ModalDialogService } from '../common/modal-dialog.service';

@Component({
    selector: 'app-top-bar',
    imports: [ButtonModule, AccountSyncComponent],
    templateUrl: './top-bar.component.html',
    styleUrl: './top-bar.component.scss'
})
export class TopBarComponent implements OnInit {
    private modalDialogService = inject(ModalDialogService);
    private router = inject(Router);


    _dlg: DynamicDialogRef | undefined;

    ngOnInit(): void {
        this.router.events.subscribe((x) => {
            if (x instanceof NavigationSkipped || x instanceof NavigationStart) {
                this.closeDialog();
            }
        });
    }

    onOpenMenuClicked() {
        this._dlg = this.modalDialogService.open(MenuComponent, {
            dismissableMask: true,
            position: 'left',
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
