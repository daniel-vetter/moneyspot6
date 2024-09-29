import { Routes } from '@angular/router';
import { SummaryComponent } from './summary/summary.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { DebugComponent } from './debug/debug.component';
import { HistoryComponent } from './history/history.component';

export const routes: Routes = [
    { path: '', component: SummaryComponent },
    { path: 'summary', component: SummaryComponent },
    { path: 'transactions', component: TransactionsComponent},
    { path: 'history', component: HistoryComponent},
    { path: 'debug', component: DebugComponent}
];
