import { Routes } from '@angular/router';
import { SummaryComponent } from './summary/summary.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { DebugComponent } from './debug/debug.component';
import { HistoryComponent } from './history/history.component';
import { CashflowComponent } from './cashflow/cashflow.component';
import { StockHistoryComponent } from './stock-history/stock-history.component';
import { StockTransactionsComponent } from "./stock-transactions/stock-transactions.component";
import { StockDetailsComponent } from './stock-transactions/stock-details/stock-details.component';
import { SettingsComponent } from './settings/settings.component';
import { CategoriesComponent } from './categories/categories.component';

export const routes: Routes = [
    { path: '', component: SummaryComponent, title: 'MoneySpot - Übersicht' },
    { path: 'summary', component: SummaryComponent, title: 'MoneySpot - Übersicht' },
    { path: 'transactions', component: TransactionsComponent, title: 'MoneySpot - Transaktionen' },
    { path: 'history', component: HistoryComponent, title: 'MoneySpot - Trends' },
    { path: 'cashflow', component: CashflowComponent, title: 'MoneySpot - Cashflow' },
    { path: 'categories', component: CategoriesComponent, title: 'MoneySpot - Kategorien' },
    { path: 'stock-transactions', component: StockTransactionsComponent, title: 'MoneySpot - Aktien-Trades' },
    { path: 'stock-transactions/orders/:id', component: StockDetailsComponent, title: 'MoneySpot - Aktien-Trades' },
    { path: 'stock-history', component: StockHistoryComponent, title: 'MoneySpot - Kursverlauf' },
    { path: 'settings', component: SettingsComponent, title: 'MoneySpot - Einstellungen' },
    { path: 'debug', component: DebugComponent, title: 'MoneySpot - Debug' },
];
