
namespace Shared.QuestHa.Infrastructure.Services.Journal.Statements;

/// <summary>
/// Author: KienLT
/// Description: SQL journal giữ nguyên transaction, khoá và điều kiện CAS của từng thao tác.
/// Created date: 31/10/2026
/// </summary>
internal static class QuestJournalSql
{
    // Append
    internal const string AppendBatchSql = """
        SET NOCOUNT ON; SET XACT_ABORT ON;
        DECLARE @b TABLE (Ord int NOT NULL PRIMARY KEY, TargetTable nvarchar(128) NOT NULL, EventKey nvarchar(256) NOT NULL,
                          DesignatedTs datetime2(7) NOT NULL, Payload nvarchar(max) NOT NULL, SchemaVer int NOT NULL,
                          Version bigint NULL, Sup bit NOT NULL DEFAULT (0));
        INSERT @b (Ord, TargetTable, EventKey, DesignatedTs, Payload, SchemaVer, Version)
        SELECT j.o, j.t, j.k, CONVERT(datetime2(7), j.ts, 126), j.p, j.s, j.v
        FROM OPENJSON(@Batch) WITH (o int '$.o', t nvarchar(128) '$.t', k nvarchar(256) '$.k', ts varchar(27) '$.ts',
                                    p nvarchar(max) '$.p', s int '$.s', v bigint '$.v') AS j;
        DECLARE @out TABLE (Seq bigint NOT NULL, CreatedUtc datetime2(7) NOT NULL);
        BEGIN TRAN;
        DECLARE @lock int;
        EXEC @lock = sp_getapplock @Resource = N'QuestOutbox.Append', @LockMode = 'Exclusive',
                                   @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @lock < 0 BEGIN ROLLBACK; THROW 51000, N'QuestOutbox append lock timeout', 1; END;
        UPDATE b SET Sup = 1
        FROM @b AS b
        WHERE b.Version IS NOT NULL
          AND (EXISTS (SELECT 1 FROM dbo.QuestOutbox AS o
                       WHERE o.TargetTable = b.TargetTable AND o.EventKey = b.EventKey AND o.Superseded = 0 AND o.Version > b.Version)
               OR EXISTS (SELECT 1 FROM @b AS p
                          WHERE p.TargetTable = b.TargetTable AND p.EventKey = b.EventKey AND p.Ord < b.Ord AND p.Version > b.Version));
        INSERT dbo.QuestOutbox (TargetTable, EventKey, DesignatedTs, Payload, SchemaVer, Version, Superseded)
        OUTPUT inserted.Seq, inserted.CreatedUtc INTO @out
        SELECT TargetTable, EventKey, DesignatedTs, Payload, SchemaVer, Version, Sup FROM @b ORDER BY Ord;
        COMMIT;
        SELECT b.Ord, o.Seq, b.Sup AS Superseded, o.CreatedUtc
        FROM (SELECT Ord, Sup, ROW_NUMBER() OVER (ORDER BY Ord) AS rn FROM @b) AS b
        JOIN (SELECT Seq, CreatedUtc, ROW_NUMBER() OVER (ORDER BY Seq) AS rn FROM @out) AS o ON o.rn = b.rn
        ORDER BY b.Ord;
        """;

    internal const string AppendSingle = """
        SET NOCOUNT ON; SET XACT_ABORT ON;
        DECLARE @out TABLE (Seq bigint, Superseded bit, CreatedUtc datetime2(7));
        BEGIN TRAN;
        DECLARE @lock int;
        EXEC @lock = sp_getapplock @Resource = N'QuestOutbox.Append', @LockMode = 'Exclusive',
                                   @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @lock < 0 BEGIN ROLLBACK; THROW 51000, N'QuestOutbox append lock timeout', 1; END;
        DECLARE @sup bit = 0;
        IF @Version IS NOT NULL AND EXISTS (
            SELECT 1 FROM dbo.QuestOutbox
            WHERE TargetTable = @TargetTable AND EventKey = @EventKey AND Superseded = 0 AND Version > @Version)
            SET @sup = 1;
        INSERT dbo.QuestOutbox (TargetTable, EventKey, DesignatedTs, Payload, SchemaVer, Version, Superseded)
        OUTPUT inserted.Seq, inserted.Superseded, inserted.CreatedUtc INTO @out
        VALUES (@TargetTable, @EventKey, @DesignatedTs, @Payload, @SchemaVer, @Version, @sup);
        COMMIT;
        SELECT Seq, Superseded, CreatedUtc FROM @out;
        """;

    internal const string GetLatest =
        "SELECT TOP (@take) Seq, TargetTable, EventKey, DesignatedTs, Payload, SchemaVer, Version, Superseded, CreatedUtc FROM " +
        "dbo.QuestOutbox ORDER BY Seq DESC";

    internal const string ReadBatch =
        "SELECT TOP (@max) Seq, TargetTable, EventKey, DesignatedTs, Payload, SchemaVer, Version, Superseded, CreatedUtc FROM " +
        "dbo.QuestOutbox WHERE Seq > @after AND RowVer < MIN_ACTIVE_ROWVERSION() ORDER BY Seq";

    internal const string ReadWindow =
        "SELECT Seq, TargetTable, EventKey, DesignatedTs, Payload, SchemaVer, Version, Superseded, CreatedUtc FROM " +
        "dbo.QuestOutbox WHERE TargetTable = @t AND DesignatedTs >= @from AND DesignatedTs < @to AND Superseded = 0 AND Seq <= " +
        "@maxSeq AND RowVer < MIN_ACTIVE_ROWVERSION() ORDER BY Seq";

    internal const string GetCreatedUtc = "SELECT CreatedUtc FROM dbo.QuestOutbox WHERE Seq = @seq";

    // Cursor
    internal const string GetCursor = """
        SET NOCOUNT ON;
        IF NOT EXISTS (SELECT 1 FROM dbo.QuestNodeCursor WHERE Node = @node)
            INSERT dbo.QuestNodeCursor (Node, LastSeq, SafeSeq, UpdatedBy)
            SELECT @node, 0, 0, N'init' WHERE NOT EXISTS (SELECT 1 FROM dbo.QuestNodeCursor WITH (UPDLOCK, HOLDLOCK) WHERE Node = @node);
        SELECT Node, LastSeq, SafeSeq FROM dbo.QuestNodeCursor WHERE Node = @node;
        """;

    internal const string TryAdvanceCursor = """
        SET NOCOUNT ON;
        UPDATE dbo.QuestNodeCursor
           SET LastSeq = @new,
               SafeSeq = CASE WHEN @safe > SafeSeq AND @safe <= @new THEN @safe ELSE SafeSeq END,
               UpdatedUtc = SYSUTCDATETIME(), UpdatedBy = N'replicator'
         WHERE Node = @node AND LastSeq = @expected;
        SELECT @@ROWCOUNT;
        """;

    internal const string TryRaiseSafeSeq = "UPDATE dbo.QuestNodeCursor SET SafeSeq = @safe WHERE Node = @node AND SafeSeq < @safe AND LastSeq >= @safe";

    internal const string RewindToSafe = """
        SET NOCOUNT ON;
        DECLARE @r TABLE (OldSeq bigint, NewSeq bigint);
        UPDATE dbo.QuestNodeCursor
           SET LastSeq = SafeSeq, UpdatedUtc = SYSUTCDATETIME(), UpdatedBy = N'rewind'
        OUTPUT deleted.LastSeq, inserted.LastSeq INTO @r
         WHERE Node = @node AND SafeSeq < LastSeq;
        SELECT OldSeq, NewSeq FROM @r;
        """;

    internal const string LowerCursor = """
        SET NOCOUNT ON;
        UPDATE dbo.QuestNodeCursor
           SET LastSeq = @seq,
               SafeSeq = CASE WHEN SafeSeq > @seq THEN @seq ELSE SafeSeq END,
               UpdatedUtc = SYSUTCDATETIME(), UpdatedBy = @by
         WHERE Node = @node AND LastSeq > @seq;
        SELECT LastSeq FROM dbo.QuestNodeCursor WHERE Node = @node;
        """;

    internal const string SetCursor = "UPDATE dbo.QuestNodeCursor SET LastSeq = @seq, SafeSeq = @seq, UpdatedUtc = SYSUTCDATETIME(), UpdatedBy = @by WHERE Node = @node";

    // Status
    internal const string StatsCursors = "SELECT Node, LastSeq, SafeSeq FROM dbo.QuestNodeCursor";

    internal const string StatsHead = """
        SET NOCOUNT ON;
        SELECT
          (SELECT ISNULL(MAX(Seq), 0) FROM dbo.QuestOutbox) AS MaxSeq,
          (SELECT COUNT_BIG(*) FROM dbo.QuestOutbox) AS TotalRows,
          (SELECT COUNT(*) FROM dbo.QuestQuarantine WHERE ResolvedUtc IS NULL) AS QuarantineCount;
        """;

    internal const string ReadLagCore = """
        SET NOCOUNT ON;
        DECLARE @max bigint = ISNULL((SELECT TOP (1) Seq FROM dbo.QuestOutbox
                                      WHERE RowVer < MIN_ACTIVE_ROWVERSION() ORDER BY Seq DESC), 0);
        DECLARE @oldest datetime2(7) = (SELECT TOP (1) CreatedUtc FROM dbo.QuestOutbox
                                        WHERE Seq > @cursor AND RowVer < MIN_ACTIVE_ROWVERSION() ORDER BY Seq);
        SELECT @max AS MaxSeq,
               CASE WHEN @max > @cursor THEN @max - @cursor ELSE 0 END AS Pending,
               CAST(ISNULL(DATEDIFF_BIG(MILLISECOND, @oldest, SYSUTCDATETIME()), 0) AS float) / 1000.0 AS AgeSeconds;
        """;

    internal const string WriteStatus = """
        SET NOCOUNT ON;
        MERGE dbo.QuestNodeStatus WITH (HOLDLOCK) AS t
        USING (SELECT @node AS Node) AS src ON t.Node = src.Node
        WHEN MATCHED THEN UPDATE SET
            State = @state, Paused = @paused, FenceReason = @fence, WalSuspended = @wal,
            LastSeq = @lastSeq, MaxSeq = @maxSeq, LagSeq = @lagSeq, LagSeconds = @lagSec,
            HeartbeatUtc = SYSUTCDATETIME(), ReadySinceUtc = @readySince, LastDeliveredUtc = @delivered,
            LastError = @err, LastErrorUtc = @errUtc
        WHEN NOT MATCHED THEN INSERT
            (Node, State, Paused, FenceReason, WalSuspended, LastSeq, MaxSeq, LagSeq, LagSeconds,
             HeartbeatUtc, ReadySinceUtc, LastDeliveredUtc, LastError, LastErrorUtc)
            VALUES (@node, @state, @paused, @fence, @wal, @lastSeq, @maxSeq, @lagSeq, @lagSec,
             SYSUTCDATETIME(), @readySince, @delivered, @err, @errUtc);
        """;

    internal const string ReadStatuses = """
        SELECT Node, State, Paused, FenceReason, WalSuspended, LastSeq, MaxSeq, LagSeq, LagSeconds,
               ReadySinceUtc, LastDeliveredUtc, LastError, LastErrorUtc, LastReconcileOk,
               DATEDIFF_BIG(MILLISECOND, HeartbeatUtc, SYSUTCDATETIME()) AS HeartbeatAgeMs,
               DATEDIFF_BIG(MILLISECOND, LastReconcileUtc, SYSUTCDATETIME()) AS ReconcileAgeMs
        FROM dbo.QuestNodeStatus
        """;

    internal const string WriteReconcile = """
        SET NOCOUNT ON;
        MERGE dbo.QuestNodeStatus WITH (HOLDLOCK) AS t
        USING (SELECT @node AS Node) AS src ON t.Node = src.Node
        WHEN MATCHED THEN UPDATE SET LastReconcileUtc = SYSUTCDATETIME(), LastReconcileOk = @ok
        WHEN NOT MATCHED THEN INSERT (Node, LastReconcileUtc, LastReconcileOk) VALUES (@node, SYSUTCDATETIME(), @ok);
        """;

    internal const string GetRecovery = """
        SET NOCOUNT ON;
        MERGE dbo.QuestNodeStatus WITH (HOLDLOCK) AS t USING (SELECT @node AS Node) AS src ON t.Node = src.Node WHEN NOT MATCHED THEN INSERT (Node) VALUES (@node);
        SELECT RecoveryGeneration, RecoveryPhase, RecoveryCoveredUtc, ReconciledGeneration, LastReconcileUtc, LastReconcileOk, ManualFence, SYSUTCDATETIME() AS SqlNowUtc FROM dbo.QuestNodeStatus WHERE Node = @node;
        """;

    internal const string BeginRecovery = """
        SET NOCOUNT ON;
        MERGE dbo.QuestNodeStatus WITH (HOLDLOCK) AS t USING (SELECT @node AS Node) AS src ON t.Node = src.Node WHEN NOT MATCHED THEN INSERT (Node) VALUES (@node);
        UPDATE dbo.QuestNodeStatus SET RecoveryGeneration = RecoveryGeneration + 1, RecoveryPhase = 1, RecoveryCoveredUtc = NULL, ReconciledGeneration = NULL OUTPUT inserted.RecoveryGeneration WHERE Node = @node;
        """;

    internal const string MarkRecoveryCovered =
        "UPDATE dbo.QuestNodeStatus SET RecoveryPhase = 2, RecoveryCoveredUtc = SYSUTCDATETIME() WHERE Node = @node AND " +
        "RecoveryGeneration = @gen AND RecoveryPhase = 1";

    internal const string CompleteRecovery =
        "UPDATE dbo.QuestNodeStatus SET RecoveryPhase = 0 WHERE Node = @node AND RecoveryGeneration = @gen AND RecoveryPhase = 2 " +
        "AND ReconciledGeneration = @gen AND LastReconcileOk = 1 AND LastReconcileUtc > RecoveryCoveredUtc";

    internal const string SetManualFence = """
        SET NOCOUNT ON;
        MERGE dbo.QuestNodeStatus WITH (HOLDLOCK) AS t USING (SELECT @node AS Node) AS src ON t.Node = src.Node WHEN NOT MATCHED THEN INSERT (Node) VALUES (@node);
        UPDATE dbo.QuestNodeStatus SET ManualFence = @fenced WHERE Node = @node;
        """;

    internal const string TryClearManualFence = """
        UPDATE dbo.QuestNodeStatus SET ManualFence=0
        WHERE Node=@node AND RecoveryGeneration=@gen AND (RecoveryPhase=0 OR @ready=1);
        """;

    internal const string CertifyReconcile = """
        SET NOCOUNT ON;
        MERGE dbo.QuestNodeStatus WITH (HOLDLOCK) AS t USING (SELECT @node AS Node) AS src ON t.Node = src.Node WHEN NOT MATCHED THEN INSERT (Node) VALUES (@node);
        UPDATE dbo.QuestNodeStatus
        SET LastReconcileUtc = SYSUTCDATETIME(),
            ReconciledGeneration = @gen,
            LastReconcileOk = CASE WHEN @ok = 1 AND NOT EXISTS (
                    SELECT 1
                    FROM dbo.QuestQuarantine q
                    LEFT JOIN dbo.QuestOutbox o ON o.Seq = q.Seq
                    WHERE q.Node = @node AND q.ResolvedUtc IS NULL AND ISNULL(o.Superseded, 0) = 0
                      AND EXISTS (
                          SELECT 1 FROM dbo.QuestNodeStatus n2
                          WHERE NOT EXISTS (
                              SELECT 1 FROM dbo.QuestQuarantine q2
                              WHERE q2.Seq = q.Seq AND q2.Node = n2.Node AND q2.ResolvedUtc IS NULL)))
                THEN 1 ELSE 0 END
        OUTPUT CAST(inserted.LastReconcileOk AS int)
        WHERE Node = @node AND RecoveryGeneration = @gen
          AND (RecoveryPhase <> 2 OR @readStarted > RecoveryCoveredUtc);
        """;

    // Quarantine
    internal const string GetQuarantine = "SELECT TOP (@take) Seq, Node, Error, FirstSeenUtc, LastSeenUtc, Attempts, ResolvedUtc FROM dbo.QuestQuarantine ORDER BY LastSeenUtc DESC";

    internal const string Quarantine = """
        SET NOCOUNT ON;
        MERGE dbo.QuestQuarantine WITH (HOLDLOCK) AS t
        USING (SELECT @seq AS Seq, @node AS Node) AS src ON t.Seq = src.Seq AND t.Node = src.Node
        WHEN MATCHED THEN UPDATE SET Error = @err, LastSeenUtc = SYSUTCDATETIME(), Attempts = t.Attempts + 1,
            ResolvedUtc = NULL, ResolvedBy = NULL
        WHEN NOT MATCHED THEN INSERT (Seq, Node, Error) VALUES (@seq, @node, @err);
        """;

    internal const string GetQuarantinedSeqs = "SELECT Seq FROM dbo.QuestQuarantine WHERE Node = @node AND ResolvedUtc IS NULL AND Seq BETWEEN @from AND @to";

    internal const string GetOpenQuarantine =
        "SELECT q.Seq, q.Node, q.Attempts, CAST(ISNULL(o.Superseded, 0) AS bit) AS Superseded FROM dbo.QuestQuarantine q LEFT " +
        "JOIN dbo.QuestOutbox o ON o.Seq = q.Seq WHERE q.ResolvedUtc IS NULL AND q.Seq BETWEEN @from AND @to";

    internal const string ResolveQuarantine =
        "UPDATE dbo.QuestQuarantine SET ResolvedUtc = SYSUTCDATETIME(), ResolvedBy = @by WHERE Seq = @seq AND Node = @node AND " +
        "ResolvedUtc IS NULL AND Attempts = @expected";

    // Backup catalog
    internal const string GetBackups =
        "SELECT BackupId, Node, WatermarkSeq, WatermarkTs, Path, SizeBytes, Verified, CreatedUtc, RetiredUtc FROM " +
        "dbo.QuestBackupCatalog WHERE (@node IS NULL OR Node = @node) ORDER BY CreatedUtc DESC";

    internal const string AddBackup =
        "INSERT dbo.QuestBackupCatalog (BackupId, Node, WatermarkSeq, WatermarkTs, Path, SizeBytes, Verified, CreatedUtc, " +
        "RetiredUtc) VALUES (@id, @node, @seq, @ts, @path, @size, @ver, @created, @retired)";

    internal const string HasJournalGap = """
        SELECT CASE WHEN PurgedThroughSeq>@watermark OR
          (SELECT MIN(Seq)-1 FROM dbo.QuestOutbox)>@watermark
          THEN 1 ELSE 0 END FROM dbo.QuestJournalPurgeState WHERE Id=1;
        """;

    internal const string SetRestoreCursor = """
        SET XACT_ABORT ON;
        BEGIN TRAN;
        UPDATE dbo.QuestNodeCursor WITH (UPDLOCK,HOLDLOCK)
          SET LastSeq=@seq,SafeSeq=@seq,UpdatedUtc=SYSUTCDATETIME(),UpdatedBy=N'restore'
          WHERE Node=@node;
        IF @@ROWCOUNT<>1 BEGIN ROLLBACK; THROW 51034, N'Restore cursor missing.', 1; END;
        DECLARE @purged bigint;
        SELECT @purged=PurgedThroughSeq FROM dbo.QuestJournalPurgeState WITH (HOLDLOCK) WHERE Id=1;
        IF @purged IS NULL OR @purged>@seq OR (SELECT MIN(Seq)-1 FROM dbo.QuestOutbox WITH (HOLDLOCK))>@seq
        BEGIN ROLLBACK; THROW 51035, N'Journal gap during restore; backfill required.', 1; END;
        COMMIT;
        """;

    internal const string SetBackupRetired = """
        SET XACT_ABORT ON;
        BEGIN TRAN;
        DECLARE @node nvarchar(32), @latest uniqueidentifier;
        SELECT @node=Node FROM dbo.QuestBackupCatalog WITH (UPDLOCK,HOLDLOCK) WHERE BackupId=@id;
        IF @node IS NULL BEGIN ROLLBACK; THROW 51031, N'BackupId not found.', 1; END;
        SELECT TOP(1) @latest=BackupId FROM dbo.QuestBackupCatalog WITH (UPDLOCK,HOLDLOCK)
          WHERE Node=@node AND Verified=1 ORDER BY CreatedUtc DESC,CONVERT(char(36),BackupId) DESC;
        IF @retired=1 AND @id=@latest BEGIN ROLLBACK; THROW 51032, N'Latest Verified backup cannot be retired.', 1; END;
        IF @dry=0 UPDATE dbo.QuestBackupCatalog SET RetiredUtc=CASE WHEN @retired=1 THEN COALESCE(RetiredUtc,SYSUTCDATETIME()) ELSE NULL END WHERE BackupId=@id;
        COMMIT;
        """;

    // Purge
    internal const string BacklogNodes = """
        SELECT n.Node, CAST(CASE WHEN c.Node IS NULL THEN 0 ELSE 1 END AS bit) AS CursorExists,
          ISNULL(c.LastSeq,0) AS LastSeq, ISNULL(c.SafeSeq,0) AS SafeSeq,
          p.PendingRows, o.CreatedUtc AS OldestPendingCreatedUtc,
          CONVERT(float,ISNULL(DATEDIFF_BIG(MILLISECOND,o.CreatedUtc,SYSUTCDATETIME()),0))/1000 AS AgeSeconds
        FROM OPENJSON(@nodes) WITH (Node nvarchar(32) '$') n
        LEFT JOIN dbo.QuestNodeCursor c ON c.Node=n.Node
        CROSS APPLY (SELECT COUNT_BIG(*) AS PendingRows FROM
          (SELECT TOP (@limit) Seq FROM dbo.QuestOutbox
           WHERE Seq>ISNULL(c.LastSeq,0) AND RowVer<MIN_ACTIVE_ROWVERSION() ORDER BY Seq) bounded) p
        OUTER APPLY (SELECT TOP (1) CreatedUtc FROM dbo.QuestOutbox
          WHERE Seq>ISNULL(c.LastSeq,0) AND RowVer<MIN_ACTIVE_ROWVERSION() ORDER BY Seq) o;
        """;

    internal const string BacklogHeader = """
        SELECT SYSUTCDATETIME() AS MeasuredUtc,
          ISNULL(SUM(CASE WHEN index_id IN (0,1) THEN row_count ELSE 0 END),0) AS TotalRows,
          ISNULL(SUM(used_page_count),0) * CONVERT(bigint,8192) AS JournalBytes,
          (SELECT COUNT_BIG(*) FROM dbo.QuestQuarantine WHERE ResolvedUtc IS NULL) AS OpenQuarantineCount
        FROM sys.dm_db_partition_stats WHERE object_id=OBJECT_ID(N'dbo.QuestOutbox');
        """;

    internal const string PurgeDelete = """
        SET NOCOUNT ON; DECLARE @deleted TABLE(Seq bigint);
        DELETE o OUTPUT deleted.Seq INTO @deleted
        FROM OPENJSON(@seqs) WITH (Seq bigint '$') e
        JOIN dbo.QuestOutbox o WITH (ROWLOCK) ON o.Seq=e.Seq
        OPTION (LOOP JOIN, FORCE ORDER);
        -- cùng transaction với DELETE; không mất dấu khi purge hết hoặc giữ quarantine ở đầu.
        UPDATE dbo.QuestJournalPurgeState SET PurgedThroughSeq=
          CASE WHEN PurgedThroughSeq < (SELECT MAX(Seq) FROM @deleted)
          THEN (SELECT MAX(Seq) FROM @deleted) ELSE PurgedThroughSeq END WHERE Id=1;
        IF @@ROWCOUNT<>1 THROW 51033, N'Missing QuestJournalPurgeState migration.', 1;
        SELECT COUNT_BIG(*) FROM @deleted;
        """;

    internal const string PurgeReadyRows = """
        SELECT o.Seq FROM OPENJSON(@seqs) WITH (Seq bigint '$') e
        JOIN dbo.QuestOutbox o WITH (UPDLOCK,HOLDLOCK,ROWLOCK) ON o.Seq=e.Seq
        WHERE o.CreatedUtc<@cutoff AND o.RowVer<MIN_ACTIVE_ROWVERSION()
          AND NOT EXISTS (SELECT 1 FROM dbo.QuestQuarantine q WITH (HOLDLOCK)
                          WHERE q.Seq=o.Seq AND q.ResolvedUtc IS NULL)
        OPTION (LOOP JOIN, FORCE ORDER);
        """;

    // Export Parquet được tính như backup: dòng phải nằm trong backup lạnh (Seq ≤ @cold) hoặc trong bản xuất Verified chưa retire của
    // cùng bảng, khoảng UTC chứa DesignatedTs, watermark ≥ Seq. HOLDLOCK giữ catalog không đổi (retention không retire chen) tới COMMIT.
    internal const string PurgeReadyRowsExport = """
        SELECT o.Seq, CAST(CASE WHEN x.Hit IS NULL THEN 0 ELSE 1 END AS bit) AS Exported
        FROM OPENJSON(@seqs) WITH (Seq bigint '$') e
        JOIN dbo.QuestOutbox o WITH (UPDLOCK,HOLDLOCK,ROWLOCK) ON o.Seq=e.Seq
        OUTER APPLY (SELECT TOP (1) 1 AS Hit FROM dbo.QuestExportCatalog x WITH (HOLDLOCK)
                     WHERE x.TableName=o.TargetTable AND x.Verified=1 AND x.RetiredUtc IS NULL
                       AND o.DesignatedTs>=x.FromUtc AND o.DesignatedTs<x.ToUtc AND x.WatermarkSeq>=o.Seq) x
        WHERE o.CreatedUtc<@cutoff AND o.RowVer<MIN_ACTIVE_ROWVERSION()
          AND NOT EXISTS (SELECT 1 FROM dbo.QuestQuarantine q WITH (HOLDLOCK)
                          WHERE q.Seq=o.Seq AND q.ResolvedUtc IS NULL)
          AND (o.Seq<=@cold OR x.Hit IS NOT NULL)
        OPTION (LOOP JOIN, FORCE ORDER);
        """;

    // Tập kết quả thứ ba của mốc purge khi Export bật: có ít nhất một bản xuất Verified còn hiệu lực.
    internal const string PurgeBoundaryExportFlag =
        "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.QuestExportCatalog WHERE Verified=1 AND RetiredUtc IS NULL) THEN 1 ELSE 0 END;";

    internal const string PurgeBoundaryExportFlagLocked =
        "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.QuestExportCatalog WITH (HOLDLOCK) WHERE Verified=1 AND RetiredUtc IS NULL) THEN 1 ELSE 0 END;";

    internal const string PurgeCandidates = """
        SELECT TOP (@take) Seq,CreatedUtc,
          CAST(CASE WHEN RowVer<MIN_ACTIVE_ROWVERSION() THEN 1 ELSE 0 END AS bit)
        FROM dbo.QuestOutbox WITH (FORCESEEK)
        WHERE Seq>@after AND Seq<=@max ORDER BY Seq;
        """;

    internal const string PurgeAcquireLock = """
        SET LOCK_TIMEOUT 250; SET DEADLOCK_PRIORITY LOW; SET XACT_ABORT ON;
        DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'QuestHa:JournalPurge',
          @LockMode='Exclusive',@LockOwner='Session',@LockTimeout=0; SELECT @r;
        """;

    internal const string ReadNonMonotonicReason = """
        SELECT TOP (1) Seq FROM (
          SELECT TOP (500) Seq,CreatedUtc FROM dbo.QuestOutbox WITH (FORCESEEK)
          WHERE Seq>@blocking AND Seq<=@max ORDER BY Seq
        ) upcoming WHERE CreatedUtc<@cutoff ORDER BY Seq;
        """;

    internal const string RetireCursorDelete = "DELETE dbo.QuestNodeCursor WHERE Node=@node;";

    internal const string RetireCursorHeartbeat = """
        SELECT CASE WHEN HeartbeatUtc>DATEADD(minute,-@stale,SYSUTCDATETIME()) THEN 1 ELSE 0 END
        FROM dbo.QuestNodeStatus WITH (HOLDLOCK) WHERE Node=@node;
        """;

    internal const string RetireCursorRead = "SELECT LastSeq,SafeSeq FROM dbo.QuestNodeCursor WITH (UPDLOCK,HOLDLOCK) WHERE Node=@node;";

    internal const string PurgeBoundary =
        "SELECT Node,LastSeq,SafeSeq FROM dbo.QuestNodeCursor;SELECT SYSUTCDATETIME(),MIN(WatermarkSeq) FROM (SELECT " +
        "Node,WatermarkSeq,CreatedUtc,RetiredUtc, ROW_NUMBER() OVER(PARTITION BY Node ORDER BY CreatedUtc " +
        "DESC,CONVERT(char(36),BackupId) DESC) AS rn FROM dbo.QuestBackupCatalog WHERE Verified=1) b WHERE (rn=1 OR (RetiredUtc " +
        "IS NULL AND CreatedUtc>=DATEADD(day,-@days,SYSUTCDATETIME()))) AND (Node IN (SELECT Node FROM dbo.QuestNodeCursor) OR " +
        "Node IN (SELECT value FROM OPENJSON(@nodes)));";

    internal const string PurgeBoundaryLocked =
        "SELECT Node,LastSeq,SafeSeq FROM dbo.QuestNodeCursor WITH (HOLDLOCK);SELECT SYSUTCDATETIME(),MIN(WatermarkSeq) FROM " +
        "(SELECT Node,WatermarkSeq,CreatedUtc,RetiredUtc, ROW_NUMBER() OVER(PARTITION BY Node ORDER BY CreatedUtc " +
        "DESC,CONVERT(char(36),BackupId) DESC) AS rn FROM dbo.QuestBackupCatalog WITH (HOLDLOCK) WHERE Verified=1) b WHERE (rn=1 " +
        "OR (RetiredUtc IS NULL AND CreatedUtc>=DATEADD(day,-@days,SYSUTCDATETIME()))) AND (Node IN (SELECT Node FROM " +
        "dbo.QuestNodeCursor WITH (HOLDLOCK)) OR Node IN (SELECT value FROM OPENJSON(@nodes)));";

    // Coverage
    internal const string ReadCoverageCore = """
        SELECT s.RecoveryGeneration AS Generation,s.RecoveryPhase AS Phase,s.ManualFence,c.LastSeq,
               (SELECT MIN(q.Seq) FROM dbo.QuestQuarantine q WHERE q.Node=s.Node AND q.ResolvedUtc IS NULL) AS FirstQuarantinedSeq
        FROM dbo.QuestNodeStatus s JOIN dbo.QuestNodeCursor c ON c.Node=s.Node WHERE s.Node=@node;
        """;
}
