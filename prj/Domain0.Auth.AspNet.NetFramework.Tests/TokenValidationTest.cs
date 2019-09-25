using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Domain0.Tokens;
using Jose;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Domain0.Auth.AspNet.NetFramework.Tests
{
    public class TokenValidationTest
    {
        [Fact]
        public void NoSignatureToken_Should_Fail()
        {
            var payload = GetPayload();
            var token = JWT.Encode(payload, null, JwsAlgorithm.none);

            var settings = BuildDefaultTokenValidationSettings();

            var validationParameters = settings.BuildTokenValidationParameters();

            Assert.Throws<SecurityTokenInvalidSignatureException>(() =>
            {
                var claims = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out SecurityToken securityToken);
            });
        }

        [Theory]
        [InlineData(JwsAlgorithm.HS256, "kiHLSfGebYvXGTDx0vWb53Jhyfpnw6HvgRwOJ6h/hUs=")]
        [InlineData(JwsAlgorithm.RS256, "PFJTQUtleVZhbHVlPjxNb2R1bHVzPjdQbkEvcS9FRFErYW9hOGY3RzB6Mlp5T2pTRjd1U29xRHhCUkp1UlJYNWlnQU1nbmlwYkZvVTI4TlBqMFFIcTVvbDVPNUMxZXp3L1ZicVE3SUtITWpnaW5wSFp2MHBDVDFkSzZrT05lVlMveGxSaFdESUZjdzlrZkRYVEMzY21mTXFKUW9IWUhKVXlOTVIyMmxRY1g1NWV6eVhyblNXVjBEZ3dpNXJsSjY0MUdLTkkreWtnZ1FFMFZzZXhjaC9yc0Y4VGVwRUVTVjB5V2Y1V1FKbHk0UTlkVXRDYmNwWTEwcnpQcElBZ09YN2FSM1ljanNvL0NNTVp3M1NjY0ZqWkVaU0ZOM3BtNUVQUkJTM0E5ZWZ1d0NGSEwyTlExNFpYYU9oL1ZkejNsQjVQbDlhaStFK3VDK1Y0YlVrYjErY1NaZGlDdmpId055R0h4MFdPR043eGMvUT09PC9Nb2R1bHVzPjxFeHBvbmVudD5BUUFCPC9FeHBvbmVudD48UD44d3FtM01kWFdNNFI0c0pCZG1FY2twVnF3czhlRy9SZlFla05VTDloVjRDcFR3UjczZ1R0NFFvZ2FXUVI3LytBUGlGQXlIM1g3bzJ4TEVEVWtaTEx6OS8zcUNQNEJvcUw1bUhIRUpMN3BzdnZGYVhpQ1RSVUlWN2xIVnpSUHkvenRtTFdCcmxwb2ZxVThOdE9VK2oyTHhzckE4V3RibTBuN3hNUWF0RHRWN2M9PC9QPjxRPitaeE9LUmtQWFNoWmRQSkV3eUJ0K01GOXhhSHBKeEZndW5WUVRGSzVKQXNGZElLa2lFQUk4UHZWN29xL1Nhd3dEZTk1a3A3a28rVXZPTVExQTJaY2JuSW92YkdYZ0NHNjdPMVJlRWx0Qm9JZWZ1ZHVpbFRyRDltVTFBeWVQQ3hwa084QUVkL2F5dXA4WDg4Z3N5U2xGaFpVbWtmTGVIWVJqYU9EVTJhWjZPcz08L1E+PERQPnBVNEdaajNUUkJ5TS83MStSdVVRU1FjRm9WQzhPdWxBYlJUMU1JbXF6SmcvRC9hTnhWbDI3a3d6OVZyUjlIbkVvUDEvRVo2K1lvdlBDTGxqbTB2TUFpeGtSdUdJRGZMbjZwOXdoTzVqNlhQbHZzU2Y5QUM2aENRR0U2MlF5TGgxdkFTSGEvVnFTbmlrR3hvZXNXWFBKQVVIZ2I1UEVyOTluTmRMb0V0UVV3cz08L0RQPjxEUT5sWllwZFJHeWxtWjI3ZEcraVoxbXFqdnl6cnlRU0R4dTFtODFsdmZBUWl4a3NZVVZheDNNL2ZZK0o3MTRrNE1nTFVuRmRxdklZN3dXUjVPMkhYcDdqQ2pYNTQ2Rk4yRi9ienR0cG9PQ1ZmTW1xWEN5V3k0MnpJSGRZaExKeFUvc1Y5SVRIYU1rc0pSRHd2c1RJcWlrVW85QlZsQU9UUHVjMjJBUmRLcTVNODg9PC9EUT48SW52ZXJzZVE+cU0ycndPbnFZMFE0TDZSQjVzRmNhLzRsTkR6aHN2NHBQYXhzU05IMldjTkJ1c1QycVpRUUxrVjVVc1RSaVFxMGdHaVM0SFlrRXdUaWJnclBLbklKZGlKV0pvUlc4MGJ0Skt4YVRCREhKbDBPY0hDMkovQmp6clZsMWQ4Qy9QdzRKbEpmc1RtRmpkai9vWUUxaWxZZ3JoMVNsOVIyRVRESVFxUXM3VEh3RUVjPTwvSW52ZXJzZVE+PEQ+R0czV3UycWJKMDJJZjBweVhBa1Y0MFVFWFFBcXBQZjN4ZjNFbitIQmtiMUdJVTZBNVFTTjh3YjBjL0dHSEcrS1czYWxZMWhhQWJaOEpoL0FDS2tsVVM4TE9TVU1lZ1IxZVFGMFFpTnVRaEhqTVorRW4xMW1scm4zUFlmZjNzVStLQlliZ2E4cEpXc3kwdkZoTlJPNkh5SEFZdkRNandCcWFsaE44TU1CSDhzSnV4MTNGSVVzVTlsMW9IMjY5cHIzSG1xWWFFZDVXOHJHeU0zZnZTTG01NFVuRWlFc2F1Q2NmQjNncmRZZEwwSEdTRXFFRXczbVNnbEJCMUpqNzFiV3dYYkNkTXdjVGN5akhmUmNwakhWZ2lZdUhSYkEwYUo5WWJJY1JHQVcvSk53M3lxRDdKVDA3YnJNMldpbE5LcytDS1VpREpuaWxKVUxIV3gyRGE5cjhRPT08L0Q+PC9SU0FLZXlWYWx1ZT4=")]
        public void WrongSignatureToken_Should_Fail(JwsAlgorithm algorithm, string key)
        {
            var payload = GetPayload();
            var signKey = GetKey(key, algorithm);

            var token = JWT.Encode(payload, signKey, algorithm);

            var settings = BuildDefaultTokenValidationSettings();

            var validationParameters = settings.BuildTokenValidationParameters();

            Assert.Throws<SecurityTokenInvalidSignatureException>(() =>
            {
                var claims = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out SecurityToken securityToken);
            });
        }

        [Theory]
        [InlineData(JwsAlgorithm.HS256, "kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs=")]
        [InlineData(JwsAlgorithm.RS256, "PFJTQUtleVZhbHVlPjxNb2R1bHVzPng1Y2tLak5NQzUvVTRETnZ2ZEhTRnkzc1ZoUDgxRmZrZ1lmZ3o5RjRXK05mUVpXK3k2Mk43NmZwRjRMOGh0cVNoSjFRcjJuRUtFNjdPZ2V0R3FjZWJ6dlppaXU3UXlpTFlzN1VJK2sxUGhiN1B1WDFTNUFBelpkY2tRSDdiNXZiV2lXVmdyUG9xYjM3M1BnRTJDai8vNFZmYUR4TEhWOThBbCtXbTV5VEd3WjhPWk45MVFSSmdmQkVseXB0ZEw1MDVRbkVzZGQ3aEZUU0VkUTdlUDRaOE1JK1REaVk5VU10WFh4cU1haUY0UWw1RFR5bUMvUWpiSi9aaVZLbXZXeCtFRkZFSHdUTFZYSHNxZUp5bTg5SzNma3Zvdm11NHNhOGN3ZUovWlRwUnJDM0R0M2YrZnd0NFFBRTNEOGxZNjBmNjhEQkdteWNQVDVtRDliUzlYYWxoUT09PC9Nb2R1bHVzPjxFeHBvbmVudD5BUUFCPC9FeHBvbmVudD48UD55Z1lHR00zcFZzemhMWTh1WEZ6cFh5TERxbWFVQVI0aU5nL09yVnNWWkZ5TGVVWnFzQ21WUCsyQjZMcDN0MzNYUS8wc3FISzNmK2p4SlRCRVY0L1lKNi9xTFk1Wi93ekozT0RPZUQ2SFJUMlBOSGFGNEQxem9TUDI4UGx1YjJDZm5yKzNoMTEzQndWZnFiM3ZDVmhTUStjNVpLbWZCdExZL2RmRHg4SGlNcTg9PC9QPjxRPi9PcXlQTnBPL0FST2NMT0crR01IaXVPNDRaZGR6VXE2RndxY3VXSUhITDZ0QkJkQ0tuNFZwTUt6d0dHRlZJWmg1YUlhaml3N1NqYjJwOGRCclMwRUxVNkU0MW5kRHBNVEtLYjNLUnhpTFhkelFTVTdWNStRS1NQSnB2YmhuUVJnT3hQWERwMmY0anhoOWpuYVNudDRkTDNvNndkWUhoS0xVVlNRMURHbkNBcz08L1E+PERQPm1zdVMwRjYyZERQNE5oaUh4VzNMdzRHM3UxRnVCbzA0V1lRek1OR2h5b3krc2VZcXlXQ1RZN0J3NGdvK3dQSkRoMnp1VXdQVFpzYnVQemlFcFRNcXhQNGR5VnBSeXdQWmlNMFlaenBDRytQWFhyT1NVUWZGR2F1ZEEzZWNEdXRTWXlrelR4MW1ucEtYZ2xCdVlCSzB0aUx0N3h1cEptbFlxdWd3czFiMEl6RT08L0RQPjxEUT55MWFkNmxMU0FjZ3NrR3VsN2ZrZ1RVZjhrbEt4OWFWSXE5RzZZMGt1MHF0eVNzR3dUcDJFSlN6c2U1VnNMcUxEL20wdjBISTdVTldUeFJ4cjd5RXNKSWptU2lzcmtOWXFKeHJseDhXc0lVWFNBZVEvSzVsN3U3ZXNIbktLdlVTUllhMzN6eVpuTHVyQkQ4Yy9lM1o5Ujg2UGZyWXU0QzZrbDhUWGsrSS9talU9PC9EUT48SW52ZXJzZVE+V1liRmtGdGFhbm1mZUxmNkVOSVZpcVlRTGdyTmRIZHB4TXlYWXdDbi9tVXlzbzZiTktVUmhtT2VpL0poYVErZzhkWks3emRpdEdPelZSYU91S0Q3VUJZeGIzRHJTdFA4NVZRZm5BdHQzd1JKN0hrU0p1bjFKZnc3VjJNMEtlU29LNnJIUG9sUVV2N1h2OUJnUUJvSGhVRWRlb0tGUkYxM2NoQmZFRTVySkNJPTwvSW52ZXJzZVE+PEQ+aHN1UHRDU0MxbEx5clJ4ditnM0x5clhNS0hKRlRZK3lscnlTMnlmSUZwN2Z4V2FCdmdNUG1leVg0clluSUZoYm5jNjFJRkxaRkxQZmhKaU1relNNdkdqNlNYT3hlL0RVK2oxZlRvV0EzTmNlaGVNK24vSzRhQ0V5ZGdpVnJGSGhlZWRxS3lTZ0hJdGZuMk90dWVNdXlYNWs2ZnZXYXhjQ3BJTzBMcEkzVTRjQmZCdnU3Um92R0Q1ejF4UmlSREdRK1M3OVBabi9LRTlqMWNmcDdKMjIzM2l5WVNqbHRIZ0ZsV2owbkZMNzhXS0tVbVB4QmZoa1BBa05EQ2Jjdm02ZUhpTGJuY2FqdWcrL2JlMXljVmNKZnRqcTJGZU5Jc24vVGJVVFlsekFwSGdUWWF2eCtJRVVzRXV1VGhFNEgrbXpBQ0dmeks1MWxGS0pKTXZicEtTWWZRPT08L0Q+PC9SU0FLZXlWYWx1ZT4=")]
        public void PassCorrectToken(JwsAlgorithm algorithm, string key)
        {
            IdentityModelEventSource.ShowPII = true;

            var payload = GetPayload();
            var signKey = GetKey(key, algorithm);

            var token = JWT.Encode(payload, signKey, algorithm);

            var settings = BuildDefaultTokenValidationSettings();

            var validationParameters = settings.BuildTokenValidationParameters();
            var claims = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
            Assert.NotNull(claims);
        }

        [Theory]
        [InlineData(JwsAlgorithm.HS256, "kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs=")]
        [InlineData(JwsAlgorithm.RS256, "PFJTQUtleVZhbHVlPjxNb2R1bHVzPng1Y2tLak5NQzUvVTRETnZ2ZEhTRnkzc1ZoUDgxRmZrZ1lmZ3o5RjRXK05mUVpXK3k2Mk43NmZwRjRMOGh0cVNoSjFRcjJuRUtFNjdPZ2V0R3FjZWJ6dlppaXU3UXlpTFlzN1VJK2sxUGhiN1B1WDFTNUFBelpkY2tRSDdiNXZiV2lXVmdyUG9xYjM3M1BnRTJDai8vNFZmYUR4TEhWOThBbCtXbTV5VEd3WjhPWk45MVFSSmdmQkVseXB0ZEw1MDVRbkVzZGQ3aEZUU0VkUTdlUDRaOE1JK1REaVk5VU10WFh4cU1haUY0UWw1RFR5bUMvUWpiSi9aaVZLbXZXeCtFRkZFSHdUTFZYSHNxZUp5bTg5SzNma3Zvdm11NHNhOGN3ZUovWlRwUnJDM0R0M2YrZnd0NFFBRTNEOGxZNjBmNjhEQkdteWNQVDVtRDliUzlYYWxoUT09PC9Nb2R1bHVzPjxFeHBvbmVudD5BUUFCPC9FeHBvbmVudD48UD55Z1lHR00zcFZzemhMWTh1WEZ6cFh5TERxbWFVQVI0aU5nL09yVnNWWkZ5TGVVWnFzQ21WUCsyQjZMcDN0MzNYUS8wc3FISzNmK2p4SlRCRVY0L1lKNi9xTFk1Wi93ekozT0RPZUQ2SFJUMlBOSGFGNEQxem9TUDI4UGx1YjJDZm5yKzNoMTEzQndWZnFiM3ZDVmhTUStjNVpLbWZCdExZL2RmRHg4SGlNcTg9PC9QPjxRPi9PcXlQTnBPL0FST2NMT0crR01IaXVPNDRaZGR6VXE2RndxY3VXSUhITDZ0QkJkQ0tuNFZwTUt6d0dHRlZJWmg1YUlhaml3N1NqYjJwOGRCclMwRUxVNkU0MW5kRHBNVEtLYjNLUnhpTFhkelFTVTdWNStRS1NQSnB2YmhuUVJnT3hQWERwMmY0anhoOWpuYVNudDRkTDNvNndkWUhoS0xVVlNRMURHbkNBcz08L1E+PERQPm1zdVMwRjYyZERQNE5oaUh4VzNMdzRHM3UxRnVCbzA0V1lRek1OR2h5b3krc2VZcXlXQ1RZN0J3NGdvK3dQSkRoMnp1VXdQVFpzYnVQemlFcFRNcXhQNGR5VnBSeXdQWmlNMFlaenBDRytQWFhyT1NVUWZGR2F1ZEEzZWNEdXRTWXlrelR4MW1ucEtYZ2xCdVlCSzB0aUx0N3h1cEptbFlxdWd3czFiMEl6RT08L0RQPjxEUT55MWFkNmxMU0FjZ3NrR3VsN2ZrZ1RVZjhrbEt4OWFWSXE5RzZZMGt1MHF0eVNzR3dUcDJFSlN6c2U1VnNMcUxEL20wdjBISTdVTldUeFJ4cjd5RXNKSWptU2lzcmtOWXFKeHJseDhXc0lVWFNBZVEvSzVsN3U3ZXNIbktLdlVTUllhMzN6eVpuTHVyQkQ4Yy9lM1o5Ujg2UGZyWXU0QzZrbDhUWGsrSS9talU9PC9EUT48SW52ZXJzZVE+V1liRmtGdGFhbm1mZUxmNkVOSVZpcVlRTGdyTmRIZHB4TXlYWXdDbi9tVXlzbzZiTktVUmhtT2VpL0poYVErZzhkWks3emRpdEdPelZSYU91S0Q3VUJZeGIzRHJTdFA4NVZRZm5BdHQzd1JKN0hrU0p1bjFKZnc3VjJNMEtlU29LNnJIUG9sUVV2N1h2OUJnUUJvSGhVRWRlb0tGUkYxM2NoQmZFRTVySkNJPTwvSW52ZXJzZVE+PEQ+aHN1UHRDU0MxbEx5clJ4ditnM0x5clhNS0hKRlRZK3lscnlTMnlmSUZwN2Z4V2FCdmdNUG1leVg0clluSUZoYm5jNjFJRkxaRkxQZmhKaU1relNNdkdqNlNYT3hlL0RVK2oxZlRvV0EzTmNlaGVNK24vSzRhQ0V5ZGdpVnJGSGhlZWRxS3lTZ0hJdGZuMk90dWVNdXlYNWs2ZnZXYXhjQ3BJTzBMcEkzVTRjQmZCdnU3Um92R0Q1ejF4UmlSREdRK1M3OVBabi9LRTlqMWNmcDdKMjIzM2l5WVNqbHRIZ0ZsV2owbkZMNzhXS0tVbVB4QmZoa1BBa05EQ2Jjdm02ZUhpTGJuY2FqdWcrL2JlMXljVmNKZnRqcTJGZU5Jc24vVGJVVFlsekFwSGdUWWF2eCtJRVVzRXV1VGhFNEgrbXpBQ0dmeks1MWxGS0pKTXZicEtTWWZRPT08L0Q+PC9SU0FLZXlWYWx1ZT4=")]
        public void ExpiredToken_Should_Fail(JwsAlgorithm algorithm, string key)
        {
            var payload = GetPayload();
            var signKey = GetKey(key, algorithm);

            var token = JWT.Encode(payload, signKey, algorithm);

            var settings = BuildDefaultTokenValidationSettings();
            settings.ValidateLifetime = true;

            var validationParameters = settings.BuildTokenValidationParameters();

            Assert.Throws<SecurityTokenExpiredException>(() =>
            {
                var claims = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out SecurityToken securityToken);
            });
        }

        [Theory]
        [InlineData(JwsAlgorithm.HS256, "kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs=")]
        [InlineData(JwsAlgorithm.RS256, "PFJTQUtleVZhbHVlPjxNb2R1bHVzPng1Y2tLak5NQzUvVTRETnZ2ZEhTRnkzc1ZoUDgxRmZrZ1lmZ3o5RjRXK05mUVpXK3k2Mk43NmZwRjRMOGh0cVNoSjFRcjJuRUtFNjdPZ2V0R3FjZWJ6dlppaXU3UXlpTFlzN1VJK2sxUGhiN1B1WDFTNUFBelpkY2tRSDdiNXZiV2lXVmdyUG9xYjM3M1BnRTJDai8vNFZmYUR4TEhWOThBbCtXbTV5VEd3WjhPWk45MVFSSmdmQkVseXB0ZEw1MDVRbkVzZGQ3aEZUU0VkUTdlUDRaOE1JK1REaVk5VU10WFh4cU1haUY0UWw1RFR5bUMvUWpiSi9aaVZLbXZXeCtFRkZFSHdUTFZYSHNxZUp5bTg5SzNma3Zvdm11NHNhOGN3ZUovWlRwUnJDM0R0M2YrZnd0NFFBRTNEOGxZNjBmNjhEQkdteWNQVDVtRDliUzlYYWxoUT09PC9Nb2R1bHVzPjxFeHBvbmVudD5BUUFCPC9FeHBvbmVudD48UD55Z1lHR00zcFZzemhMWTh1WEZ6cFh5TERxbWFVQVI0aU5nL09yVnNWWkZ5TGVVWnFzQ21WUCsyQjZMcDN0MzNYUS8wc3FISzNmK2p4SlRCRVY0L1lKNi9xTFk1Wi93ekozT0RPZUQ2SFJUMlBOSGFGNEQxem9TUDI4UGx1YjJDZm5yKzNoMTEzQndWZnFiM3ZDVmhTUStjNVpLbWZCdExZL2RmRHg4SGlNcTg9PC9QPjxRPi9PcXlQTnBPL0FST2NMT0crR01IaXVPNDRaZGR6VXE2RndxY3VXSUhITDZ0QkJkQ0tuNFZwTUt6d0dHRlZJWmg1YUlhaml3N1NqYjJwOGRCclMwRUxVNkU0MW5kRHBNVEtLYjNLUnhpTFhkelFTVTdWNStRS1NQSnB2YmhuUVJnT3hQWERwMmY0anhoOWpuYVNudDRkTDNvNndkWUhoS0xVVlNRMURHbkNBcz08L1E+PERQPm1zdVMwRjYyZERQNE5oaUh4VzNMdzRHM3UxRnVCbzA0V1lRek1OR2h5b3krc2VZcXlXQ1RZN0J3NGdvK3dQSkRoMnp1VXdQVFpzYnVQemlFcFRNcXhQNGR5VnBSeXdQWmlNMFlaenBDRytQWFhyT1NVUWZGR2F1ZEEzZWNEdXRTWXlrelR4MW1ucEtYZ2xCdVlCSzB0aUx0N3h1cEptbFlxdWd3czFiMEl6RT08L0RQPjxEUT55MWFkNmxMU0FjZ3NrR3VsN2ZrZ1RVZjhrbEt4OWFWSXE5RzZZMGt1MHF0eVNzR3dUcDJFSlN6c2U1VnNMcUxEL20wdjBISTdVTldUeFJ4cjd5RXNKSWptU2lzcmtOWXFKeHJseDhXc0lVWFNBZVEvSzVsN3U3ZXNIbktLdlVTUllhMzN6eVpuTHVyQkQ4Yy9lM1o5Ujg2UGZyWXU0QzZrbDhUWGsrSS9talU9PC9EUT48SW52ZXJzZVE+V1liRmtGdGFhbm1mZUxmNkVOSVZpcVlRTGdyTmRIZHB4TXlYWXdDbi9tVXlzbzZiTktVUmhtT2VpL0poYVErZzhkWks3emRpdEdPelZSYU91S0Q3VUJZeGIzRHJTdFA4NVZRZm5BdHQzd1JKN0hrU0p1bjFKZnc3VjJNMEtlU29LNnJIUG9sUVV2N1h2OUJnUUJvSGhVRWRlb0tGUkYxM2NoQmZFRTVySkNJPTwvSW52ZXJzZVE+PEQ+aHN1UHRDU0MxbEx5clJ4ditnM0x5clhNS0hKRlRZK3lscnlTMnlmSUZwN2Z4V2FCdmdNUG1leVg0clluSUZoYm5jNjFJRkxaRkxQZmhKaU1relNNdkdqNlNYT3hlL0RVK2oxZlRvV0EzTmNlaGVNK24vSzRhQ0V5ZGdpVnJGSGhlZWRxS3lTZ0hJdGZuMk90dWVNdXlYNWs2ZnZXYXhjQ3BJTzBMcEkzVTRjQmZCdnU3Um92R0Q1ejF4UmlSREdRK1M3OVBabi9LRTlqMWNmcDdKMjIzM2l5WVNqbHRIZ0ZsV2owbkZMNzhXS0tVbVB4QmZoa1BBa05EQ2Jjdm02ZUhpTGJuY2FqdWcrL2JlMXljVmNKZnRqcTJGZU5Jc24vVGJVVFlsekFwSGdUWWF2eCtJRVVzRXV1VGhFNEgrbXpBQ0dmeks1MWxGS0pKTXZicEtTWWZRPT08L0Q+PC9SU0FLZXlWYWx1ZT4=")]
        public void WrongType_Should_Fail(JwsAlgorithm algorithm, string key)
        {
            var payload = GetPayload();
            payload = payload.Replace("access_token", "another");

            var signKey = GetKey(key, algorithm);

            var token = JWT.Encode(payload, signKey, algorithm);

            var settings = BuildDefaultTokenValidationSettings();

            var validationParameters = settings.BuildTokenValidationParameters();

            Assert.Throws<UnauthorizedAccessException>(() =>
            {
                var claims = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out SecurityToken securityToken);
            });
        }

        private static string GetPayload()
        {
            var json = @"{""typ"":""access_token"",""sub"":""371"",""permissions"":""[\""domain0.basic\""]"",""exp"":1544422770,""iat"":1544422170,""iss"":""Issuer"",""aud"":""*""}";
            return json;
        }

        private static object GetKey(string key, JwsAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case JwsAlgorithm.HS256:
                    return Convert.FromBase64String(key);

                case JwsAlgorithm.RS256:
                    var rsa = new RSACryptoServiceProvider(2048);
                    var raw = Convert.FromBase64String(key);
                    var privateKey = Encoding.UTF8.GetString(raw);
                    rsa.FromXmlString(privateKey);
                    return rsa;

                default:
                    return null;
            }
        }

        private static TokenValidationSettings BuildDefaultTokenValidationSettings()
        {
            var settings = new TokenValidationSettings
            {
                Audience = "*",
                Issuer = "Issuer",
                ValidateLifetime = false,
                Keys = new[]
                {
                    new KeyInfo
                    {
                        Alg = SecurityAlgorithms.HmacSha256,
                        Key = "kiHLSfGebYvXGTDx0vWb53JhyUpnw6HvgRwOJ6h/hUs="
                    },
                    new KeyInfo
                    {
                        Alg = SecurityAlgorithms.RsaSha256,
                        Key = "PFJTQUtleVZhbHVlPjxNb2R1bHVzPng1Y2tLak5NQzUvVTRETnZ2ZEhTRnkzc1ZoUDgxRmZrZ1lmZ3o5RjRXK05mUVpXK3k2Mk43NmZwRjRMOGh0cVNoSjFRcjJuRUtFNjdPZ2V0R3FjZWJ6dlppaXU3UXlpTFlzN1VJK2sxUGhiN1B1WDFTNUFBelpkY2tRSDdiNXZiV2lXVmdyUG9xYjM3M1BnRTJDai8vNFZmYUR4TEhWOThBbCtXbTV5VEd3WjhPWk45MVFSSmdmQkVseXB0ZEw1MDVRbkVzZGQ3aEZUU0VkUTdlUDRaOE1JK1REaVk5VU10WFh4cU1haUY0UWw1RFR5bUMvUWpiSi9aaVZLbXZXeCtFRkZFSHdUTFZYSHNxZUp5bTg5SzNma3Zvdm11NHNhOGN3ZUovWlRwUnJDM0R0M2YrZnd0NFFBRTNEOGxZNjBmNjhEQkdteWNQVDVtRDliUzlYYWxoUT09PC9Nb2R1bHVzPjxFeHBvbmVudD5BUUFCPC9FeHBvbmVudD48L1JTQUtleVZhbHVlPg =="
                    }
                }
            };
            return settings;
        }
    }
}
