import { Component, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { TreeSelectModule } from 'primeng/treeselect';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { CheckboxModule } from 'primeng/checkbox';
import { CategoryConfigurationClient, CategoryResponse, TransactionDetailsResponse, TransactionDetailsUpdateRequest, TransactionOverrideDetails, TransactionPageClient } from '../../server';
import { lastValueFrom } from 'rxjs';
import { TreeNode } from 'primeng/api';
import { TextareaModule } from 'primeng/textarea';

@Component({
    selector: 'app-transaction-details-dialog',
    imports: [ButtonModule, InputTextModule, DatePickerModule, TreeSelectModule, IconFieldModule, InputIconModule, CheckboxModule, ReactiveFormsModule, TextareaModule],
    templateUrl: './transaction-details-dialog.component.html',
    styleUrl: './transaction-details-dialog.component.scss'
})
export class TransactionDetailsDialogComponent implements OnInit {
    private dynamicDialogRef = inject(DynamicDialogRef);
    private transactionPageClient = inject(TransactionPageClient);
    private categoryConfigurationClient = inject(CategoryConfigurationClient);

    transaction!: TransactionDetailsResponse;

    dateOverriden = false;
    purposeOverriden = false;
    nameOverriden = false;
    bankCodeOverriden = false;
    accountNumberOverriden = false;
    ibanOverriden = false;
    bicOverriden = false;
    amountOverriden = false;
    categoryOverriden = false;
    endToEndReferenceOverriden = false;
    customerReferenceOverriden = false;
    mandateReferenceOverriden = false;
    creditorIdentifierOverriden = false;
    originatorIdentifierOverriden = false;
    alternateInitiatorOverriden = false;
    alternateReceiverOverriden = false;

    form = new FormGroup({
        date: new FormControl<Date>(new Date(), { nonNullable: true }),
        purpose: new FormControl<string>("", { nonNullable: true }),
        name: new FormControl<string>("", { nonNullable: true }),
        bankCode: new FormControl<string>("", { nonNullable: true }),
        accountNumber: new FormControl<string>("", { nonNullable: true }),
        iban: new FormControl<string>("", { nonNullable: true }),
        bic: new FormControl<string>("", { nonNullable: true }),
        amount: new FormControl<string>("", { nonNullable: true }),
        category: new FormControl<TreeNode | undefined>(undefined),
        endToEndReference: new FormControl<string>("", { nonNullable: true }),
        customerReference: new FormControl<string>("", { nonNullable: true }),
        mandateReference: new FormControl<string>("", { nonNullable: true }),
        creditorIdentifier: new FormControl<string>("", { nonNullable: true }),
        originatorIdentifier: new FormControl<string>("", { nonNullable: true }),
        alternateInitiator: new FormControl<string>("", { nonNullable: true }),
        alternateReceiver: new FormControl<string>("", { nonNullable: true }),
        note: new FormControl<string>("", { nonNullable: true }),
    })
    nodes!: any[];
    id: number;
    categoryNodes: TreeNode[] = [];

    constructor() {
        const dialogConfig = inject(DynamicDialogConfig);

        this.id = dialogConfig.data.id;
        dialogConfig.modal = true;
        dialogConfig.width = "700px";
        dialogConfig.header = "Buchungsdetails";
    }

    async ngOnInit(): Promise<void> {
        const categories = await lastValueFrom(this.categoryConfigurationClient.getCategoryTree());
        this.categoryNodes = this.mapCategoriesToNodes(categories);

        this.transaction = await lastValueFrom(this.transactionPageClient.get(this.id));
        this.form.patchValue({
            date: this.transaction.overriddenDetails.date ?? this.transaction.baseDetails.date,
            purpose: this.transaction.overriddenDetails.purpose ?? this.transaction.baseDetails.purpose,
            name: this.transaction.overriddenDetails.name ?? this.transaction.baseDetails.name,
            bankCode: this.transaction.overriddenDetails.bankCode ?? this.transaction.baseDetails.bankCode,
            accountNumber: this.transaction.overriddenDetails.accountNumber ?? this.transaction.baseDetails.accountNumber,
            iban: this.transaction.overriddenDetails.iban ?? this.transaction.baseDetails.iban,
            bic: this.transaction.overriddenDetails.bic ?? this.transaction.baseDetails.bic,
            amount: (this.transaction.overriddenDetails.amount ?? this.transaction.baseDetails.amount)?.toString(),
            category: this.getCatergoryNodeById(this.transaction.overriddenDetails.categoryId ?? this.transaction.baseDetails.categoryId),
            endToEndReference: this.transaction.overriddenDetails.endToEndReference ?? this.transaction.baseDetails.endToEndReference,
            customerReference: this.transaction.overriddenDetails.customerReference ?? this.transaction.baseDetails.customerReference,
            mandateReference: this.transaction.overriddenDetails.mandateReference ?? this.transaction.baseDetails.mandateReference,
            creditorIdentifier: this.transaction.overriddenDetails.creditorIdentifier ?? this.transaction.baseDetails.creditorIdentifier,
            originatorIdentifier: this.transaction.overriddenDetails.originatorIdentifier ?? this.transaction.baseDetails.originatorIdentifier,
            alternateInitiator: this.transaction.overriddenDetails.alternateInitiator ?? this.transaction.baseDetails.alternateInitiator,
            alternateReceiver: this.transaction.overriddenDetails.alternateReceiver ?? this.transaction.baseDetails.alternateReceiver,
            note: this.transaction.note
        });

        this.dateOverriden = this.transaction.overriddenDetails.date !== undefined && this.transaction.overriddenDetails.date !== null;
        this.purposeOverriden = this.transaction.overriddenDetails.purpose !== undefined && this.transaction.overriddenDetails.purpose !== null;
        this.nameOverriden = this.transaction.overriddenDetails.name !== undefined && this.transaction.overriddenDetails.name !== null;
        this.bankCodeOverriden = this.transaction.overriddenDetails.bankCode !== undefined && this.transaction.overriddenDetails.bankCode !== null;
        this.accountNumberOverriden = this.transaction.overriddenDetails.accountNumber !== undefined && this.transaction.overriddenDetails.accountNumber !== null;
        this.ibanOverriden = this.transaction.overriddenDetails.iban !== undefined && this.transaction.overriddenDetails.iban !== null;
        this.bicOverriden = this.transaction.overriddenDetails.bic !== undefined && this.transaction.overriddenDetails.bic !== null;
        this.amountOverriden = this.transaction.overriddenDetails.amount !== undefined && this.transaction.overriddenDetails.amount !== null;
        this.categoryOverriden = this.transaction.overriddenDetails.categoryId !== undefined && this.transaction.overriddenDetails.categoryId !== null;
        this.endToEndReferenceOverriden = this.transaction.overriddenDetails.endToEndReference !== undefined && this.transaction.overriddenDetails.endToEndReference !== null;
        this.customerReferenceOverriden = this.transaction.overriddenDetails.customerReference !== undefined && this.transaction.overriddenDetails.customerReference !== null;
        this.mandateReferenceOverriden = this.transaction.overriddenDetails.mandateReference !== undefined && this.transaction.overriddenDetails.mandateReference !== null;
        this.creditorIdentifierOverriden = this.transaction.overriddenDetails.creditorIdentifier !== undefined && this.transaction.overriddenDetails.creditorIdentifier !== null;
        this.originatorIdentifierOverriden = this.transaction.overriddenDetails.originatorIdentifier !== undefined && this.transaction.overriddenDetails.originatorIdentifier !== null;
        this.alternateInitiatorOverriden = this.transaction.overriddenDetails.alternateInitiator !== undefined && this.transaction.overriddenDetails.alternateInitiator !== null;
        this.alternateReceiverOverriden = this.transaction.overriddenDetails.alternateReceiver !== undefined && this.transaction.overriddenDetails.alternateReceiver !== null;
    }

    getCatergoryNodeById(id: number | undefined): TreeNode | undefined {
        if (id === undefined) {
            return undefined;
        }

        function search(nodes: TreeNode[], id: number | undefined): TreeNode | undefined {
            for (const node of nodes) {
                if (node.data === id) {
                    return node;
                }
                if (node.children) {
                    const found = search(node.children, id);
                    if (found !== undefined) {
                        return found;
                    }
                }
            }
            return undefined;
        }

        return search(this.categoryNodes, id);
    }

    mapCategoriesToNodes(categories: CategoryResponse[]): TreeNode[] {
        return categories.map(category => {
            return {
                label: category.name,
                data: category.id,
                children: this.mapCategoriesToNodes(category.children)
            };
        });
    }

    resetDate() {
        this.form.patchValue({
            date: this.transaction.baseDetails.date
        });
        this.dateOverriden = false;
    }

    resetName() {
        this.form.patchValue({
            name: this.transaction.baseDetails.name
        });
        this.nameOverriden = false;
    }

    resetPurpose() {
        this.form.patchValue({
            purpose: this.transaction.baseDetails.purpose
        });
        this.purposeOverriden = false;
    }

    resetBankCode() {
        this.form.patchValue({
            bankCode: this.transaction.baseDetails.bankCode
        });
        this.bankCodeOverriden = false;
    }

    resetAccountNumber() {
        this.form.patchValue({
            accountNumber: this.transaction.baseDetails.accountNumber
        });
        this.accountNumberOverriden = false;
    }

    resetIban() {
        this.form.patchValue({
            iban: this.transaction.baseDetails.iban
        });
        this.ibanOverriden = false;
    }

    resetBic() {
        this.form.patchValue({
            bic: this.transaction.baseDetails.bic
        });
        this.bicOverriden = false;
    }

    resetAmount() {
        this.form.patchValue({
            amount: this.transaction.baseDetails.amount?.toString()
        });
        this.amountOverriden = false;
    }

    resetCategory() {
        this.form.patchValue({
            category: this.getCatergoryNodeById(this.transaction.baseDetails.categoryId)
        });
        this.categoryOverriden = false;
    }

    resetEndToEndReference() {
        this.form.patchValue({
            endToEndReference: this.transaction.baseDetails.endToEndReference
        });
        this.endToEndReferenceOverriden = false;
    }

    resetCustomerReference() {
        this.form.patchValue({
            customerReference: this.transaction.baseDetails.customerReference
        });
        this.customerReferenceOverriden = false;
    }

    resetMandateReference() {
        this.form.patchValue({
            mandateReference: this.transaction.baseDetails.mandateReference
        });
        this.mandateReferenceOverriden = false;
    }

    resetCreditorIdentifier() {
        this.form.patchValue({
            creditorIdentifier: this.transaction.baseDetails.creditorIdentifier
        });
        this.creditorIdentifierOverriden = false;
    }

    resetOriginatorIdentifier() {
        this.form.patchValue({
            originatorIdentifier: this.transaction.baseDetails.originatorIdentifier
        });
        this.originatorIdentifierOverriden = false;
    }

    resetAlternateInitiator() {
        this.form.patchValue({
            alternateInitiator: this.transaction.baseDetails.alternateInitiator
        });
        this.alternateInitiatorOverriden = false;
    }

    resetAlternateReceiver() {
        this.form.patchValue({
            alternateReceiver: this.transaction.baseDetails.alternateReceiver
        });
        this.alternateReceiverOverriden = false;
    }


    onCancelClicked() {
        this.dynamicDialogRef.close();
    }

    async onSubmit() {
        await lastValueFrom(this.transactionPageClient.update(new TransactionDetailsUpdateRequest({
            id: this.id,
            overriddenDetails: new TransactionOverrideDetails({
                date: this.dateOverriden ? this.form.value.date : undefined,
                purpose: this.purposeOverriden ? this.form.value.purpose : undefined,
                name: this.nameOverriden ? this.form.value.name : undefined,
                bankCode: this.bankCodeOverriden ? this.form.value.bankCode : undefined,
                accountNumber: this.accountNumberOverriden ? this.form.value.accountNumber : undefined,
                iban: this.ibanOverriden ? this.form.value.iban : undefined,
                bic: this.bicOverriden ? this.form.value.bic : undefined,
                amount: this.amountOverriden ? parseFloat(this.form.value.amount ?? "") : undefined,
                categoryId: this.categoryOverriden ? (this.form.value.category as TreeNode).data as number : undefined,
                endToEndReference: this.endToEndReferenceOverriden ? this.form.value.endToEndReference : undefined,
                customerReference: this.customerReferenceOverriden ? this.form.value.customerReference : undefined,
                mandateReference: this.mandateReferenceOverriden ? this.form.value.mandateReference : undefined,
                creditorIdentifier: this.creditorIdentifierOverriden ? this.form.value.creditorIdentifier : undefined,
                originatorIdentifier: this.originatorIdentifierOverriden ? this.form.value.originatorIdentifier : undefined,
                alternateInitiator: this.alternateInitiatorOverriden ? this.form.value.alternateInitiator : undefined,
                alternateReceiver: this.alternateReceiverOverriden ? this.form.value.alternateReceiver : undefined
            }),
            note: this.form.value.note ?? "",
        })));
        this.dynamicDialogRef.close(true);
    }
}
