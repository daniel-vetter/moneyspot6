using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace MoneySpot6.WebApp.Features.Ui.MailIntegrationPage
{
    [ApiController]
    [Route("api/[controller]")]
    public class MailIntegrationController : Controller
    {
        private IOptions<MailIntegrationOptions> _configuration;
        private readonly Db _db;

        public MailIntegrationController(IOptions<MailIntegrationOptions> configuration, Db db)
        {
            _configuration = configuration;
            _db = db;
        }

        [HttpGet("GetStatus")]
        public async Task<IntegrationStatusResponse> GetStatusAsync()
        {
            string? gmailLoginUrl = null;
            if (!string.IsNullOrWhiteSpace(_configuration.Value.GmailClientId) && !string.IsNullOrWhiteSpace(_configuration.Value.GmailClientSecret))
            {
                gmailLoginUrl = QueryHelpers.AddQueryString("https://accounts.google.com/o/oauth2/v2/auth", new Dictionary<string, string?>
                {
                    ["client_id"] = _configuration.Value.GmailClientId,
                    ["redirect_uri"] = Url.Link(null, new { controller = "MailIntegration", action = "GMailAuthorize" }),
                    ["response_type"] = "code",
                    ["scope"] = "https://www.googleapis.com/auth/gmail.readonly",
                    ["access_type"] = "offline",
                    ["prompt"] = "consent"
                });
            }

            var existingAccounts = await _db
                .Set<DbGMailIntegration>()
                .Select(x => x.Name)
                .ToArrayAsync();

            return new IntegrationStatusResponse
            {
                GmailLoginUrl = gmailLoginUrl,
                ConnectedAccounts = existingAccounts
            };
        }

        [HttpGet("GMailAuthorize")]
        public async Task<IActionResult> GMailAuthorize(string code)
        {

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _configuration.Value.GmailClientId,
                    ClientSecret = _configuration.Value.GmailClientSecret
                },
                Scopes = [GmailService.Scope.GmailReadonly],
            });

            var token = await flow.ExchangeCodeForTokenAsync(
                userId: "1",
                code: code,
                redirectUri: Url.Link(null, new { controller = "MailIntegration", action = "GMailAuthorize" }),
                taskCancellationToken: CancellationToken.None);

            var credential = new UserCredential(flow, "1", token);

            var gmail = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "MoneySpot6"
            });

            var user = await gmail.Users.GetProfile("me").ExecuteAsync();

            var dbEntry = await _db
                .Set<DbGMailIntegration>()
                .AsTracking()
                .SingleOrDefaultAsync(x => x.Name == user.EmailAddress);

            if (dbEntry == null)
            {
                dbEntry = new DbGMailIntegration
                {
                    Name = user.EmailAddress,
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken!,
                    ExpiresAt = token.IssuedUtc.AddSeconds(token.ExpiresInSeconds ?? throw new Exception("'ExpiresInSeconds' was null."))
                };
                _db.Add(dbEntry);
            }
            else
            {
                dbEntry.AccessToken = token.AccessToken;
                dbEntry.RefreshToken = token.RefreshToken!;
                dbEntry.ExpiresAt = token.IssuedUtc.AddSeconds(token.ExpiresInSeconds ?? throw new Exception("'ExpiresInSeconds' was null."));
            }

            await _db.SaveChangesAsync();

            return Redirect("/settings/mail-integration");
        }

        [HttpPost("DisconnectGMailAccount")]
        public async Task<IActionResult> DisconnectGMailAccount([Required] string accountName)
        {
            var dbEntry = await _db
                .Set<DbGMailIntegration>()
                .AsTracking()
                .SingleOrDefaultAsync(x => x.Name == accountName);

            if (dbEntry != null)
            {
                _db.Remove(dbEntry);
                await _db.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost("CreateMonitoredAddress")]
        [ProducesResponseType<int>(200)]
        [ProducesResponseType<CreateMonitoredAddressValidationErrorResponse>(400)]
        public async Task<IActionResult> CreateMonitoredAddress(CreateMonitoredAddressRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return BadRequest(new CreateMonitoredAddressValidationErrorResponse
                {
                    MissingAddress = true
                });
            }

            var trimmedAddress = request.Address.Trim();
            var existing = await _db
                .Set<DbMonitoredEmailAddress>()
                .Where(x => x.EmailAddress == trimmedAddress)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return BadRequest(new CreateMonitoredAddressValidationErrorResponse
                {
                    AlreadyConfigured = true
                });
            }

            _db.Set<DbMonitoredEmailAddress>().Add(new DbMonitoredEmailAddress
            {
                EmailAddress = trimmedAddress,
                Prompt = request.Prompt
            });

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("GetAllMonitoredAddresses")]
        [ProducesResponseType<MonitoredAddressResponse[]>(200)]
        public async Task<IActionResult> GetAllMonitoredAddresses()
        {
            var result = await _db
                .Set<DbMonitoredEmailAddress>()
                .OrderBy(x => x.EmailAddress)
                .Select(x => new MonitoredAddressResponse
                {
                    Id = x.Id,
                    Address = x.EmailAddress,
                    Prompt = x.Prompt
                }).ToImmutableArrayAsync();

            return Ok(result);
        }

        [HttpPost("UpdateMonitoredAddress")]
        [ProducesResponseType<int>(200)]
        [ProducesResponseType<UpdateMonitoredAddressValidationErrorResponse>(400)]
        public async Task<IActionResult> UpdateMonitoredAddress(UpdateMonitoredAddressRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return BadRequest(new UpdateMonitoredAddressValidationErrorResponse
                {
                    MissingAddress = true
                });
            }

            var trimmedAddress = request.Address.Trim();
            var otherExisting = await _db
                .Set<DbMonitoredEmailAddress>()
                .AsNoTracking()
                .Where(x => x.EmailAddress == trimmedAddress && x.Id != request.Id)
                .FirstOrDefaultAsync();

            if (otherExisting != null)
            {
                return BadRequest(new UpdateMonitoredAddressValidationErrorResponse
                {
                    AlreadyConfigured = true
                });
            }

            var existing = await _db
                .Set<DbMonitoredEmailAddress>()
                .AsTracking()
                .Where(x => x.Id == request.Id)
                .SingleOrDefaultAsync();

            if (existing == null)
                return NotFound();

            existing.EmailAddress = request.Address;
            existing.Prompt = request.Prompt;
            
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("DeleteMonitoredAddress")]
        [ProducesResponseType<int>(200)]
        public async Task<IActionResult> DeleteMonitoredAddress(int id)
        {
            var existing = await _db
                .Set<DbMonitoredEmailAddress>()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (existing != null)
                _db.Set<DbMonitoredEmailAddress>().Remove(existing!);

            await _db.SaveChangesAsync();
            return Ok();
        }
    }

    public class CreateMonitoredAddressRequest
    {
        [Required] public required string Address { get; init; }
        [Required] public required string Prompt { get; init; }
    }

    public class UpdateMonitoredAddressRequest
    {
        [Required] public required int Id { get; init; }
        [Required] public required string Address { get; init; }
        [Required] public required string Prompt { get; init; }
    }

    public class MonitoredAddressResponse
    {
        [Required] public required int Id { get; init; }
        [Required] public required string Address { get; init; }
        [Required] public required string Prompt { get; init; }
    }

    public class CreateMonitoredAddressValidationErrorResponse
    {
        public bool MissingAddress { get; init; }
        public bool AlreadyConfigured { get; init; }
    }

    public class UpdateMonitoredAddressValidationErrorResponse
    {
        public bool MissingAddress { get; init; }
        public bool AlreadyConfigured { get; init; }
    }

    public class DisconnectGMailAccountRequest
    {
        [Required] public required string AccountName { get; init; }
    }

    public record IntegrationStatusResponse
    {
        public string? GmailLoginUrl { get; init; }
        [Required] public string[] ConnectedAccounts { get; init; } = [];
    };
}
