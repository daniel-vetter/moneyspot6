using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MoneySpot6.WebApp.Database;
using MoneySpot6.WebApp.Infrastructure;
using System.Collections.Immutable;

namespace MoneySpot6.WebApp.Features.Core.MailIntegration
{
    [ScopedService]
    public class MailAccountProvider
    {
        private readonly Db _db;
        private readonly IOptions<MailIntegrationOptions> _configuration;

        public MailAccountProvider(Db db, IOptions<MailIntegrationOptions> configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<ImmutableArray<GMailAccountInfo>> GetConfiguredAccounts()
        {
            return await _db
                .Set<DbGMailIntegration>()
                .AsNoTracking()
                .Select(x => new GMailAccountInfo { Id = x.Id, EmailAddress = x.Name })
                .ToImmutableArrayAsync();
        }

        public async Task<GmailService> GetClient(GMailAccountInfo accountInfo)
        {
            var account = await _db
                .Set<DbGMailIntegration>()
                .AsNoTracking()
                .SingleAsync(x => x.Id == accountInfo.Id);

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _configuration.Value.GmailClientId,
                    ClientSecret = _configuration.Value.GmailClientSecret
                },
                Scopes = [GmailService.Scope.GmailReadonly],
            });

            var credential = new UserCredential(flow, "1", new TokenResponse
            {
                RefreshToken = account.RefreshToken,
                AccessToken = account.AccessToken
            });

            var client = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "MoneySpot",
            });

            return client;
        }
    }

    public record GMailAccountInfo
    {
        public required int Id { get; init; }
        public required string EmailAddress { get; init; }
    }
}
