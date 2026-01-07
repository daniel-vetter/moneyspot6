import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { InputTextModule } from 'primeng/inputtext';
import { TreeSelectModule } from 'primeng/treeselect';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { TreeNode } from 'primeng/api';
import { TransactionDetailsResponse, CategoryConfigurationClient, CategoryResponse, TransactionPageClient, TransactionDetailsUpdateRequest, TransactionOverrideDetails, TransactionType } from '../../../server';
import { lastValueFrom } from 'rxjs';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
    selector: 'app-transaction-details-tab',
    imports: [ButtonModule, InputTextModule, DatePickerModule, TreeSelectModule, ReactiveFormsModule, TextareaModule, ProgressSpinnerModule, SelectModule],
    templateUrl: './transaction-details-tab.component.html',
    styleUrl: './transaction-details-tab.component.scss'
})
export class TransactionDetailsTabComponent implements OnInit {
    private transactionPageClient = inject(TransactionPageClient);
    private categoryConfigurationClient = inject(CategoryConfigurationClient);

    @Input() transactionId!: number;
    @Output() saved = new EventEmitter<void>();

    transaction!: TransactionDetailsResponse;
    categoryNodes: TreeNode[] = [];
    isLoading = false;

    transactionTypeOptions = [
        { label: 'Extern', value: TransactionType.External },
        { label: 'Umbuchung', value: TransactionType.Transfer },
        { label: 'Investment', value: TransactionType.Investment },
        { label: 'Erstattung', value: TransactionType.Refund }
    ];

    dateOverridden = false;
    amountOverridden = false;
    nameOverridden = false;
    bankCodeOverridden = false;
    accountNumberOverridden = false;
    ibanOverridden = false;
    bicOverridden = false;
    purposeOverridden = false;
    categoryOverridden = false;
    endToEndReferenceOverridden = false;
    customerReferenceOverridden = false;
    mandateReferenceOverridden = false;
    creditorIdentifierOverridden = false;
    originatorIdentifierOverridden = false;
    transactionTypeOverridden = false;

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
        transactionType: new FormControl<TransactionType>(TransactionType.External, { nonNullable: true }),
    })

    async ngOnInit(): Promise<void> {
        this.isLoading = true;
        this.form.disable();

        const categories = await lastValueFrom(this.categoryConfigurationClient.getCategoryTree());
        this.categoryNodes = this.mapCategoriesToNodes(categories);
        this.transaction = await lastValueFrom(this.transactionPageClient.get(this.transactionId));

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
            note: this.transaction.note,
            transactionType: this.transaction.overriddenDetails.transactionType ?? this.transaction.baseDetails.transactionType
        });

        this.dateOverridden = this.transaction.overriddenDetails.date !== undefined && this.transaction.overriddenDetails.date !== null;
        this.purposeOverridden = this.transaction.overriddenDetails.purpose !== undefined && this.transaction.overriddenDetails.purpose !== null;
        this.nameOverridden = this.transaction.overriddenDetails.name !== undefined && this.transaction.overriddenDetails.name !== null;
        this.bankCodeOverridden = this.transaction.overriddenDetails.bankCode !== undefined && this.transaction.overriddenDetails.bankCode !== null;
        this.accountNumberOverridden = this.transaction.overriddenDetails.accountNumber !== undefined && this.transaction.overriddenDetails.accountNumber !== null;
        this.ibanOverridden = this.transaction.overriddenDetails.iban !== undefined && this.transaction.overriddenDetails.iban !== null;
        this.bicOverridden = this.transaction.overriddenDetails.bic !== undefined && this.transaction.overriddenDetails.bic !== null;
        this.amountOverridden = this.transaction.overriddenDetails.amount !== undefined && this.transaction.overriddenDetails.amount !== null;
        this.categoryOverridden = this.transaction.overriddenDetails.categoryId !== undefined && this.transaction.overriddenDetails.categoryId !== null;
        this.endToEndReferenceOverridden = this.transaction.overriddenDetails.endToEndReference !== undefined && this.transaction.overriddenDetails.endToEndReference !== null;
        this.customerReferenceOverridden = this.transaction.overriddenDetails.customerReference !== undefined && this.transaction.overriddenDetails.customerReference !== null;
        this.mandateReferenceOverridden = this.transaction.overriddenDetails.mandateReference !== undefined && this.transaction.overriddenDetails.mandateReference !== null;
        this.creditorIdentifierOverridden = this.transaction.overriddenDetails.creditorIdentifier !== undefined && this.transaction.overriddenDetails.creditorIdentifier !== null;
        this.originatorIdentifierOverridden = this.transaction.overriddenDetails.originatorIdentifier !== undefined && this.transaction.overriddenDetails.originatorIdentifier !== null;
        this.transactionTypeOverridden = this.transaction.overriddenDetails.transactionType !== undefined && this.transaction.overriddenDetails.transactionType !== null;

        this.form.enable();
        this.isLoading = false;
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
        this.dateOverridden = false;
    }

    resetName() {
        this.form.patchValue({
            name: this.transaction.baseDetails.name
        });
        this.nameOverridden = false;
    }

    resetPurpose() {
        this.form.patchValue({
            purpose: this.transaction.baseDetails.purpose
        });
        this.purposeOverridden = false;
    }

    resetBankCode() {
        this.form.patchValue({
            bankCode: this.transaction.baseDetails.bankCode
        });
        this.bankCodeOverridden = false;
    }

    resetAccountNumber() {
        this.form.patchValue({
            accountNumber: this.transaction.baseDetails.accountNumber
        });
        this.accountNumberOverridden = false;
    }

    resetIban() {
        this.form.patchValue({
            iban: this.transaction.baseDetails.iban
        });
        this.ibanOverridden = false;
    }

    resetBic() {
        this.form.patchValue({
            bic: this.transaction.baseDetails.bic
        });
        this.bicOverridden = false;
    }

    resetAmount() {
        this.form.patchValue({
            amount: this.transaction.baseDetails.amount?.toString()
        });
        this.amountOverridden = false;
    }

    resetCategory() {
        this.form.patchValue({
            category: this.getCatergoryNodeById(this.transaction.baseDetails.categoryId)
        });
        this.categoryOverridden = false;
    }

    resetEndToEndReference() {
        this.form.patchValue({
            endToEndReference: this.transaction.baseDetails.endToEndReference
        });
        this.endToEndReferenceOverridden = false;
    }

    resetCustomerReference() {
        this.form.patchValue({
            customerReference: this.transaction.baseDetails.customerReference
        });
        this.customerReferenceOverridden = false;
    }

    resetMandateReference() {
        this.form.patchValue({
            mandateReference: this.transaction.baseDetails.mandateReference
        });
        this.mandateReferenceOverridden = false;
    }

    resetCreditorIdentifier() {
        this.form.patchValue({
            creditorIdentifier: this.transaction.baseDetails.creditorIdentifier
        });
        this.creditorIdentifierOverridden = false;
    }

    resetOriginatorIdentifier() {
        this.form.patchValue({
            originatorIdentifier: this.transaction.baseDetails.originatorIdentifier
        });
        this.originatorIdentifierOverridden = false;
    }

    resetTransactionType() {
        this.form.patchValue({
            transactionType: this.transaction.baseDetails.transactionType
        });
        this.transactionTypeOverridden = false;
    }

    async onSubmit() {
        await lastValueFrom(this.transactionPageClient.update(new TransactionDetailsUpdateRequest({
            id: this.transactionId,
            overriddenDetails: new TransactionOverrideDetails({
                date: this.dateOverridden ? this.form.value.date : undefined,
                purpose: this.purposeOverridden ? this.form.value.purpose : undefined,
                name: this.nameOverridden ? this.form.value.name : undefined,
                bankCode: this.bankCodeOverridden ? this.form.value.bankCode : undefined,
                accountNumber: this.accountNumberOverridden ? this.form.value.accountNumber : undefined,
                iban: this.ibanOverridden ? this.form.value.iban : undefined,
                bic: this.bicOverridden ? this.form.value.bic : undefined,
                amount: this.amountOverridden ? parseFloat(this.form.value.amount ?? "") : undefined,
                categoryId: this.categoryOverridden ? (this.form.value.category as TreeNode).data as number : undefined,
                endToEndReference: this.endToEndReferenceOverridden ? this.form.value.endToEndReference : undefined,
                customerReference: this.customerReferenceOverridden ? this.form.value.customerReference : undefined,
                mandateReference: this.mandateReferenceOverridden ? this.form.value.mandateReference : undefined,
                creditorIdentifier: this.creditorIdentifierOverridden ? this.form.value.creditorIdentifier : undefined,
                originatorIdentifier: this.originatorIdentifierOverridden ? this.form.value.originatorIdentifier : undefined,
                alternateInitiator: undefined,
                alternateReceiver: undefined,
                transactionType: this.transactionTypeOverridden ? this.form.value.transactionType : undefined
            }),
            note: this.form.value.note ?? "",
        })));
        this.saved.emit();
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
}
