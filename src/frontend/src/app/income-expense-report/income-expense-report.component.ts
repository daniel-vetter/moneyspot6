import { Component, OnInit } from '@angular/core';
import { IncomeExpenseClient, IncomeExpenseEntryResponse, IncomeExpenseGrouping } from '../server';
import { lastValueFrom } from 'rxjs';
import { ValueComponent } from "../common/value/value.component";
import { PanelModule } from 'primeng/panel';
import { RippleModule } from 'primeng/ripple';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';

@Component({
  selector: 'app-income-expense-report',
  standalone: true,
  imports: [ValueComponent, PanelModule, RippleModule, FormsModule, InputTextModule, FormsModule],
  templateUrl: './income-expense-report.component.html',
  styleUrl: './income-expense-report.component.scss'
})
export class IncomeExpenseReportComponent implements OnInit {
  onSearchSubmit() {

  }
  lines: Line[] = [];
  blocks: Block[] = [];
  searchText = "";

  constructor(private incomeExpenseClient: IncomeExpenseClient) { }

  async ngOnInit(): Promise<void> {
    const response = (await lastValueFrom(this.incomeExpenseClient.get(IncomeExpenseGrouping.Month))).reverse();
    const blocks: Block[] = [];


    let currentBlock: Block | undefined;
    for (const entry of response) {
      const groupId = this.getGroupId(entry);
      if (currentBlock === undefined || currentBlock.id != groupId) {
        currentBlock = {
          id: groupId,
          name: groupId,
          lines: []
        };
        blocks.push(currentBlock);
      }

      currentBlock.lines.push({
        id: entry.year * 12 + (entry.month ?? 0),
        name: this.getName(entry.year, entry.month),
        expense: entry.expense,
        income: entry.income,
        total: entry.income - entry.expense,
        bar: <any>{}!
      });
    }

    for (const block of blocks) {
      this.calsBars(block.lines);
    }
    this.blocks = blocks;
  }

  calsBars(lines: Line[]) {
    let max = 0;
    for (const line of lines) {
      max = Math.max(max, Math.abs(line.total));
    }
    max *= 1.2;

    for (const line of lines) {
      if (line.total > 0) {
        const percent = (line.total / max * 100) / 2;
        line.bar = {
          color: "var(--green-600)",
          left: 50,
          width: percent,
          radius: "0 0.5rem 0.5rem 0"
        }
      } else {
        const percent = (-line.total / max * 100) / 2;
        line.bar = {
          color: "var(--red-600)",
          left: 50 - percent,
          width: percent,
          radius: "0.5rem 0 0 0.5rem"
        }
      }
    }
  }

  getGroupId(entry: IncomeExpenseEntryResponse) {
    return entry.year.toString();
  }

  private getName(year: number, month?: number) {
    if (month === undefined || month === null) {
      return year.toString();
    }
    return ["Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"][month - 1] + " " + year;
  }
}

interface Line {
  id: number
  name: string;
  income: number;
  expense: number;
  total: number;
  bar: Bar;
}

interface Bar {
  left: number;
  width: number;
  color: string;
  radius: string;
}

interface Block {
  id: string;
  name: string;
  lines: Line[];
}