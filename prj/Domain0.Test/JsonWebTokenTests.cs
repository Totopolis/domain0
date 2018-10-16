using Autofac;
using Domain0.Service;
using System;
using Xunit;
using Sdl.Domain0.Shared;
using Newtonsoft.Json;

namespace Domain0.Test
{
    public class JsonWebTokenTests
    {
        [Theory]
        [InlineData(JwtHashAlgorithm.HS256, "kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs=")]
        [InlineData(JwtHashAlgorithm.RS256, "PFJTQUtleVZhbHVlPjxNb2R1bHVzPng1Y2tLak5NQzUvVTRETnZ2ZEhTRnkzc1ZoUDgxRmZrZ1lmZ3o5RjRXK05mUVpXK3k2Mk43NmZwRjRMOGh0cVNoSjFRcjJuRUtFNjdPZ2V0R3FjZWJ6dlppaXU3UXlpTFlzN1VJK2sxUGhiN1B1WDFTNUFBelpkY2tRSDdiNXZiV2lXVmdyUG9xYjM3M1BnRTJDai8vNFZmYUR4TEhWOThBbCtXbTV5VEd3WjhPWk45MVFSSmdmQkVseXB0ZEw1MDVRbkVzZGQ3aEZUU0VkUTdlUDRaOE1JK1REaVk5VU10WFh4cU1haUY0UWw1RFR5bUMvUWpiSi9aaVZLbXZXeCtFRkZFSHdUTFZYSHNxZUp5bTg5SzNma3Zvdm11NHNhOGN3ZUovWlRwUnJDM0R0M2YrZnd0NFFBRTNEOGxZNjBmNjhEQkdteWNQVDVtRDliUzlYYWxoUT09PC9Nb2R1bHVzPjxFeHBvbmVudD5BUUFCPC9FeHBvbmVudD48UD55Z1lHR00zcFZzemhMWTh1WEZ6cFh5TERxbWFVQVI0aU5nL09yVnNWWkZ5TGVVWnFzQ21WUCsyQjZMcDN0MzNYUS8wc3FISzNmK2p4SlRCRVY0L1lKNi9xTFk1Wi93ekozT0RPZUQ2SFJUMlBOSGFGNEQxem9TUDI4UGx1YjJDZm5yKzNoMTEzQndWZnFiM3ZDVmhTUStjNVpLbWZCdExZL2RmRHg4SGlNcTg9PC9QPjxRPi9PcXlQTnBPL0FST2NMT0crR01IaXVPNDRaZGR6VXE2RndxY3VXSUhITDZ0QkJkQ0tuNFZwTUt6d0dHRlZJWmg1YUlhaml3N1NqYjJwOGRCclMwRUxVNkU0MW5kRHBNVEtLYjNLUnhpTFhkelFTVTdWNStRS1NQSnB2YmhuUVJnT3hQWERwMmY0anhoOWpuYVNudDRkTDNvNndkWUhoS0xVVlNRMURHbkNBcz08L1E+PERQPm1zdVMwRjYyZERQNE5oaUh4VzNMdzRHM3UxRnVCbzA0V1lRek1OR2h5b3krc2VZcXlXQ1RZN0J3NGdvK3dQSkRoMnp1VXdQVFpzYnVQemlFcFRNcXhQNGR5VnBSeXdQWmlNMFlaenBDRytQWFhyT1NVUWZGR2F1ZEEzZWNEdXRTWXlrelR4MW1ucEtYZ2xCdVlCSzB0aUx0N3h1cEptbFlxdWd3czFiMEl6RT08L0RQPjxEUT55MWFkNmxMU0FjZ3NrR3VsN2ZrZ1RVZjhrbEt4OWFWSXE5RzZZMGt1MHF0eVNzR3dUcDJFSlN6c2U1VnNMcUxEL20wdjBISTdVTldUeFJ4cjd5RXNKSWptU2lzcmtOWXFKeHJseDhXc0lVWFNBZVEvSzVsN3U3ZXNIbktLdlVTUllhMzN6eVpuTHVyQkQ4Yy9lM1o5Ujg2UGZyWXU0QzZrbDhUWGsrSS9talU9PC9EUT48SW52ZXJzZVE+V1liRmtGdGFhbm1mZUxmNkVOSVZpcVlRTGdyTmRIZHB4TXlYWXdDbi9tVXlzbzZiTktVUmhtT2VpL0poYVErZzhkWks3emRpdEdPelZSYU91S0Q3VUJZeGIzRHJTdFA4NVZRZm5BdHQzd1JKN0hrU0p1bjFKZnc3VjJNMEtlU29LNnJIUG9sUVV2N1h2OUJnUUJvSGhVRWRlb0tGUkYxM2NoQmZFRTVySkNJPTwvSW52ZXJzZVE+PEQ+aHN1UHRDU0MxbEx5clJ4ditnM0x5clhNS0hKRlRZK3lscnlTMnlmSUZwN2Z4V2FCdmdNUG1leVg0clluSUZoYm5jNjFJRkxaRkxQZmhKaU1relNNdkdqNlNYT3hlL0RVK2oxZlRvV0EzTmNlaGVNK24vSzRhQ0V5ZGdpVnJGSGhlZWRxS3lTZ0hJdGZuMk90dWVNdXlYNWs2ZnZXYXhjQ3BJTzBMcEkzVTRjQmZCdnU3Um92R0Q1ejF4UmlSREdRK1M3OVBabi9LRTlqMWNmcDdKMjIzM2l5WVNqbHRIZ0ZsV2owbkZMNzhXS0tVbVB4QmZoa1BBa05EQ2Jjdm02ZUhpTGJuY2FqdWcrL2JlMXljVmNKZnRqcTJGZU5Jc24vVGJVVFlsekFwSGdUWWF2eCtJRVVzRXV1VGhFNEgrbXpBQ0dmeks1MWxGS0pKTXZicEtTWWZRPT08L0Q+PC9SU0FLZXlWYWx1ZT4=")]
        public void AccessToken_Basic(JwtHashAlgorithm alg, string secretBase64)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
                {
                    builder.RegisterType<SymmetricKeyTokenGenerator>()
                        .Keyed<ITokenGenerator>(JwtHashAlgorithm.HS256)
                        .SingleInstance();
                    builder.RegisterType<AsymmetricKeyPairTokenGenerator>()
                        .Keyed<ITokenGenerator>(JwtHashAlgorithm.RS256)
                        .SingleInstance();
                });
            var tokenGenerator = container.ResolveKeyed<ITokenGenerator>(alg);
            var userId = 165;
            var secret = Convert.FromBase64String(secretBase64);

            var permissions = new [] {"test1","test2"};

            var issueTime = DateTime.UtcNow;
            var accessToken = new JsonWebToken().Encode(new
            {
                typ = "access_token",
                sub = $"{userId}",
                permissions = JsonConvert.SerializeObject(permissions),
                exp = new DateTimeOffset(issueTime.AddMinutes(15)).ToUnixTimeSeconds(),
                iat = new DateTimeOffset(issueTime).ToUnixTimeSeconds(),
                iss = "issuer",
                aud = "*",
            }, secret, alg);

            var accessToken2 = tokenGenerator.GenerateAccessToken(userId, issueTime, permissions);
            Assert.Equal(accessToken, accessToken2);

            var principal = tokenGenerator.Parse(accessToken);
            Assert.Equal(userId.ToString(), principal.Identity.Name);
        }

        [Theory]
        [InlineData(JwtHashAlgorithm.HS256, "kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs=")]
        [InlineData(JwtHashAlgorithm.RS256, "PFJTQUtleVZhbHVlPjxNb2R1bHVzPng1Y2tLak5NQzUvVTRETnZ2ZEhTRnkzc1ZoUDgxRmZrZ1lmZ3o5RjRXK05mUVpXK3k2Mk43NmZwRjRMOGh0cVNoSjFRcjJuRUtFNjdPZ2V0R3FjZWJ6dlppaXU3UXlpTFlzN1VJK2sxUGhiN1B1WDFTNUFBelpkY2tRSDdiNXZiV2lXVmdyUG9xYjM3M1BnRTJDai8vNFZmYUR4TEhWOThBbCtXbTV5VEd3WjhPWk45MVFSSmdmQkVseXB0ZEw1MDVRbkVzZGQ3aEZUU0VkUTdlUDRaOE1JK1REaVk5VU10WFh4cU1haUY0UWw1RFR5bUMvUWpiSi9aaVZLbXZXeCtFRkZFSHdUTFZYSHNxZUp5bTg5SzNma3Zvdm11NHNhOGN3ZUovWlRwUnJDM0R0M2YrZnd0NFFBRTNEOGxZNjBmNjhEQkdteWNQVDVtRDliUzlYYWxoUT09PC9Nb2R1bHVzPjxFeHBvbmVudD5BUUFCPC9FeHBvbmVudD48UD55Z1lHR00zcFZzemhMWTh1WEZ6cFh5TERxbWFVQVI0aU5nL09yVnNWWkZ5TGVVWnFzQ21WUCsyQjZMcDN0MzNYUS8wc3FISzNmK2p4SlRCRVY0L1lKNi9xTFk1Wi93ekozT0RPZUQ2SFJUMlBOSGFGNEQxem9TUDI4UGx1YjJDZm5yKzNoMTEzQndWZnFiM3ZDVmhTUStjNVpLbWZCdExZL2RmRHg4SGlNcTg9PC9QPjxRPi9PcXlQTnBPL0FST2NMT0crR01IaXVPNDRaZGR6VXE2RndxY3VXSUhITDZ0QkJkQ0tuNFZwTUt6d0dHRlZJWmg1YUlhaml3N1NqYjJwOGRCclMwRUxVNkU0MW5kRHBNVEtLYjNLUnhpTFhkelFTVTdWNStRS1NQSnB2YmhuUVJnT3hQWERwMmY0anhoOWpuYVNudDRkTDNvNndkWUhoS0xVVlNRMURHbkNBcz08L1E+PERQPm1zdVMwRjYyZERQNE5oaUh4VzNMdzRHM3UxRnVCbzA0V1lRek1OR2h5b3krc2VZcXlXQ1RZN0J3NGdvK3dQSkRoMnp1VXdQVFpzYnVQemlFcFRNcXhQNGR5VnBSeXdQWmlNMFlaenBDRytQWFhyT1NVUWZGR2F1ZEEzZWNEdXRTWXlrelR4MW1ucEtYZ2xCdVlCSzB0aUx0N3h1cEptbFlxdWd3czFiMEl6RT08L0RQPjxEUT55MWFkNmxMU0FjZ3NrR3VsN2ZrZ1RVZjhrbEt4OWFWSXE5RzZZMGt1MHF0eVNzR3dUcDJFSlN6c2U1VnNMcUxEL20wdjBISTdVTldUeFJ4cjd5RXNKSWptU2lzcmtOWXFKeHJseDhXc0lVWFNBZVEvSzVsN3U3ZXNIbktLdlVTUllhMzN6eVpuTHVyQkQ4Yy9lM1o5Ujg2UGZyWXU0QzZrbDhUWGsrSS9talU9PC9EUT48SW52ZXJzZVE+V1liRmtGdGFhbm1mZUxmNkVOSVZpcVlRTGdyTmRIZHB4TXlYWXdDbi9tVXlzbzZiTktVUmhtT2VpL0poYVErZzhkWks3emRpdEdPelZSYU91S0Q3VUJZeGIzRHJTdFA4NVZRZm5BdHQzd1JKN0hrU0p1bjFKZnc3VjJNMEtlU29LNnJIUG9sUVV2N1h2OUJnUUJvSGhVRWRlb0tGUkYxM2NoQmZFRTVySkNJPTwvSW52ZXJzZVE+PEQ+aHN1UHRDU0MxbEx5clJ4ditnM0x5clhNS0hKRlRZK3lscnlTMnlmSUZwN2Z4V2FCdmdNUG1leVg0clluSUZoYm5jNjFJRkxaRkxQZmhKaU1relNNdkdqNlNYT3hlL0RVK2oxZlRvV0EzTmNlaGVNK24vSzRhQ0V5ZGdpVnJGSGhlZWRxS3lTZ0hJdGZuMk90dWVNdXlYNWs2ZnZXYXhjQ3BJTzBMcEkzVTRjQmZCdnU3Um92R0Q1ejF4UmlSREdRK1M3OVBabi9LRTlqMWNmcDdKMjIzM2l5WVNqbHRIZ0ZsV2owbkZMNzhXS0tVbVB4QmZoa1BBa05EQ2Jjdm02ZUhpTGJuY2FqdWcrL2JlMXljVmNKZnRqcTJGZU5Jc24vVGJVVFlsekFwSGdUWWF2eCtJRVVzRXV1VGhFNEgrbXpBQ0dmeks1MWxGS0pKTXZicEtTWWZRPT08L0Q+PC9SU0FLZXlWYWx1ZT4=")]
        public void RefreshToken_Basic(JwtHashAlgorithm alg, string secretBase64)
        {
            var container = TestContainerBuilder.GetContainer(builder =>
            {
                builder.RegisterType<SymmetricKeyTokenGenerator>()
                    .Keyed<ITokenGenerator>(JwtHashAlgorithm.HS256)
                    .SingleInstance();
                builder.RegisterType<AsymmetricKeyPairTokenGenerator>()
                    .Keyed<ITokenGenerator>(JwtHashAlgorithm.RS256)
                    .SingleInstance();
            });

            var tokenGenerator = container.ResolveKeyed<ITokenGenerator>(alg);

            var issueTime = DateTime.UtcNow;
            var userId = 165;
            var tid = 111;
            var secret = Convert.FromBase64String(secretBase64);
            var refreshToken = new JsonWebToken().Encode(new
            {
                typ = "refresh_token",
                sub = $"{userId}",
                tid = $"{tid}",
                exp = new DateTimeOffset(issueTime.AddMinutes(15)).ToUnixTimeSeconds(),
                iat = new DateTimeOffset(issueTime).ToUnixTimeSeconds(),
                iss = "issuer",
                aud = "*"
            }, secret, alg);

            var refreshToken2 = tokenGenerator.GenerateRefreshToken(tid, issueTime, userId);
            Assert.Equal(refreshToken, refreshToken2);
       }
    }
}
