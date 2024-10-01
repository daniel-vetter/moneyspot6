import { Component, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { TransactionEntryResponse, TransactionPageClient, TransactionResponse } from '../server';
import { lastValueFrom } from 'rxjs';
import { ValueComponent } from '../common/value/value.component';
import { CustomDatePipe } from '../common/custom-date.pipe';
import { PanelModule } from 'primeng/panel';
import { InputTextModule } from 'primeng/inputtext';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
  selector: 'app-transactions',
  standalone: true,
  imports: [TableModule, ValueComponent, PanelModule, CustomDatePipe, InputTextModule, FormsModule, ButtonModule, DropdownModule, ProgressSpinnerModule],
  templateUrl: './transactions.component.html',
  styleUrl: './transactions.component.scss'
})
export class TransactionsComponent implements OnInit {

  transactions: TransactionResponse[] = [];
  groupsShown: Group[] = [];
  groupsHidden: Group[] = [];
  searchText: string = "";
  showClearButton = false;
  isLoading = false;

  groupOptions: GroupingSelecteItem[] = [
    { name: "Keine", key: "None" },
    { name: "Monatlich", key: "Monthly" },
    { name: "Jährlich", key: "Yearly" },
  ]
  selectedGrouping: Grouping = "Monthly";

  constructor(private transactionPageClient: TransactionPageClient, private router: Router, private activatedRoute: ActivatedRoute) { }

  async ngOnInit(): Promise<void> {

    this.activatedRoute.queryParams.subscribe(async x => {
      this.readRouteParameter(x);
      await this.update();
    });
  }

  readRouteParameter(map: any) {
    this.searchText = map["search"] ? map["search"] : "";
    this.selectedGrouping = map["grouping"] ? map["grouping"] : "Monthly";
  }

  async update() {
    this.groupsShown = [];
    this.showClearButton = this.searchText != "";
    this.isLoading = true;
    const response = await  lastValueFrom(this.transactionPageClient.getTransactions(this.searchText === "" ? undefined : this.searchText));
    this.isLoading = false;
    const blocks: Group[] = [];
    let currentBlock: Group | undefined;
    for (const transaction of response.entries!) {

      const groupId = this.getGroupId(transaction.date!);
      if (currentBlock === undefined || currentBlock.id != groupId) {
        currentBlock = {
          id: groupId,
          total: 0,
          expense: 0,
          income: 0,
          transactions: [],
          title: this.getTitle(transaction.date!)
        };
        blocks.push(currentBlock);
      }

      currentBlock.total += transaction.value!;
      currentBlock.income += transaction.value! > 0 ? transaction.value! : 0;
      currentBlock.expense += transaction.value! < 0 ? -transaction.value! : 0;
      currentBlock.transactions.push(transaction);
    }

    this.groupsHidden = blocks;
    this.groupsShown = [];
    this.showMore();
  }

  showMore() {
    let totalEntriesShown = 0;
    while (this.groupsHidden.length > 0) {
      const toMove = this.groupsHidden.shift()!;
      totalEntriesShown += toMove.transactions.length;
      this.groupsShown.push(toMove);

      if (totalEntriesShown > 1000) {
        break;
      }
    }
  }

  getGroupId(date: Date): string {
    if (this.selectedGrouping === "None") return "0";
    if (this.selectedGrouping === "Monthly") return date.getFullYear().toString() + date.getMonth().toString();
    if (this.selectedGrouping === "Yearly") return date.getFullYear().toString();
    throw Error("Invalid group: " + this.selectedGrouping);
  }

  getTitle(date: Date): string {
    if (this.selectedGrouping === "None") return "Ergebnis";
    if (this.selectedGrouping === "Monthly") return ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"][date.getMonth()] + " " + date.getFullYear().toString();
    if (this.selectedGrouping === "Yearly") return date.getFullYear().toString();
    throw Error("Invalid group: " + this.selectedGrouping);
  }

  async onSearchKeyPressed($event: KeyboardEvent) {
    if ($event.code != "Enter") {
      return;
    }

    this.router.navigate([],
      {
        relativeTo: this.activatedRoute,
        queryParams: {
          search: this.searchText !== undefined && this.searchText.trim() !== "" ? this.searchText : undefined
        }, queryParamsHandling: "merge"
      });
  }

  async onGroupingChanged(event: any) {
    this.router.navigate([],
      {
        relativeTo: this.activatedRoute,
        queryParams: {
          grouping: this.selectedGrouping == "Monthly" ? undefined : this.selectedGrouping
        }, queryParamsHandling: "merge"
      });
  }

  onResetButtonClicked() {
    this.router.navigate(["/", "transactions"]);
  }
}

interface Group {
  id: string;
  title: string;
  transactions: TransactionEntryResponse[];
  total: number;
  income: number;
  expense: number;
}

interface GroupingSelecteItem {
  name: string;
  key: Grouping
}

type Grouping = "None" | "Monthly" | "Yearly"