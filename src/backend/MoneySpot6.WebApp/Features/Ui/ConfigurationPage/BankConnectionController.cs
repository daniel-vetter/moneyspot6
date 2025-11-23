using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.Ui.ConfigurationPage;

[ApiController]
[Route("api/[controller]")]
public class BankConnectionController : Controller
{
    private readonly Db _db;

    public BankConnectionController(Db db)
    {
        _db = db;
    }

    [HttpGet("GetAll")]
    [ProducesResponseType<ImmutableArray<BankConnectionListResponse>>(200)]
    public async Task<IActionResult> GetAll()
    {
        var connections = await _db.BankConnections
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var response = connections.Select(x => new BankConnectionListResponse
        {
            Id = x.Id,
            Name = x.Name,
            BankCode = x.BankCode,
            UserId = x.UserId,
            LastSuccessfulSync = x.LastSuccessfulSync
        }).ToImmutableArray();

        return Ok(response);
    }

    [HttpGet("Get")]
    [ProducesResponseType<BankConnectionDetailsResponse>(200)]
    public async Task<IActionResult> Get(int id)
    {
        var connection = await _db.BankConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id);

        if (connection == null)
            return NotFound();

        return Ok(new BankConnectionDetailsResponse
        {
            Id = connection.Id,
            Name = connection.Name,
            HbciVersion = connection.HbciVersion,
            BankCode = connection.BankCode,
            CustomerId = connection.CustomerId,
            UserId = connection.UserId,
            Pin = connection.Pin
        });
    }

    [HttpPut("Create")]
    [ProducesResponseType<int>(200)]
    [ProducesResponseType<BankConnectionValidationErrorResponse>(400)]
    public async Task<IActionResult> Create(CreateBankConnectionRequest request)
    {
        var validationError = ValidateRequest(request.Name, request.HbciVersion, request.BankCode,
            request.CustomerId, request.UserId, request.Pin);

        if (validationError != null)
            return BadRequest(validationError);

        // Check if name already exists
        var nameExists = await _db.BankConnections
            .AnyAsync(x => x.Name == request.Name);

        if (nameExists)
        {
            return BadRequest(new BankConnectionValidationErrorResponse
            {
                NameAlreadyExists = true
            });
        }

        var newConnection = new DbBankConnection
        {
            Name = request.Name,
            HbciVersion = request.HbciVersion,
            BankCode = request.BankCode,
            CustomerId = request.CustomerId,
            UserId = request.UserId,
            Pin = request.Pin,
            Passport = null,
            LastSuccessfulSync = null
        };

        _db.BankConnections.Add(newConnection);
        await _db.SaveChangesAsync();

        return Ok(newConnection.Id);
    }

    [HttpPost("Update")]
    [ProducesResponseType(200)]
    [ProducesResponseType<BankConnectionValidationErrorResponse>(400)]
    public async Task<IActionResult> Update(UpdateBankConnectionRequest request)
    {
        var connection = await _db.BankConnections
            .SingleOrDefaultAsync(x => x.Id == request.Id);

        if (connection == null)
            return NotFound();

        var validationError = ValidateRequest(request.Name, request.HbciVersion, request.BankCode,
            request.CustomerId, request.UserId, request.Pin);

        if (validationError != null)
            return BadRequest(validationError);

        // Check if name already exists (excluding current connection)
        var nameExists = await _db.BankConnections
            .AnyAsync(x => x.Name == request.Name && x.Id != request.Id);

        if (nameExists)
        {
            return BadRequest(new BankConnectionValidationErrorResponse
            {
                NameAlreadyExists = true
            });
        }

        connection.Name = request.Name;
        connection.HbciVersion = request.HbciVersion;
        connection.BankCode = request.BankCode;
        connection.CustomerId = request.CustomerId;
        connection.UserId = request.UserId;
        connection.Pin = request.Pin;

        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("Delete")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Delete(int id)
    {
        var connection = await _db.BankConnections
            .SingleOrDefaultAsync(x => x.Id == id);

        if (connection == null)
            return NotFound();

        // Cascade delete: Remove all bank accounts (transactions will cascade from there)
        var bankAccounts = await _db.BankAccounts
            .Where(x => x.BankConnection.Id == id)
            .ToListAsync();

        _db.BankAccounts.RemoveRange(bankAccounts);
        _db.BankConnections.Remove(connection);

        await _db.SaveChangesAsync();

        return Ok();
    }

    private BankConnectionValidationErrorResponse? ValidateRequest(string name, string hbciVersion,
        string bankCode, string customerId, string userId, string pin)
    {
        var error = new BankConnectionValidationErrorResponse();

        if (string.IsNullOrWhiteSpace(name))
            error.MissingName = true;

        if (string.IsNullOrWhiteSpace(hbciVersion))
            error.MissingHbciVersion = true;

        if (string.IsNullOrWhiteSpace(bankCode))
            error.MissingBankCode = true;

        if (string.IsNullOrWhiteSpace(customerId))
            error.MissingCustomerId = true;

        if (string.IsNullOrWhiteSpace(userId))
            error.MissingUserId = true;

        if (string.IsNullOrWhiteSpace(pin))
            error.MissingPin = true;

        return error.HasError() ? error : null;
    }
}

[PublicAPI]
public record BankConnectionListResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required string BankCode { get; init; }
    [Required] public required string UserId { get; init; }
    public DateTimeOffset? LastSuccessfulSync { get; init; }
}

[PublicAPI]
public record BankConnectionDetailsResponse
{
    [Required] public required int Id { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required string HbciVersion { get; init; }
    [Required] public required string BankCode { get; init; }
    [Required] public required string CustomerId { get; init; }
    [Required] public required string UserId { get; init; }
    [Required] public required string Pin { get; init; }
}

[PublicAPI]
public record CreateBankConnectionRequest
{
    [Required] public required string Name { get; init; }
    [Required] public required string HbciVersion { get; init; }
    [Required] public required string BankCode { get; init; }
    [Required] public required string CustomerId { get; init; }
    [Required] public required string UserId { get; init; }
    [Required] public required string Pin { get; init; }
}

[PublicAPI]
public record UpdateBankConnectionRequest
{
    [Required] public required int Id { get; init; }
    [Required] public required string Name { get; init; }
    [Required] public required string HbciVersion { get; init; }
    [Required] public required string BankCode { get; init; }
    [Required] public required string CustomerId { get; init; }
    [Required] public required string UserId { get; init; }
    [Required] public required string Pin { get; init; }
}

[PublicAPI]
public record BankConnectionValidationErrorResponse
{
    public bool MissingName { get; set; }
    public bool MissingHbciVersion { get; set; }
    public bool MissingBankCode { get; set; }
    public bool MissingCustomerId { get; set; }
    public bool MissingUserId { get; set; }
    public bool MissingPin { get; set; }
    public bool NameAlreadyExists { get; set; }

    public bool HasError()
    {
        return MissingName || MissingHbciVersion || MissingBankCode ||
               MissingCustomerId || MissingUserId || MissingPin || NameAlreadyExists;
    }
}
