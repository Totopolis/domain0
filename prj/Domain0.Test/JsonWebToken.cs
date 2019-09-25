using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace Sdl.Domain0.Shared
{
    public enum JwtHashAlgorithm
    {
        HS256,
        HS384,
        HS512,
        RS256
    }

    /// <summary>
    /// Provides methods for encoding and decoding JSON Web Tokens.
    /// </summary>
    public class JsonWebToken
    {
        private readonly Dictionary<JwtHashAlgorithm, Func<byte[], byte[], byte[]>> _hashAlgorithms;
        private readonly JavaScriptSerializer _jsonSerializer = new JavaScriptSerializer();

        public JsonWebToken()
        {
            _hashAlgorithms = new Dictionary<JwtHashAlgorithm, Func<byte[], byte[], byte[]>>
            {
                { JwtHashAlgorithm.HS256, (key, value) => { using (var sha = new HMACSHA256(key)) { return sha.ComputeHash(value); } } },
                { JwtHashAlgorithm.HS384, (key, value) => { using (var sha = new HMACSHA384(key)) { return sha.ComputeHash(value); } } },
                { JwtHashAlgorithm.HS512, (key, value) => { using (var sha = new HMACSHA512(key)) { return sha.ComputeHash(value); } } },
                { JwtHashAlgorithm.RS256, (key, value) =>
                    {
                        using (var rsaProvider = new RSACryptoServiceProvider(2048))
                        {
                            rsaProvider.FromXmlString(
                                Encoding.UTF8.GetString(key));

                            return rsaProvider.SignData(
                                value, 
                                HashAlgorithmName.SHA256, 
                                RSASignaturePadding.Pkcs1);
                        }
                    }
                },
            };
        }

        /// <summary>
        /// Creates a JWT given a payload, the signing key (as a string that will be decoded with UTF8), and the algorithm to use.
        /// </summary>
        /// <param name="payload">An arbitrary payload (must be serializable to JSON via <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).</param>
        /// <param name="key">The key used to sign the token.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>The generated JWT.</returns>
        public string Encode(object payload, string key, JwtHashAlgorithm algorithm)
        {
            return Encode(payload, Encoding.UTF8.GetBytes(key), algorithm);
        }

        /// <summary>
        /// Creates a JWT given a payload, the signing key, and the algorithm to use.
        /// </summary>
        /// <param name="payload">An arbitrary payload (must be serializable to JSON via <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).</param>
        /// <param name="key">The key used to sign the token.</param>
        /// <param name="algorithm">The hash algorithm to use.</param>
        /// <returns>The generated JWT.</returns>
        public string Encode(object payload, byte[] key, JwtHashAlgorithm algorithm)
        {
            var segments = new List<string>();
            var header = new { alg = algorithm.ToString(), typ = "JWT" };

            byte[] headerBytes = Encoding.UTF8.GetBytes(_jsonSerializer.Serialize(header));
            byte[] payloadBytes = Encoding.UTF8.GetBytes(_jsonSerializer.Serialize(payload));

            segments.Add(Base64UrlEncode(headerBytes));
            segments.Add(Base64UrlEncode(payloadBytes));

            var stringToSign = string.Join(".", segments.ToArray());

            var bytesToSign = Encoding.UTF8.GetBytes(stringToSign);

            byte[] signature = _hashAlgorithms[algorithm](key, bytesToSign);
            segments.Add(Base64UrlEncode(signature));

            return string.Join(".", segments.ToArray());
        }

        /// <summary>
        /// Given a JWT, decode it and return the JSON payload.
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT (string that will be decoded with UTF8).</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <returns>A string containing the JSON payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        public string Decode(string token, byte[] key, bool verify = true)
        {
            var parts = token.Split('.');
            var header = parts[0];
            var payload = parts[1];
            byte[] crypto = Base64UrlDecode(parts[2]);

            var headerJson = Encoding.UTF8.GetString(Base64UrlDecode(header));
            var headerData = _jsonSerializer.Deserialize<Dictionary<string, object>>(headerJson);
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));

            if (verify)
            {
                var bytesToSign = Encoding.UTF8.GetBytes(string.Concat(header, ".", payload));
                var keyBytes = key;
                var algorithm = (string) headerData["alg"];

                var signature = _hashAlgorithms[GetHashAlgorithm(algorithm)](keyBytes, bytesToSign);
                var decodedCrypto = Convert.ToBase64String(crypto);
                var decodedSignature = Convert.ToBase64String(signature);

                if (decodedCrypto != decodedSignature)
                {
                   // throw new SecurityTokenValidationException(string.Format(CultureInfo.InvariantCulture, ErrorMessages.IDX10504, jwt.ToString()));
                    throw new SignatureVerificationException(
                        $"Invalid signature. Expected {decodedCrypto} got {decodedSignature}");
                }
            }

            return payloadJson;
        }

        /// <summary>
        /// Given a JWT, decode it and return the payload as an object (by deserializing it with <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT as a string that will be decoded with UTF8.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <param name="checkExpiration">Whether to check token expiration (exp) (default is false)</param>
        /// <param name="audience">The expected audience (aud) of the JWT (default is null, dont check audience)</param>
        /// <param name="issuer">The expected issuer (iss) of the JWT (default is null, dont check issuer)</param>
        /// <returns>An object representing the payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        /// <exception cref="TokenValidationException">Thrown if audience, expiration or issuer check fails.</exception>
        public object DecodeToObject(string token, string key, bool verify = true, bool checkExpiration = false, string audience = null, string issuer = null)
        {
            return DecodeToObject(token, Encoding.UTF8.GetBytes(key), verify, checkExpiration, audience, issuer);
        }

        /// <summary>
        /// Given a JWT, decode it and return the payload as an object (by deserializing it with <see cref="System.Web.Script.Serialization.JavaScriptSerializer"/>).
        /// </summary>
        /// <param name="token">The JWT.</param>
        /// <param name="key">The key that was used to sign the JWT as a byte array.</param>
        /// <param name="verify">Whether to verify the signature (default is true).</param>
        /// <param name="checkExpiration">Whether to check token expiration (exp) (default is false)</param>
        /// <param name="audience">The expected audience (aud) of the JWT (default is null, dont check audience)</param>
        /// <param name="issuer">The expected issuer (iss) of the JWT (default is null, dont check issuer)</param>
        /// <returns>An object representing the payload.</returns>
        /// <exception cref="SignatureVerificationException">Thrown if the verify parameter was true and the signature was NOT valid or if the JWT was signed with an unsupported algorithm.</exception>
        /// <exception cref="TokenValidationException">Thrown if audience, expiration or issuer check fails.</exception>
        public object DecodeToObject(string token, byte[] key, bool verify = true, bool checkExpiration = false, string audience = null, string issuer = null)
        {
            var payloadJson = Decode(token, key, verify);
            var payloadData = _jsonSerializer.Deserialize<Dictionary<string, object>>(payloadJson);

            if (!string.IsNullOrEmpty(audience) && payloadData.TryGetValue("aud", out var aud))
            {
                if (!aud.ToString().Equals(audience, StringComparison.Ordinal))
                {
                    throw new TokenValidationException($"Audience mismatch. Expected: '{audience}' and got: '{aud}'");
                }
            }

            if (checkExpiration && payloadData.TryGetValue("exp", out var exp))
            {
                DateTime validTo = FromUnixTime(long.Parse(exp.ToString()));
                if (DateTime.Compare(validTo, DateTime.UtcNow) <= 0)
                {
                    throw new TokenValidationException(
                        $"Token is expired. Expiration: '{validTo}'. Current: '{DateTime.UtcNow}'");
                }
            }

            if (!string.IsNullOrEmpty(issuer) && payloadData.TryGetValue("iss", out var iss))
            {
                if (!iss.ToString().Equals(issuer, StringComparison.Ordinal))
                {
                    throw new TokenValidationException($"Token issuer mismatch. Expected: '{issuer}' and got: '{iss}'");
                }
            }

            return payloadData;
        }

        private DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        private JwtHashAlgorithm GetHashAlgorithm(string algorithm)
        {
            switch (algorithm)
            {
                case "HS256": return JwtHashAlgorithm.HS256;
                case "HS384": return JwtHashAlgorithm.HS384;
                case "HS512": return JwtHashAlgorithm.HS512;
                default: throw new SignatureVerificationException("Algorithm not supported.");
            }
        }

        // from JWT spec
        private string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Split('=')[0]; // Remove any trailing '='s
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding
            return output;
        }

        // from JWT spec
        private byte[] Base64UrlDecode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break; // One pad char
                default: throw new Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }
    }

    public class SignatureVerificationException : Exception
    {
        public SignatureVerificationException(string message)
            : base(message)
        {
        }
    }

    public class TokenValidationException : Exception
    {
        public TokenValidationException(string message)
            : base(message)
        {
        }
    }
}