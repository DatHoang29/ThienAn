using NATS.Client.Core;
using Modules.DataTransporter.Natsio.Infrastructure.Configuration;
using NATS.Client;

namespace Modules.DataTransporter.Natsio.Infrastructure.Security;

public static class NatsAuthFactory
{
    public static NatsAuthOpts BuildAuthOpts(NatsRuntimeConfig config, out string? currentNKey)
    {
        currentNKey = null;
        var mode = config.AuthMode?.Trim().ToUpper() ?? "NONE";

        return mode switch
        {
            "NKEY" => BuildNKeyAuth(config, out currentNKey),
            "TOKEN" => BuildTokenAuth(config),
            "USERPASS" => BuildUserPassAuth(config),
            "TLS" => NatsAuthOpts.Default,
            "JWT" => BuildJwtAuth(config, out currentNKey),
            "NONE" => NatsAuthOpts.Default,
            _ => NatsAuthOpts.Default
        };
    }

    public static NatsTlsOpts BuildTlsOpts(NatsRuntimeConfig config)
    {
        var isTls = config.Url?.StartsWith("tls://", StringComparison.OrdinalIgnoreCase) == true
                    || config.Url?.StartsWith("wss://", StringComparison.OrdinalIgnoreCase) == true;
        var hasTlsAuth = string.Equals(config.AuthMode?.Trim(), "TLS", StringComparison.OrdinalIgnoreCase);
        var hasCaFile = !string.IsNullOrWhiteSpace(config.CaFile);
        var hasCert = !string.IsNullOrWhiteSpace(config.TlsCertFile);

        // Nếu không dùng tls:// hoặc wss://, không có TLS auth, không có cert/CA và không bật skip verify
        if (!isTls && !hasTlsAuth && !hasCaFile && !hasCert && !config.InsecureSkipVerify)
            return NatsTlsOpts.Default;

        string? caFile = null;
        if (hasCaFile)
        {
            var path = ResolvePath(config.CaFile!);
            if (File.Exists(path))
                caFile = path;
        }

        string? certFile = null;
        if (hasCert)
        {
            var path = ResolvePath(config.TlsCertFile!);
            if (File.Exists(path))
                certFile = path;
        }

        string? keyFile = null;
        if (!string.IsNullOrWhiteSpace(config.TlsKeyFile))
        {
            var path = ResolvePath(config.TlsKeyFile!);
            if (File.Exists(path))
                keyFile = path;
        }

        return new NatsTlsOpts
        {
            CaFile = caFile,
            CertFile = certFile,
            KeyFile = keyFile,
            InsecureSkipVerify = config.InsecureSkipVerify
        };
    }

    private static NatsAuthOpts BuildNKeyAuth(NatsRuntimeConfig config, out string publicNKey)
    {
        var seed = GetSeed(config);
        publicNKey = !string.IsNullOrWhiteSpace(config.NKey)
            ? config.NKey.Trim()
            : Nkeys.PublicKeyFromSeed(seed);

        return new NatsAuthOpts { NKey = publicNKey, Seed = seed };
    }

    private static NatsAuthOpts BuildTokenAuth(NatsRuntimeConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Token))
            throw new InvalidOperationException("Token auth requires 'Token' in config.");

        return new NatsAuthOpts { Token = config.Token.Trim() };
    }

    private static NatsAuthOpts BuildUserPassAuth(NatsRuntimeConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Username) || string.IsNullOrWhiteSpace(config.Password))
            throw new InvalidOperationException("UserPass auth requires 'Username' and 'Password' in config.");

        return new NatsAuthOpts
        {
            Username = config.Username.Trim(),
            Password = config.Password.Trim()
        };
    }

    private static NatsAuthOpts BuildJwtAuth(NatsRuntimeConfig config, out string currentNKey)
    {
        var jwt = GetUserJwt(config);
        var seed = GetUserSeed(config);

        try { currentNKey = Nkeys.PublicKeyFromSeed(seed); }
        catch { currentNKey = "N/A (JWT)"; }

        return new NatsAuthOpts { Jwt = jwt, Seed = seed };
    }

    private static string GetSeed(NatsRuntimeConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.Seed))
            return config.Seed.Trim();

        if (!string.IsNullOrWhiteSpace(config.SeedFile))
        {
            var seedPath = ResolvePath(config.SeedFile);
            if (!File.Exists(seedPath))
                throw new FileNotFoundException("NKEY Seed file not found.", seedPath);

            var seed = File.ReadAllLines(seedPath)
                .Select(x => x.Trim())
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && x.StartsWith("SU", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(seed)) return seed;
        }

        throw new InvalidOperationException("NKEY Seed not configured.");
    }

    private static string GetUserJwt(NatsRuntimeConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.Jwt))
            return config.Jwt.Trim();

        if (!string.IsNullOrWhiteSpace(config.JwtFile))
        {
            var path = ResolvePath(config.JwtFile);
            if (!File.Exists(path))
                throw new FileNotFoundException("JWT file not found.", path);

            var jwt = File.ReadAllLines(path)
                .Select(x => x.Trim())
                .FirstOrDefault(x => x.StartsWith("eyJ", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(jwt)) return jwt;

            throw new InvalidDataException("No valid JWT found in file.");
        }

        throw new InvalidOperationException("JWT not configured.");
    }

    private static string GetUserSeed(NatsRuntimeConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.UserSeed))
            return config.UserSeed.Trim();

        if (!string.IsNullOrWhiteSpace(config.UserSeedFile))
        {
            var path = ResolvePath(config.UserSeedFile);
            if (!File.Exists(path))
                throw new FileNotFoundException("User Seed file not found.", path);

            var seed = File.ReadAllLines(path)
                .Select(x => x.Trim())
                .FirstOrDefault(x => x.StartsWith("SU", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(seed)) return seed;

            throw new InvalidDataException("No valid Seed found in file.");
        }

        throw new InvalidOperationException("UserSeed not configured.");
    }

    private static string ResolvePath(string path)
    {
        return Path.IsPathRooted(path) ? path : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
    }
}
