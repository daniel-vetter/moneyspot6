import { Routes } from '@angular/router';
import { SummaryComponent } from './summary/summary.component';
import { TransactionsComponent } from './transactions/transactions.component';
import { SystemComponent } from './settings/system/system.component';
import { HistoryComponent } from './history/history.component';
import { CashflowComponent } from './cashflow/cashflow.component';
import { StockHistoryComponent } from './stock-history/stock-history.component';
import { StockTransactionsComponent } from "./stock-transactions/stock-transactions.component";
import { StockDetailsComponent } from './stock-transactions/stock-details/stock-details.component';
import { SettingsComponent } from './settings/settings.component';
import { CategoriesComponent } from './categories/categories.component';
import { CategoriesComponent as CategoriesEditorComponent } from './settings/categories/categories.component';
import {RulesComponent} from "./settings/rules/rules.component";
import {MailIntegrationComponent} from "./settings/mail-integration/mail-integration.component";
import {InflationDataComponent} from "./settings/inflation-data/inflation-data.component";
import {BankConnectionsComponent} from "./settings/bank-connections/bank-connections.component";

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
    { path: 'settings/bank-connections', component: BankConnectionsComponent, title: 'MoneySpot - Bankverbindungen' },
    { path: 'settings/system', component: SystemComponent, title: 'MoneySpot - Debug' },
    { path: 'settings/rules', component: RulesComponent, title: 'MoneySpot - Regeln' },
    { path: 'settings/categories', component: CategoriesEditorComponent, title: 'MoneySpot - Kategorien' },
    { path: 'settings/inflation-data', component: InflationDataComponent, title: 'MoneySpot - VPI-Daten' },
    { path: 'settings/mail-integration', component: MailIntegrationComponent, title: 'MoneySpot - Mail Integration' },
];
