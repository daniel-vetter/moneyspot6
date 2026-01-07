using MoneySpot6.WebApp.Database;

namespace MoneySpot6.WebApp.Tests.Features.RawDataParsing;

public class ParsedDataBuilder
{
    private DateOnly _date = new(2020, 1, 1);
    private string? _name;
    private string? _bankCode;
    private string? _accountNumber;
    private string? _purpose;
    private string? _iban;
    private string? _bic;
    private decimal _amount;
    private string? _endToEndReference;
    private string? _customerReference;
    private string? _mandateReference;
    private string? _creditorIdentifier;
    private string? _originatorIdentifier;
    private string? _alternateInitiator;
    private string? _alternateReceiver;
    private PaymentProcessor _paymentProcessor;
    private TransactionType _transactionType;

    public ParsedDataBuilder WithDate(DateOnly date)
    {
        _date = date;
        return this;
    }

    public ParsedDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ParsedDataBuilder WithBankCode(string bankCode)
    {
        _bankCode = bankCode;
        return this;
    }

    public ParsedDataBuilder WithAccountNumber(string accountNumber)
    {
        _accountNumber = accountNumber;
        return this;
    }

    public ParsedDataBuilder WithPurpose(string purpose)
    {
        _purpose = purpose;
        return this;
    }

    public ParsedDataBuilder WithIban(string iban)
    {
        _iban = iban;
        return this;
    }

    public ParsedDataBuilder WithBic(string bic)
    {
        _bic = bic;
        return this;
    }

    public ParsedDataBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public ParsedDataBuilder WithEndToEndReference(string endToEndReference)
    {
        _endToEndReference = endToEndReference;
        return this;
    }

    public ParsedDataBuilder WithCustomerReference(string customerReference)
    {
        _customerReference = customerReference;
        return this;
    }

    public ParsedDataBuilder WithMandateReference(string mandateReference)
    {
        _mandateReference = mandateReference;
        return this;
    }

    public ParsedDataBuilder WithCreditorIdentifier(string creditorIdentifier)
    {
        _creditorIdentifier = creditorIdentifier;
        return this;
    }

    public ParsedDataBuilder WithOriginatorIdentifier(string originatorIdentifier)
    {
        _originatorIdentifier = originatorIdentifier;
        return this;
    }

    public ParsedDataBuilder WithAlternateInitiator(string alternateInitiator)
    {
        _alternateInitiator = alternateInitiator;
        return this;
    }

    public ParsedDataBuilder WithAlternateReceiver(string alternateReceiver)
    {
        _alternateReceiver = alternateReceiver;
        return this;
    }

    public ParsedDataBuilder WithPaymentProcessor(PaymentProcessor paymentProcessor)
    {
        _paymentProcessor = paymentProcessor;
        return this;
    }

    public ParsedDataBuilder WithTransactionType(TransactionType transactionType)
    {
        _transactionType = transactionType;
        return this;
    }

    public DbBankAccountTransactionParsedData Build()
    {
        return new DbBankAccountTransactionParsedData
        {
            Date = _date,
            Name = _name ?? "",
            BankCode = _bankCode ?? "",
            AccountNumber = _accountNumber ?? "",
            Purpose = _purpose ?? "",
            Iban = _iban ?? "",
            Bic = _bic ?? "",
            Amount = _amount,
            EndToEndReference = _endToEndReference ?? "",
            CustomerReference = _customerReference ?? "",
            MandateReference = _mandateReference ?? "",
            CreditorIdentifier = _creditorIdentifier ?? "",
            OriginatorIdentifier = _originatorIdentifier ?? "",
            AlternateInitiator = _alternateInitiator ?? "",
            AlternateReceiver = _alternateReceiver ?? "",
            PaymentProcessor = _paymentProcessor,
            TransactionType = _transactionType
        };
    }
}