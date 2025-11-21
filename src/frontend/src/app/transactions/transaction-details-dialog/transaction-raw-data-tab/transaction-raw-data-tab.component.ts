import { Component, Input, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { DatePickerModule } from 'primeng/datepicker';
import { TransactionParsedDataResponse, TransactionPageClient } from '../../../server';
import { lastValueFrom } from 'rxjs';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
    selector: 'app-transaction-raw-data-tab',
    imports: [InputTextModule, DatePickerModule, ReactiveFormsModule, ProgressSpinnerModule],
    templateUrl: './transaction-raw-data-tab.component.html',
    styleUrl: './transaction-raw-data-tab.component.scss'
})
export class TransactionRawDataTabComponent implements OnInit {
    private transactionPageClient = inject(TransactionPageClient);

    @Input() transactionId!: number;

    transaction!: TransactionParsedDataResponse;
    isLoading = false;

    rawDataForm = new FormGroup({
        date: new FormControl<Date>(new Date(), { nonNullable: true }),
        purpose: new FormControl<string>("", { nonNullable: true }),
        name: new FormControl<string>("", { nonNullable: true }),
        bankCode: new FormControl<string>("", { nonNullable: true }),
        accountNumber: new FormControl<string>("", { nonNullable: true }),
        iban: new FormControl<string>("", { nonNullable: true }),
        bic: new FormControl<string>("", { nonNullable: true }),
        amount: new FormControl<string>("", { nonNullable: true }),
        endToEndReference: new FormControl<string>("", { nonNullable: true }),
        customerReference: new FormControl<string>("", { nonNullable: true }),
        mandateReference: new FormControl<string>("", { nonNullable: true }),
        creditorIdentifier: new FormControl<string>("", { nonNullable: true }),
        originatorIdentifier: new FormControl<string>("", { nonNullable: true }),
        alternateInitiator: new FormControl<string>("", { nonNullable: true }),
        alternateReceiver: new FormControl<string>("", { nonNullable: true }),
    })

    async ngOnInit(): Promise<void> {
        this.isLoading = true;

        this.transaction = await lastValueFrom(this.transactionPageClient.getParsedData(this.transactionId));

        this.rawDataForm.patchValue({
            date: this.transaction.date,
            purpose: this.transaction.purpose,
            name: this.transaction.name,
            bankCode: this.transaction.bankCode,
            accountNumber: this.transaction.accountNumber,
            iban: this.transaction.iban,
            bic: this.transaction.bic,
            amount: this.transaction.amount?.toString(),
            endToEndReference: this.transaction.endToEndReference,
            customerReference: this.transaction.customerReference,
            mandateReference: this.transaction.mandateReference,
            creditorIdentifier: this.transaction.creditorIdentifier,
            originatorIdentifier: this.transaction.originatorIdentifier,
            alternateInitiator: this.transaction.alternateInitiator,
            alternateReceiver: this.transaction.alternateReceiver
        });

        this.isLoading = false;
    }
}
