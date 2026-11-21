using Modules.VDS.Core.Media;

internal static class ShareAccessChecks
{
    public static async Task RunAsync(Action<bool, string> check, string testRoot)
    {
        const string root = @"\\image-server\ImageShared\ITS015";
        const string samplePassword = "test-value-not-a-real-password";
        var values = new Dictionary<string, string?>
        {
            ["vds_media_storage_directory"] = root, ["vds_media_storage_username"] = "image-user",
            ["vds_media_storage_password"] = samplePassword, ["vds_media_storage_domain"] = "IMAGE-DOMAIN",
            ["unrelated"] = "not-used"
        };
        var settings = VdsImageShareSettings.FromValues(values);
        var oldKeys = VdsImageShareSettings.FromValues(new Dictionary<string, string?>
        {
            ["base_url_image"] = root, ["base_url_username"] = "post-inspection",
            ["base_url_password"] = samplePassword
        });
        check(oldKeys.UserName is null && oldKeys.Password is null && oldKeys.BasePath is null,
            "post-inspection SysOpConfig keys cannot provide VDS credentials");
        var rows = values.Where(x => x.Key.StartsWith("vds_")).Select(x => new Modules.VDS.Core.Entities.VdsConfig
        {
            TenantId = "tenant-a", GroupCode = "vds_config", Code = x.Key, Value = x.Value,
            Status = Shared.DTO.Enums.BaseEnums.StatusEnum.Enable
        }).ToList();
        var resolved = VdsImageShareSettings.ForRoot(rows, root + @"\IncidentImageState");
        check(resolved.UserName == settings.UserName && resolved.Password == samplePassword,
            "VDS root credentials cover both media and state directories on that share");
        check(VdsImageShareSettings.ForRoot(rows, @"\\other\share").UserName is null,
            "VDS account is not selected for a different configured share");
        rows.Single(x => x.Code == "vds_media_storage_username").Status = Shared.DTO.Enums.BaseEnums.StatusEnum.Disable;
        check(VdsImageShareSettings.ForRoot(rows, root).UserName is null, "disabled VDS username is not applied");
        rows.Single(x => x.Code == "vds_media_storage_username").Status = Shared.DTO.Enums.BaseEnums.StatusEnum.Enable;
        rows.AddRange(rows.ToArray().Select(x => new Modules.VDS.Core.Entities.VdsConfig
        {
            TenantId = "tenant-b", GroupCode = x.GroupCode, Code = x.Code,
            Value = x.Code == "vds_media_storage_password" ? "different-test-secret" : x.Value, Status = x.Status
        }));
        var ambiguous = false;
        try { VdsImageShareSettings.ForRoot(rows, root); }
        catch (InvalidOperationException) { ambiguous = true; }
        check(ambiguous, "conflicting tenant credentials for a shared root fail instead of selecting an arbitrary account");
        check(settings.BasePath == root && settings.UserName == "image-user" && settings.Password == samplePassword
            && settings.Domain == "IMAGE-DOMAIN", "share access uses existing VdsConfig keys without adding host credentials");
        check(!settings.ToString().Contains(samplePassword) && !settings.ToString().Contains("image-user"),
            "share settings diagnostic text does not disclose credentials");
        check(VdsWindowsNetworkFileIdentity.Account(settings) == ("image-user", "IMAGE-DOMAIN")
            && VdsWindowsNetworkFileIdentity.Account(new(root, @"OTHER\qualified", samplePassword, "ignored")) == ("qualified", "OTHER")
            && VdsWindowsNetworkFileIdentity.Account(new(root, "qualified@example.test", samplePassword, "ignored")) == ("qualified@example.test", null),
            "plain, domain-qualified and UPN accounts retain the correct Windows identity");
        check(VdsImageShareAccess.UncShare(root + @"\IncidentImageState\locks") == @"\\image-server\ImageShared"
            && VdsImageShareAccess.UncShare("//IMAGE-SERVER/ImageShared/ITS015/VdsImage") == @"\\IMAGE-SERVER\ImageShared"
            && VdsImageShareAccess.UncShare(@"D:\images") is null,
            "state and media subdirectories resolve to the same UNC share while local paths stay local");

        var identity = new RecordingIdentity();
        var loads = 0;
        var access = new VdsImageShareAccess(_ => { loads++; return Task.FromResult(settings); }, identity);
        var result = await access.RunAsync(root + @"\IncidentImageState\locks", async () =>
        {
            await Task.Yield();
            return identity.InIdentity;
        }, default);
        check(result && identity.Calls == 1 && !identity.InIdentity && identity.LastRoot == root + @"\IncidentImageState\locks",
            "UNC IO receives the actual configured root and stays in authenticated identity across await");
        await access.RunAsync(root + @"\VdsImage", () => Task.FromResult(true), default);
        check(loads == 1 && identity.Calls == 2, "state and full-image reads share the existing credentials and bounded memory cache");
        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => access.RunAsync(root, async () =>
        {
            await Task.Yield();
            if (!identity.InIdentity) throw new Exception("Lost async identity");
            return true;
        }, default)));
        check(loads == 1 && !identity.InIdentity, "concurrent file operations keep independent async identity scopes");
        var localCalls = identity.Calls;
        var localResult = await access.RunAsync(testRoot, () => Task.FromResult(!identity.InIdentity), default);
        check(localResult && identity.Calls == localCalls && loads == 1, "local files do not load or apply network credentials");
        var localOnly = new VdsImageShareAccess(_ => throw new Exception("Local IO must not query VdsConfig"), identity);
        check(await localOnly.RunAsync(testRoot, () => Task.FromResult(!identity.InIdentity), default),
            "local storage works without loading VDS share settings at all");

        var differentServer = await access.RunAsync(@"\\configured-vds-server\ImageShared\ITS015\IncidentImageState",
            () => Task.FromResult(!identity.InIdentity), default);
        check(differentServer && identity.Calls == localCalls,
            "a different configured server keeps existing host access without applying another share's credentials");
        var differentShare = await access.RunAsync(@"\\image-server\ConfiguredVdsShare\ITS015\VdsImage",
            () => Task.FromResult(!identity.InIdentity), default);
        check(differentShare && identity.Calls == localCalls,
            "a different configured share keeps host access and does not inherit the VDS account");
        foreach (var VDSPath in new string?[] { @"D:\VDS-images", "https://images.example.test/VDS", null })
        {
            var independentIdentity = new RecordingIdentity();
            var independentAccess = new VdsImageShareAccess(_ => Task.FromResult(
                new VdsImageShareSettings(VDSPath, settings.UserName, settings.Password, settings.Domain)), independentIdentity);
            var hostAccess = await independentAccess.RunAsync(root + @"\IncidentImageState",
                () => Task.FromResult(!independentIdentity.InIdentity), default);
            check(hostAccess && independentIdentity.Calls == 0,
                "UNC storage retains host access when VDS credentials have no matching UNC root: " + (VDSPath ?? "missing"));
        }
        var hostFailure = new IOException("Simulated host authentication failure", unchecked((int)0x8007052E));
        IOException? mismatch = null;
        try { await access.RunAsync<bool>(@"\\configured-vds-server\ImageShared\ITS015", () => Task.FromException<bool>(hostFailure), default); }
        catch (IOException ex) { mismatch = ex; }
        check(mismatch?.InnerException == hostFailure && mismatch.Message.Contains("different UNC share")
            && identity.Calls == localCalls && !mismatch.Message.Contains(samplePassword),
            "different-share authentication failure keeps the original error and explains account scope without disclosing secrets");

        var failure = new IOException("simulated access failure");
        Exception? propagated = null;
        try { await access.RunAsync<bool>(root, () => Task.FromException<bool>(failure), default); }
        catch (IOException ex) { propagated = ex; }
        check(ReferenceEquals(propagated, failure) && !identity.InIdentity,
            "file access failure restores identity and remains retryable instead of claiming image success");
        using var canceled = new CancellationTokenSource(); canceled.Cancel();
        var canceledCalls = identity.Calls;
        try { await access.RunAsync(root, () => Task.FromResult(true), canceled.Token); }
        catch (OperationCanceledException) { }
        check(identity.Calls == canceledCalls, "canceled file operation cannot open an authenticated session");

        var noAccountIdentity = new RecordingIdentity();
        var noAccount = new VdsImageShareAccess(_ => Task.FromResult(new VdsImageShareSettings(root, null, null, null)), noAccountIdentity);
        await noAccount.RunAsync(root, () => Task.FromResult(true), default);
        check(noAccountIdentity.Calls == 0, "UNC paths without a configured account retain the host's existing Windows identity");
        var conflicting = false;
        try { VdsImageShareSettings.FromValues(values.Concat([new KeyValuePair<string, string?>("vds_media_storage_password", "another-test-value")])); }
        catch (InvalidOperationException ex) { conflicting = !ex.Message.Contains(samplePassword) && !ex.Message.Contains("another-test-value"); }
        check(conflicting, "conflicting active share configuration is rejected without logging password values");
        var retryLoads = 0;
        var retryAccess = new VdsImageShareAccess(_ => ++retryLoads == 1
            ? Task.FromException<VdsImageShareSettings>(new IOException("temporary config failure")) : Task.FromResult(settings), new RecordingIdentity());
        try { await retryAccess.RunAsync(root, () => Task.FromResult(true), default); }
        catch (IOException) { }
        await retryAccess.RunAsync(root, () => Task.FromResult(true), default);
        check(retryLoads == 2, "failed configuration lookup is not cached and can recover on the next frame");

        var fileAccess = new RecordingFileAccess();
        var store = new VdsRegistrationImageStore(new() { StateDirectory = Path.Combine(testRoot, "authenticated-state") }, fileAccess);
        var time = DateTime.UtcNow;
        var source = new VdsRegistrationImageSource(10, time, "raw-camera", "10.0.0.1", "Normal");
        await store.RegisterAsync("tenant", "incident", time, time, source, default);
        check(fileAccess.Paths.Count == 1 && fileAccess.MaxDepth == 1, "register authenticates before lock/read/write with a single identity scope");
        var waiting = (await store.ReadAsync("tenant", "incident", default))!;
        var paths = await store.PendingBatchAsync(null, default);
        var queued = await store.ReadPendingFileAsync(paths.Single(), default);
        check(queued == waiting && fileAccess.Paths.Count == 4,
            "registration read, background directory scan and pending-file read all pass through file authentication");
        await using (await store.LockAsync("tenant", "incident", default))
            await store.SaveAsync(waiting with { Status = "MissingSource" }, default);
        check((await store.ReadAsync("tenant", "incident", default))?.Status == "MissingSource" && fileAccess.Paths.Count == 7,
            "external registration lock and terminal-state save both retain authenticated access");
        var decision = new VdsAutomationImageDecision("tenant", "incident", "run", 2, time.AddSeconds(1), time, "camera");
        await using (await store.LockStageAsync("tenant", "incident", 2, decision.CaptureTime, default))
            await store.SaveStageAsync(decision, default);
        check(await store.ReadStageAsync("tenant", "incident", 2, decision.CaptureTime, default) == decision
            && fileAccess.Paths.Count == 10 && fileAccess.MaxDepth == 1,
            "automation stage lock/read/save authenticate separately and do not nest network sessions");
        var originalCount = fileAccess.Paths.Count;
        var media = Path.Combine(testRoot, "authenticated-media");
        var imported = await store.WithConfiguredRootAccessAsync(media,
            () => VdsRegistrationImageFiles.ImportAsync(waiting, media, Path.Combine(testRoot, "authenticated-destination"), time, default), default);
        check(imported.Status == "WaitingForSource" && fileAccess.Paths.Count == originalCount + 1 && fileAccess.Paths.Last() == media,
            "raw full-image import uses the same access wrapper and missing full still waits only for registration");
        var json = Directory.EnumerateFiles(store.Root, "*.json", SearchOption.AllDirectories).Select(File.ReadAllText);
        check(json.All(text => !text.Contains(samplePassword) && !text.Contains("vds_media_storage_password")),
            "registration and stage JSON persist no share credentials");
    }

    private sealed class RecordingIdentity : IVdsNetworkFileIdentity
    {
        private readonly AsyncLocal<bool> _active = new();
        public bool InIdentity => _active.Value;
        public int Calls;
        public string? LastRoot;
        public async Task<T> RunAsync<T>(string configuredRoot, VdsImageShareSettings settings, Func<Task<T>> operation, CancellationToken ct)
        {
            LastRoot = configuredRoot;
            Interlocked.Increment(ref Calls);
            _active.Value = true;
            try { return await operation(); }
            finally { _active.Value = false; }
        }
    }

    private sealed class RecordingFileAccess : IVdsImageFileAccess
    {
        private readonly AsyncLocal<int> _depth = new();
        public readonly List<string> Paths = [];
        public int MaxDepth;
        public async Task<T> RunAsync<T>(string path, Func<Task<T>> operation, CancellationToken ct)
        {
            Paths.Add(path);
            _depth.Value++;
            MaxDepth = Math.Max(MaxDepth, _depth.Value);
            try { return await operation(); }
            finally { _depth.Value--; }
        }
    }
}
