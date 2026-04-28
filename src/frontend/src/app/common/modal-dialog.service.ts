import { inject, Injectable, Type } from '@angular/core';
import { DialogService, DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';

@Injectable({ providedIn: 'root' })
export class ModalDialogService {
    private dialogService = inject(DialogService);

    open<T>(component: Type<T>, config: DynamicDialogConfig = {}): DynamicDialogRef<T> {
        const ref = this.dialogService.open(component, { modal: true, ...config });
        if (!ref) {
            throw new Error(`ModalDialogService: dialog for ${component.name} was not opened (PrimeNG returned null).`);
        }
        return ref;
    }
}
