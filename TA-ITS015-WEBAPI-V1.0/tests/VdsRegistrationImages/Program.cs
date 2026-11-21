using Modules.VDS.Core.Media;

var checks = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; Console.WriteLine("PASS: " + name); }
var testRoot = Path.Combine(Path.GetTempPath(), "vds-registration-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(testRoot);
try
{
    var now = DateTime.UtcNow;
    var detected = new DateTime(2026, 10, 1, 3, 12, 45, 123, DateTimeKind.Utc);
    var sourceB = new VdsRegistrationImageSource(123456, detected, "camera-B", "10.0.0.2", "Normal");
    var store = new VdsRegistrationImageStore(new() { StateDirectory = Path.Combine(testRoot, "state"), WaitTimeoutSeconds = 600, BatchSize = 1 });
    var media = Path.Combine(testRoot, "media");
    var destination = Path.Combine(testRoot, "incident");
    Check(VdsMediaPaths.IsVdsRegistration(" VDS ", 1), "VDS registration selected independently of process mode");
    Check(!VdsMediaPaths.IsVdsRegistration("user", 1), "manual registration uses existing camera branch");
    Check(!VdsMediaPaths.IsVdsRegistration("vds", 2) && !VdsMediaPaths.IsVdsRegistration("vds", 3)
        && !VdsMediaPaths.IsVdsRegistration("vds", 4), "registration importer never handles verification, processing or finish");
    Check(VdsMediaPaths.Branch("normal") == "data", "writer and reader normalize normal event case");
    Check(VdsMediaPaths.Branch("Regular") == "data", "legacy normal event spelling keeps writer compatibility");
    Check(VdsMediaPaths.Branch("WrongLane") == "event", "incident full stays in event branch");
    Check(VdsMediaPaths.CameraFolder("../\\") != "..", "camera path cannot escape root");
    await store.RegisterAsync("tenant-A", "incident-1", now, now, sourceB, default);
    var waiting = (await store.ReadAsync("tenant-A", "incident-1", default))!;
    Check(waiting.Source == sourceB && waiting.Status == "WaitingForSource", "raw B identity and deadline persisted without columns");
    var delayed = await VdsRegistrationImageFiles.ImportAsync(waiting, media, destination, now, default);
    Check(delayed.Status == "WaitingForSource" && !Directory.Exists(destination), "missing full waits and does not fabricate an image");
    // Same timestamp at camera A must not satisfy B's registration request.
    var hour = Path.Combine(media, "data", detected.ToString("yyyyMM"), detected.ToString("ddHH"));
    var cameraA = Path.Combine(hour, "10.0.0.1"); Directory.CreateDirectory(cameraA);
    var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR4nGNgAAIAAAUAAXpeqz8AAAAASUVORK5CYII=");
    var filename = $"{detected:yyyyMMddHHmmssfff}_f.png";
    await File.WriteAllBytesAsync(Path.Combine(cameraA, filename), png);
    Check(VdsMediaPaths.FindFull(media, sourceB) is null, "never borrow another camera's full at the same millisecond");
    await File.WriteAllBytesAsync(Path.Combine(hour, filename), png);
    Check(VdsMediaPaths.FindFull(media, sourceB) is null, "anonymous legacy full cannot establish camera ownership");
    // Replay does not extend deadline or substitute another camera.
    await store.RegisterAsync("tenant-A", "incident-1", now.AddSeconds(30), now.AddSeconds(30), sourceB with { EquipmentId = "camera-A" }, default);
    Check(await store.ReadAsync("tenant-A", "incident-1", default) == waiting, "replay preserves original source and wait deadline");
    var restarted = new VdsRegistrationImageStore(store.Options);
    Check((await restarted.ReadAsync("tenant-A", "incident-1", default)) == waiting, "restart restores pending source identity");
    Check(await restarted.ReadAsync("tenant-B", "incident-1", default) is null, "tenant isolation for identical incident IDs");
    var cameraB = Path.Combine(hour, "10.0.0.2"); Directory.CreateDirectory(cameraB);
    await File.WriteAllBytesAsync(Path.Combine(cameraB, filename), png);
    var ready = await VdsRegistrationImageFiles.ImportAsync(waiting, media, destination, now.AddSeconds(20), default);
    Check(ready.Status == "Ready" && ready.ContentType == "image/png", "delayed full imports with its real MIME");
    Check((await File.ReadAllBytesAsync(Path.Combine(destination, ready.FileName!))).SequenceEqual(png), "registration keeps the exact full bytes");
    Check(Directory.GetFiles(destination).Length == 1 && ready.FileName!.EndsWith("_1.png"), "only registration file created, no stage 2/3/4 copies");
    await using (var held = await store.LockAsync("tenant-A", "incident-1", default)) { await store.SaveAsync(ready, default); }
    Check((await store.PendingBatchAsync(null, default)).Count == 0 && (await store.ReadAsync("tenant-A", "incident-1", default))?.Status == "Ready", "completed import leaves pending scan and survives restart");
    var unchanged = await VdsRegistrationImageFiles.ImportAsync(ready, media, destination, now.AddHours(1), default);
    Check(unchanged == ready, "terminal image is not replaced on replay or expiry");
    var expired = await VdsRegistrationImageFiles.ImportAsync(waiting, media, destination, waiting.WaitUntilUtc, default);
    Check(expired.Status == "MissingSource", "deadline is bounded even if full appears too late");
    var badSource = sourceB with { DetectTimeUtc = detected.AddMilliseconds(1) };
    await File.WriteAllTextAsync(Path.Combine(cameraB, $"{badSource.DetectTimeUtc:yyyyMMddHHmmssfff}_f.png"), "not an image");
    var invalid = await VdsRegistrationImageFiles.ImportAsync(waiting with { Source = badSource }, media, destination, now, default);
    Check(invalid.Error == "InvalidFullImage" && invalid.Status == "WaitingForSource", "non-image full payload is never published as registration evidence");
    // Rotation prevents a missing early image starving another incident.
    await store.RegisterAsync("tenant-A", "incident-2", now, now, sourceB, default);
    await store.RegisterAsync("tenant-A", "incident-3", now, now, sourceB, default);
    var batch1 = await store.PendingBatchAsync(null, default); var batch2 = await store.PendingBatchAsync(batch1[0], default);
    Check(batch1[0] != batch2[0], "pending scan rotates across requests");
    await store.RegisterAsync("tenant-A", "historical", now.AddHours(-1), now.AddHours(-1), sourceB, default);
    Check(await store.ReadAsync("tenant-A", "historical", default) is null, "historical replay does not override legacy registration photos");

    var policy = new VdsImageReusePolicy("tenant-A", "event-type-1", 60);
    var frozen = VdsImageReusePolicy.Freeze("{\"WarningDelaySeconds\":30,\"TrafficPlanId\":\"plan-1\"}", policy);
    var parsed = VdsImageReusePolicy.Read(frozen, "tenant-A", "event-type-1");
    Check(parsed == policy && frozen.Contains("\"WarningDelaySeconds\":30") && frozen.Contains("\"TrafficPlanId\":\"plan-1\""), "policy snapshot preserves existing config root and delay independently of reuse threshold");
    Check(VdsImageReusePolicy.Read("{\"WarningDelaySeconds\":30}", "tenant-A", "event-type-1") is null,
        "old automation runs without image policy do not enable reuse");
    Check(VdsImageReusePolicy.Read("not-json", "tenant-A", "event-type-1") is null
        && VdsImageReusePolicy.Read("{\"VdsImageReusePolicy\":{\"MaxAgeSeconds\":\"bad\"}}", "tenant-A", "event-type-1") is null,
        "invalid snapshot safely disables reuse");
    Check(VdsImageReusePolicy.Read(frozen, "tenant-B", "event-type-1") is null
        && VdsImageReusePolicy.Read(frozen, "tenant-A", "other-event") is null, "policy cannot cross tenant or event type");
    Check(VdsImageReusePolicy.Read(VdsImageReusePolicy.Freeze("{}", policy with { MaxAgeSeconds = 0 }), "tenant-A", "event-type-1") is null,
        "disabled or missing setting produces a disabled snapshot");
    Check(VdsImageReusePolicy.Read(VdsImageReusePolicy.Freeze("{}", policy with { Version = 2 }), "tenant-A", "event-type-1") is null,
        "unknown policy version does not enable reuse");
    Check(VdsImageReusePolicy.ParseSeconds("60") == 60 && VdsImageReusePolicy.ParseSeconds(" 60 ") == 60,
        "reuse setting is positive integer seconds");
    Check(new[] { "", "0", "-1", "60s", "1.5", "2147483648", "+60" }.All(value => VdsImageReusePolicy.ParseSeconds(value) is null),
        "invalid seconds cannot silently become minutes or an unbounded value");
    Check(policy.CanReuse(true, " VDS ", 2, "tenant-A", "event-type-1", now, now.AddSeconds(59)), "automation verification below threshold is eligible");
    Check(!policy.CanReuse(true, "vds", 2, "tenant-A", "event-type-1", now, now.AddSeconds(60))
        && !policy.CanReuse(true, "vds", 3, "tenant-A", "event-type-1", now, now.AddSeconds(61)), "equal or above threshold uses cameras");
    var fiveSeconds = policy with { MaxAgeSeconds = 5 };
    Check(fiveSeconds.CanReuse(true, "vds", 2, "tenant-A", "event-type-1", now, now.AddMilliseconds(4999))
        && !fiveSeconds.CanReuse(true, "vds", 2, "tenant-A", "event-type-1", now, now.AddSeconds(5))
        && !fiveSeconds.CanReuse(true, "vds", 3, "tenant-A", "event-type-1", now, now.AddMilliseconds(5001)),
        "configured five-second policy reuses strictly below five seconds and retains event ID in frozen snapshots");
    Check(!policy.CanReuse(true, "vds", 2, "tenant-A", "event-type-1", now, now.AddSeconds(-1)), "negative event age is never eligible");
    Check(!policy.CanReuse(false, "vds", 2, "tenant-A", "event-type-1", now, now.AddSeconds(20))
        && !policy.CanReuse(true, "user", 2, "tenant-A", "event-type-1", now, now.AddSeconds(20)), "manual VDS steps and non-VDS incidents retain cameras");
    Check(!policy.CanReuse(true, "vds", 1, "tenant-A", "event-type-1", now, now.AddSeconds(20))
        && !policy.CanReuse(true, "vds", 4, "tenant-A", "event-type-1", now, now.AddSeconds(20)), "reuse policy does not apply to registration or finish");
    Check(policy.CanReuse(true, "vds", 2, "tenant-A", "event-type-1", now, now.AddSeconds(20))
        && !policy.CanReuse(true, "vds", 3, "tenant-A", "event-type-1", now, now.AddSeconds(80)), "verification and processing ages are independent");
    Check(!policy.CanReuse(true, "vds", 2, "tenant-B", "event-type-1", now, now.AddSeconds(20))
        && !policy.CanReuse(true, "vds", 2, "tenant-A", "other-event", now, now.AddSeconds(20)), "eligible step still requires matching tenant and event type");
    var editedLivePolicy = policy with { MaxAgeSeconds = 1 };
    Check(parsed!.CanReuse(true, "vds", 3, "tenant-A", "event-type-1", now, now.AddSeconds(20))
        && !editedLivePolicy.CanReuse(true, "vds", 3, "tenant-A", "event-type-1", now, now.AddSeconds(20)), "live config edit does not mutate frozen run policy");

    var stageTime = now.AddSeconds(20);
    var vdsStep2 = new VdsAutomationImageDecision("tenant-A", "incident-1", "run-1", 2, stageTime, now, "vds")
    {
        Source = sourceB, ContentType = "image/png",
        FileName = VdsRegistrationImageFiles.FileName("incident-1", sourceB.EquipmentId, 2, ".png")
    };
    Check(VdsRegistrationImageFiles.IsReady(ready, destination) && !VdsRegistrationImageFiles.IsReady(waiting, destination),
        "existing raw full does not make a pending registration Ready for reuse");
    var stageDirectory = Path.Combine(testRoot, "stage-images");
    var step2 = await VdsRegistrationImageFiles.CopyToStageAsync(vdsStep2, destination, stageDirectory, default);
    Check(step2.Status == "Ready" && step2.PictureType == 2 && step2.Source == sourceB && step2.ContentType == "image/png"
        && (await File.ReadAllBytesAsync(Path.Combine(stageDirectory, step2.FileName!))).SequenceEqual(png), "verification copy retains raw camera B, stage identity, exact bytes and MIME");
    var vdsStep3 = vdsStep2 with { PictureType = 3, CaptureTime = now.AddSeconds(30),
        FileName = VdsRegistrationImageFiles.FileName("incident-1", sourceB.EquipmentId, 3, ".png") };
    var step3 = await VdsRegistrationImageFiles.CopyToStageAsync(vdsStep3, destination, stageDirectory, default);
    Check(step3.Status == "Ready" && step3.FileName!.EndsWith("_3.png") && Directory.GetFiles(stageDirectory).Length == 2,
        "processing has its own immutable stage image, no finish copy");
    var missingCopy = await VdsRegistrationImageFiles.CopyToStageAsync(vdsStep2, Path.Combine(testRoot, "not-yet-ready"), stageDirectory, default);
    Check(missingCopy.ImageSource == "camera" && missingCopy.Reason == "RegistrationFileMissing",
        "unavailable registration immediately chooses camera, no wait or raw-full lookup");
    var invalidCopy = await VdsRegistrationImageFiles.CopyToStageAsync(vdsStep2 with { FileName = "../escape.png" }, destination, stageDirectory, default);
    Check(invalidCopy.ImageSource == "camera", "unsafe stage filename cannot escape image storage");

    await using (var held = await store.LockStageAsync("tenant-A", "incident-1", 2, stageTime, default))
    {
        await store.SaveStageAsync(missingCopy, default);
        var keptCamera = await store.SaveStageAsync(vdsStep2, default);
        Check(keptCamera.ImageSource == "camera", "later Ready full cannot change a stage already assigned CCTV");
        await store.SaveStageAsync(keptCamera with { Status = "Ready" }, default);
        var completedCamera = await store.SaveStageAsync(keptCamera with { Status = "CaptureFailed" }, default);
        Check(completedCamera.Status == "Ready", "replay cannot replace completed camera evidence");
    }
    Check((await restarted.ReadStageAsync("tenant-A", "incident-1", 2, stageTime, default))?.ImageSource == "camera",
        "camera fallback choice survives restart");
    Check(await restarted.ReadStageAsync("tenant-B", "incident-1", 2, stageTime, default) is null,
        "stage file reads enforce tenant isolation");
    Check(await restarted.ReadStageAsync("tenant-A", "incident-1", 2, stageTime.AddSeconds(1), default) is null,
        "another transition timestamp cannot inherit an old choice");
    await using (var held = await store.LockStageAsync("tenant-A", "incident-1", 3, step3.CaptureTime, default))
    {
        await store.SaveStageAsync(step3, default);
        var completedVds = await store.SaveStageAsync(step3.UseCamera("late-change"), default);
        Check(completedVds == step3, "completed VDS stage is immutable on replay");
    }
    Check((await store.ReadStageAsync("tenant-A", "incident-1", 2, stageTime, default))?.ImageSource == "camera"
        && (await store.ReadStageAsync("tenant-A", "incident-1", 3, step3.CaptureTime, default))?.ImageSource == "vds",
        "processing may reuse a later Ready registration while verification keeps CCTV");
    await using (var held = await store.LockAsync("tenant-A", "incident-1", default)) { await store.SaveAsync(ready, default); }
    Check((await store.ReadStageAsync("tenant-A", "incident-1", 2, stageTime, default))?.ImageSource == "camera",
        "registration worker writes cannot overwrite stage decisions");
    var runMismatchRejected = false;
    await using (var held = await store.LockStageAsync("tenant-A", "incident-1", 2, stageTime, default))
    {
        try { await store.SaveStageAsync(missingCopy with { RunId = "other-run" }, default); }
        catch (InvalidDataException) { runMismatchRejected = true; }
    }
    Check(runMismatchRejected, "another automation run cannot replace a frozen transition choice");
    var slots = Path.Combine(testRoot, "slots");
    Directory.CreateDirectory(slots);
    var evidence = Path.Combine(slots, "full.png");
    await File.WriteAllBytesAsync(evidence, png);
    var registrationSlot = Path.Combine(slots, VdsRegistrationImageFiles.IncidentSlotFileName("incident", "ptz", 1, ".png"));
    var verificationSlot = Path.Combine(slots, VdsRegistrationImageFiles.IncidentSlotFileName("incident", "ptz", 2, ".png"));
    await File.WriteAllTextAsync(registrationSlot, "old registration");
    await File.WriteAllTextAsync(verificationSlot, "verified camera evidence");
    await VdsRegistrationImageFiles.PublishToIncidentSlotsAsync(evidence, "incident", ["ptz", "ptz2"], 1, default);
    Check((await File.ReadAllBytesAsync(registrationSlot)).SequenceEqual(png)
        && (await File.ReadAllBytesAsync(Path.Combine(slots, "incident_ptz2_1.png"))).SequenceEqual(png),
        "VDS publishing replaces registration slots for the incident cameras");
    Check(await File.ReadAllTextAsync(verificationSlot) == "verified camera evidence",
        "registration slot publishing preserves other workflow stages");
    var rejectedFinish = false;
    try { await VdsRegistrationImageFiles.PublishToIncidentSlotsAsync(evidence, "incident", ["ptz"], 4, default); }
    catch (ArgumentOutOfRangeException) { rejectedFinish = true; }
    Check(rejectedFinish, "VDS slot publishing cannot replace completion images");
    await ShareAccessChecks.RunAsync(Check, testRoot);
    Console.WriteLine($"SUMMARY: {checks} checks passed. No SQL, QuestDB, NATS or camera used.");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}
finally
{
    var absoluteRoot = Path.GetFullPath(testRoot);
    var tempParent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    if (!absoluteRoot.StartsWith(tempParent, StringComparison.OrdinalIgnoreCase)
        || !Path.GetFileName(absoluteRoot).StartsWith("vds-registration-", StringComparison.Ordinal))
        throw new InvalidOperationException("Refusing cleanup outside the isolated test directory.");
    Directory.Delete(absoluteRoot, recursive: true);
}
