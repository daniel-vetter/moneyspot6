import { Routes } from '@angular/router';
import { SummaryComponent } from './summary/summary.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { DebugComponent } from './debug/debug.component';
import { HistoryComponent } from './history/history.component';
import { IncomeExpenseReportComponent } from './income-expense-report/income-expense-report.component';
import { StockHistoryComponent } from './stock-history/stock-history.component';
import {StockTransactionsComponent} from "./stock-transactions/stock-transactions.component";

export const routes: Routes = [
    { path: '', component: SummaryComponent },
    { path: 'summary', component: SummaryComponent },
    { path: 'transactions', component: TransactionsComponent },
    { path: 'history', component: HistoryComponent },
    { path: 'income-expense', component: IncomeExpenseReportComponent },
    { path: 'stock-transactions', component: StockTransactionsComponent },
    { path: 'stock-history', component: StockHistoryComponent },
    { path: 'debug', component: DebugComponent },
];
