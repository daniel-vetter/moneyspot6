using Microsoft.Playwright;
using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests;

public class TransactionUiTests : UiTest
{
    [Test]
    public async Task Shows_values_from_parsed_data()
    {
        // Arrange: Create bank connection, account and transaction
        var bankConnection = await CreateBankConnection();
        var bankAccount = await CreateBankAccount(bankConnection);
        var transaction = await CreateTransaction(bankAccount);

        // Act: Navigate to transactions page
        await Page.GotoAsync("http://localhost:4200/transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Open the transaction dialog
        await Page.GetByTestId($"transaction-row-{transaction.Id}").ClickAsync();
        await Expect(Page.GetByTestId("transaction-name-input")).ToBeVisibleAsync();

        // Assert: Verify all values from parsed data are shown
        await Expect(Page.GetByTestId("transaction-name-input")).ToHaveValueAsync("Test Name");
        await Expect(Page.GetByTestId("transaction-purpose-input")).ToHaveValueAsync("Test Purpose");
        await Expect(Page.GetByTestId("transaction-amount-input")).ToHaveValueAsync("-50");
        await Expect(Page.GetByTestId("transaction-iban-input")).ToHaveValueAsync("DE987654321");
        await Expect(Page.GetByTestId("transaction-bic-input")).ToHaveValueAsync("TESTBIC");
        await Expect(Page.GetByTestId("transaction-bankcode-input")).ToHaveValueAsync("12345678");
        await Expect(Page.GetByTestId("transaction-accountnumber-input")).ToHaveValueAsync("9876543");
        await Expect(Page.GetByTestId("transaction-endtoend-input")).ToHaveValueAsync("E2E-REF-123");
        await Expect(Page.GetByTestId("transaction-customerref-input")).ToHaveValueAsync("CUST-REF-456");
        await Expect(Page.GetByTestId("transaction-mandate-input")).ToHaveValueAsync("MANDATE-789");
        await Expect(Page.GetByTestId("transaction-creditor-input")).ToHaveValueAsync("CREDITOR-ID");
        await Expect(Page.GetByTestId("transaction-originator-input")).ToHaveValueAsync("ORIGINATOR-ID");

        // Assert: No reset buttons should be visible (no overrides)
        await Expect(Page.GetByTestId("transaction-name-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-purpose-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-amount-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-iban-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-bic-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-bankcode-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-accountnumber-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-endtoend-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-customerref-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-mandate-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-creditor-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-originator-reset")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Shows_values_from_processed_data()
    {
        // Arrange: Create bank connection, account and transaction with processed values
        var bankConnection = await CreateBankConnection();
        var bankAccount = await CreateBankAccount(bankConnection);
        var transaction = await CreateTransaction(bankAccount, withProcessedData: true);

        // Act: Navigate to transactions page
        await Page.GotoAsync("http://localhost:4200/transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Open the transaction dialog
        await Page.GetByTestId($"transaction-row-{transaction.Id}").ClickAsync();
        await Expect(Page.GetByTestId("transaction-name-input")).ToBeVisibleAsync();

        // Assert: Verify all values from processed data are shown (overriding parsed data)
        await Expect(Page.GetByTestId("transaction-name-input")).ToHaveValueAsync("Processed Name");
        await Expect(Page.GetByTestId("transaction-purpose-input")).ToHaveValueAsync("Processed Purpose");
        await Expect(Page.GetByTestId("transaction-amount-input")).ToHaveValueAsync("-100");
        await Expect(Page.GetByTestId("transaction-iban-input")).ToHaveValueAsync("DE-PROCESSED-IBAN");
        await Expect(Page.GetByTestId("transaction-bic-input")).ToHaveValueAsync("PROCBIC");
        await Expect(Page.GetByTestId("transaction-bankcode-input")).ToHaveValueAsync("99999999");
        await Expect(Page.GetByTestId("transaction-accountnumber-input")).ToHaveValueAsync("1111111");
        await Expect(Page.GetByTestId("transaction-endtoend-input")).ToHaveValueAsync("PROC-E2E");
        await Expect(Page.GetByTestId("transaction-customerref-input")).ToHaveValueAsync("PROC-CUST");
        await Expect(Page.GetByTestId("transaction-mandate-input")).ToHaveValueAsync("PROC-MANDATE");
        await Expect(Page.GetByTestId("transaction-creditor-input")).ToHaveValueAsync("PROC-CREDITOR");
        await Expect(Page.GetByTestId("transaction-originator-input")).ToHaveValueAsync("PROC-ORIGINATOR");

        // Assert: No reset buttons should be visible (no user overrides)
        await Expect(Page.GetByTestId("transaction-name-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-purpose-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-amount-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-iban-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-bic-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-bankcode-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-accountnumber-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-endtoend-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-customerref-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-mandate-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-creditor-reset")).Not.ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-originator-reset")).Not.ToBeVisibleAsync();
    }

    [Test]
    public async Task Shows_values_from_overridden_data()
    {
        // Arrange: Create bank connection, account and transaction with overridden values
        var bankConnection = await CreateBankConnection();
        var bankAccount = await CreateBankAccount(bankConnection);
        var transaction = await CreateTransaction(bankAccount, withOverriddenData: true);

        // Act: Navigate to transactions page
        await Page.GotoAsync("http://localhost:4200/transactions");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Open the transaction dialog
        await Page.GetByTestId($"transaction-row-{transaction.Id}").ClickAsync();
        await Expect(Page.GetByTestId("transaction-name-input")).ToBeVisibleAsync();

        // Assert: Verify all values from overridden data are shown
        await Expect(Page.GetByTestId("transaction-name-input")).ToHaveValueAsync("Overridden Name");
        await Expect(Page.GetByTestId("transaction-purpose-input")).ToHaveValueAsync("Overridden Purpose");
        await Expect(Page.GetByTestId("transaction-amount-input")).ToHaveValueAsync("-200");
        await Expect(Page.GetByTestId("transaction-iban-input")).ToHaveValueAsync("DE-OVERRIDDEN-IBAN");
        await Expect(Page.GetByTestId("transaction-bic-input")).ToHaveValueAsync("OVERRBIC");
        await Expect(Page.GetByTestId("transaction-bankcode-input")).ToHaveValueAsync("88888888");
        await Expect(Page.GetByTestId("transaction-accountnumber-input")).ToHaveValueAsync("3333333");
        await Expect(Page.GetByTestId("transaction-endtoend-input")).ToHaveValueAsync("OVERR-E2E");
        await Expect(Page.GetByTestId("transaction-customerref-input")).ToHaveValueAsync("OVERR-CUST");
        await Expect(Page.GetByTestId("transaction-mandate-input")).ToHaveValueAsync("OVERR-MANDATE");
        await Expect(Page.GetByTestId("transaction-creditor-input")).ToHaveValueAsync("OVERR-CREDITOR");
        await Expect(Page.GetByTestId("transaction-originator-input")).ToHaveValueAsync("OVERR-ORIGINATOR");

        // Assert: All reset buttons should be visible (user overrides exist)
        await Expect(Page.GetByTestId("transaction-name-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-purpose-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-amount-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-iban-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-bic-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-bankcode-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-accountnumber-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-endtoend-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-customerref-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-mandate-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-creditor-reset")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("transaction-originator-reset")).ToBeVisibleAsync();
    }

    private async Task<DbBankConnection> CreateBankConnection()
    {
        var connection = new DbBankConnection
        {
            Name = "Test Connection",
            HbciVersion = "300",
            BankCode = "12345678",
            CustomerId = "customer",
            UserId = "user",
            Pin = "1234"
        };
        _db.BankConnections.Add(connection);
        await _db.SaveChangesAsync();
        return connection;
    }

    private async Task<DbBankAccount> CreateBankAccount(DbBankConnection bankConnection)
    {
        var account = new DbBankAccount
        {
            BankConnection = bankConnection,
            Icon = null,
            IconColor = null,
            Name = "Test Account",
            Name2 = null,
            Country = "DE",
            Currency = "EUR",
            Bic = "TESTBIC",
            Iban = "DE123456789",
            BankCode = "12345678",
            AccountNumber = "123456789",
            CustomerId = "customer",
            AccountType = "Girokonto",
            Type = "checking",
            Balance = 1000m
        };
        _db.BankAccounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    private async Task<DbBankAccountTransaction> CreateTransaction(
        DbBankAccount bankAccount,
        bool withProcessedData = false,
        bool withOverriddenData = false)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var processed = new DbBankAccountTransactionProcessedData();
        if (withProcessedData || withOverriddenData)
        {
            processed = new DbBankAccountTransactionProcessedData
            {
                Date = today,
                Name = "Processed Name",
                Purpose = "Processed Purpose",
                Amount = -100m,
                BankCode = "99999999",
                AccountNumber = "1111111",
                Iban = "DE-PROCESSED-IBAN",
                Bic = "PROCBIC",
                EndToEndReference = "PROC-E2E",
                CustomerReference = "PROC-CUST",
                MandateReference = "PROC-MANDATE",
                CreditorIdentifier = "PROC-CREDITOR",
                OriginatorIdentifier = "PROC-ORIGINATOR"
            };
        }

        var overridden = new DbBankAccountTransactionOverrideData();
        if (withOverriddenData)
        {
            overridden = new DbBankAccountTransactionOverrideData
            {
                Date = today,
                Name = "Overridden Name",
                Purpose = "Overridden Purpose",
                Amount = -200m,
                BankCode = "88888888",
                AccountNumber = "3333333",
                Iban = "DE-OVERRIDDEN-IBAN",
                Bic = "OVERRBIC",
                EndToEndReference = "OVERR-E2E",
                CustomerReference = "OVERR-CUST",
                MandateReference = "OVERR-MANDATE",
                CreditorIdentifier = "OVERR-CREDITOR",
                OriginatorIdentifier = "OVERR-ORIGINATOR"
            };
        }

        var parsed = new DbBankAccountTransactionParsedData
        {
            Date = today,
            Name = "Test Name",
            Purpose = "Test Purpose",
            Amount = -50m,
            BankCode = "12345678",
            AccountNumber = "9876543",
            Iban = "DE987654321",
            Bic = "TESTBIC",
            EndToEndReference = "E2E-REF-123",
            CustomerReference = "CUST-REF-456",
            MandateReference = "MANDATE-789",
            CreditorIdentifier = "CREDITOR-ID",
            OriginatorIdentifier = "ORIGINATOR-ID",
            AlternateInitiator = "",
            AlternateReceiver = "",
            PaymentProcessor = PaymentProcessor.None
        };

        // Final data reflects the highest priority: Overridden > Processed > Parsed
        var final = new DbBankAccountTransactionFinalData
        {
            Date = overridden.Date ?? processed.Date ?? parsed.Date,
            Name = overridden.Name ?? processed.Name ?? parsed.Name,
            Purpose = overridden.Purpose ?? processed.Purpose ?? parsed.Purpose,
            Amount = overridden.Amount ?? processed.Amount ?? parsed.Amount,
            BankCode = overridden.BankCode ?? processed.BankCode ?? parsed.BankCode,
            AccountNumber = overridden.AccountNumber ?? processed.AccountNumber ?? parsed.AccountNumber,
            Iban = overridden.Iban ?? processed.Iban ?? parsed.Iban,
            Bic = overridden.Bic ?? processed.Bic ?? parsed.Bic,
            EndToEndReference = overridden.EndToEndReference ?? processed.EndToEndReference ?? parsed.EndToEndReference,
            CustomerReference = overridden.CustomerReference ?? processed.CustomerReference ?? parsed.CustomerReference,
            MandateReference = overridden.MandateReference ?? processed.MandateReference ?? parsed.MandateReference,
            CreditorIdentifier = overridden.CreditorIdentifier ?? processed.CreditorIdentifier ?? parsed.CreditorIdentifier,
            OriginatorIdentifier = overridden.OriginatorIdentifier ?? processed.OriginatorIdentifier ?? parsed.OriginatorIdentifier
        };

        var transaction = new DbBankAccountTransaction
        {
            Source = "test",
            BankAccount = bankAccount,
            Raw = new DbBankAccountTransactionRawData
            {
                Date = today,
                Counterparty = new CounterpartyAccount
                {
                    Name = "Raw Name",
                    Iban = "DE-RAW-IBAN"
                },
                Purpose = "Raw Purpose",
                Amount = -50m,
                NewBalance = 1000m
            },
            Parsed = parsed,
            Processed = processed,
            Overridden = overridden,
            Final = final,
            Note = "",
            IsNew = false
        };
        _db.BankAccountTransactions.Add(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }
}
