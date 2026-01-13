using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneySpot6.WebApp.Database;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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

        var response = connections.Select(x =>
        {
            return x.Type switch
            {
                BankConnectionType.FinTS => CreateFinTsListResponse(x),
                BankConnectionType.Demo => new BankConnectionListResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    BankCode = "DEMO",
                    UserId = "DEMO",
                    LastSuccessfulSync = x.LastSuccessfulSync
                },
                _ => throw new NotImplementedException($"Unsupported bank connection type {x.Type} for connection {x.Id}")
            };
        }).ToImmutableArray();

        return Ok(response);
    }

    private static BankConnectionListResponse CreateFinTsListResponse(DbBankConnection x)
    {
        var settings = JsonSerializer.Deserialize<BankConnectionSettingsFinTS>(x.Settings)
                       ?? throw new Exception($"Failed to deserialize settings for connection {x.Id}");

        return new BankConnectionListResponse
        {
            Id = x.Id,
            Name = x.Name,
            BankCode = settings.BankCode,
            UserId = settings.UserId,
            LastSuccessfulSync = x.LastSuccessfulSync
        };
    }

    [HttpGet("Get")]
    [ProducesResponseType<BankConnectionDetailsResponse>(200)]
    public async Task<IActionResult> GetFinTsConnection(int id)
    {
        var connection = await _db.BankConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id);

        if (connection == null)
            return NotFound();

        if (connection.Type != BankConnectionType.FinTS)
            return BadRequest("Connection is not a FinTS connection");

        var settings = JsonSerializer.Deserialize<BankConnectionSettingsFinTS>(connection.Settings) ?? throw new Exception($"Failed to deserialize settings for connection {id}");
        return Ok(new BankConnectionDetailsResponse
        {
            Id = connection.Id,
            Name = connection.Name,
            HbciVersion = settings.HbciVersion,
            BankCode = settings.BankCode,
            CustomerId = settings.CustomerId,
            UserId = settings.UserId,
            Pin = settings.Pin
        });
    }

    [HttpPut("Create")]
    [ProducesResponseType<int>(200)]
    [ProducesResponseType<BankConnectionValidationErrorResponse>(400)]
    public async Task<IActionResult> CreateFinTsConnection(CreateFinTsBankConnectionRequest request)
    {
        var validationError = ValidateRequest(request.Name, request.HbciVersion, request.BankCode,
            request.CustomerId, request.UserId, request.Pin);

        if (validationError != null)
            return BadRequest(validationError);

        if (await _db.BankConnections.AnyAsync(x => x.Name == request.Name))
        {
            return BadRequest(new BankConnectionValidationErrorResponse
            {
                NameAlreadyExists = true
            });
        }

        var settings = new BankConnectionSettingsFinTS
        {
            HbciVersion = request.HbciVersion,
            BankCode = request.BankCode,
            CustomerId = request.CustomerId,
            UserId = request.UserId,
            Pin = request.Pin
        };

        var newConnection = new DbBankConnection
        {
            Name = request.Name,
            Type = BankConnectionType.FinTS,
            Settings = JsonSerializer.Serialize(settings),
            LastSuccessfulSync = null
        };

        _db.BankConnections.Add(newConnection);
        await _db.SaveChangesAsync();

        return Ok(newConnection.Id);
    }

    [HttpPost("Update")]
    [ProducesResponseType(200)]
    [ProducesResponseType<BankConnectionValidationErrorResponse>(400)]
    public async Task<IActionResult> UpdateFinTsConnection(UpdateFinTsBankConnectionRequest request)
    {
        var connection = await _db.BankConnections
            .SingleOrDefaultAsync(x => x.Id == request.Id);

        if (connection == null)
            return NotFound();

        if (connection.Type != BankConnectionType.FinTS)
            return BadRequest("Connection is not a FinTS connection");

        var validationError = ValidateRequest(request.Name, request.HbciVersion, request.BankCode, request.CustomerId, request.UserId, request.Pin);

        if (validationError != null)
            return BadRequest(validationError);

        if (await _db.BankConnections.AnyAsync(x => x.Name == request.Name && x.Id != request.Id))
        {
            return BadRequest(new BankConnectionValidationErrorResponse
            {
                NameAlreadyExists = true
            });
        }

        var settings = new BankConnectionSettingsFinTS
        {
            HbciVersion = request.HbciVersion,
            BankCode = request.BankCode,
            CustomerId = request.CustomerId,
            UserId = request.UserId,
            Pin = request.Pin
        };

        connection.Name = request.Name;
        connection.Settings = JsonSerializer.Serialize(settings);

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
public record CreateFinTsBankConnectionRequest
{
    [Required] public required string Name { get; init; }
    [Required] public required string HbciVersion { get; init; }
    [Required] public required string BankCode { get; init; }
    [Required] public required string CustomerId { get; init; }
    [Required] public required string UserId { get; init; }
    [Required] public required string Pin { get; init; }
}

[PublicAPI]
public record UpdateFinTsBankConnectionRequest
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
