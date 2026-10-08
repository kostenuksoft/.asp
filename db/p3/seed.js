const at = (value) => new Date(value);

const seed = (collection, keyFields, documents) => {
    let inserted = 0;

    for (const document of documents) {
        const filter = {};
        for (const field of keyFields) {
            filter[field] = document[field];
        }

        const result = db.getCollection(collection).updateOne(filter, { $setOnInsert: document }, { upsert: true });
        inserted += result.upsertedCount;
    }

    print(`${collection.padEnd(16)} ${inserted} inserted, ${db.getCollection(collection).countDocuments({})} total`);
};

seed("buildEvents", ["runKey", "eventType", "occurredAt"], [
    {
        eventType: "runQueued",
        buildRunId: NumberLong(2),
        runKey: "devhub-auth CI#2",
        pipelineName: "devhub-auth CI",
        runNumber: NumberInt(2),
        packageId: NumberLong(2),
        packageKey: "platform-team/devhub-auth",
        occurredAt: at("2026-09-16T09:00:00Z"),
        payload: { branch: "feature/claims", triggerKind: "PullRequest", pullRequest: NumberInt(128) },
        createdAt: at("2026-09-16T09:00:01Z"),
        createdBy: "cicd-sync"
    },
    {
        eventType: "agentLeased",
        buildRunId: NumberLong(2),
        runKey: "devhub-auth CI#2",
        pipelineName: "devhub-auth CI",
        runNumber: NumberInt(2),
        packageId: NumberLong(2),
        packageKey: "platform-team/devhub-auth",
        occurredAt: at("2026-09-16T09:01:00Z"),
        payload: { agentHostname: "build-win-01", operatingSystem: "Windows" },
        createdAt: at("2026-09-16T09:01:01Z"),
        createdBy: "cicd-sync"
    },
    {
        eventType: "stepFinished",
        buildRunId: NumberLong(2),
        runKey: "devhub-auth CI#2",
        pipelineName: "devhub-auth CI",
        runNumber: NumberInt(2),
        packageId: NumberLong(2),
        packageKey: "platform-team/devhub-auth",
        occurredAt: at("2026-09-16T09:05:00Z"),
        payload: {
            stepName: "Test",
            status: "Failed",
            exitCode: NumberInt(1),
            failedTests: ["ClaimValidatorTests.RejectsExpiredToken", "ClaimValidatorTests.AcceptsRefreshedToken"],
            durationSeconds: NumberInt(61)
        },
        createdAt: at("2026-09-16T09:05:01Z"),
        createdBy: "cicd-sync"
    },
    {
        eventType: "runFinished",
        buildRunId: NumberLong(2),
        runKey: "devhub-auth CI#2",
        pipelineName: "devhub-auth CI",
        runNumber: NumberInt(2),
        packageId: NumberLong(2),
        packageKey: "platform-team/devhub-auth",
        occurredAt: at("2026-09-16T09:05:02Z"),
        payload: { status: "Failed", durationSeconds: NumberInt(242), failedStepCount: NumberInt(1) },
        createdAt: at("2026-09-16T09:05:03Z"),
        createdBy: "cicd-sync"
    },
    {
        eventType: "runFinished",
        buildRunId: NumberLong(4),
        runKey: "devhub-storage CI#1",
        pipelineName: "devhub-storage CI",
        runNumber: NumberInt(1),
        packageId: NumberLong(3),
        packageKey: "platform-team/devhub-storage",
        occurredAt: at("2026-09-12T14:20:00Z"),
        payload: { status: "Succeeded", durationSeconds: NumberInt(415), artifactCount: NumberInt(1) },
        createdAt: at("2026-09-12T14:20:01Z"),
        createdBy: "cicd-sync"
    }
]);

seed("discussions", ["packageKey", "title"], [
    {
        packageId: NumberLong(2),
        packageKey: "platform-team/devhub-auth",
        packageName: "devhub-auth",
        publisherSlug: "platform-team",
        title: "Expired tokens throw instead of returning a validation result",
        category: "bug",
        state: "answered",
        labels: ["regression", "v2"],
        affectedVersions: ["2.0.0"],
        linkedBuildRunIds: [NumberLong(2)],
        openedBy: "m.shevchenko",
        openedAt: at("2026-09-12T10:00:00Z"),
        commentCount: NumberInt(3),
        comments: [
            {
                commentId: "c1",
                author: "m.shevchenko",
                body: "After upgrading to 2.0.0 an expired token throws SecurityTokenExpiredException rather than returning Invalid.",
                postedAt: at("2026-09-12T10:00:00Z"),
                reactions: { "+1": NumberInt(4) },
                replies: [
                    {
                        commentId: "c1r1",
                        author: "o.kovalenko",
                        body: "Reproduced. The change was intentional but undocumented.",
                        postedAt: at("2026-09-13T08:15:00Z")
                    }
                ]
            },
            {
                commentId: "c2",
                author: "o.kovalenko",
                body: "Fixed in 2.1.0: validation returns a result object and only throws on malformed input.",
                postedAt: at("2026-09-14T16:40:00Z"),
                reactions: { "+1": NumberInt(7), heart: NumberInt(2) },
                replies: []
            },
            {
                commentId: "c3",
                author: "m.shevchenko",
                body: "Confirmed on 2.1.0. Thanks.",
                postedAt: at("2026-09-15T09:05:00Z"),
                replies: []
            }
        ],
        createdAt: at("2026-09-12T10:00:00Z"),
        createdBy: "discussion-api"
    },
    {
        packageId: NumberLong(3),
        packageKey: "platform-team/devhub-storage",
        packageName: "devhub-storage",
        publisherSlug: "platform-team",
        title: "Support resumable uploads for files over 5 GB",
        category: "featureRequest",
        state: "open",
        labels: ["enhancement"],
        openedBy: "a.tkachenko",
        openedAt: at("2026-09-08T12:30:00Z"),
        commentCount: NumberInt(1),
        comments: [
            {
                commentId: "c1",
                author: "a.tkachenko",
                body: "Large media ingest fails on flaky links. Resumable uploads would avoid restarting from zero.",
                postedAt: at("2026-09-08T12:30:00Z"),
                replies: []
            }
        ],
        createdAt: at("2026-09-08T12:30:00Z"),
        createdBy: "discussion-api"
    },
    {
        packageId: NumberLong(4),
        packageKey: "research-team/graph-explorer",
        packageName: "graph-explorer",
        publisherSlug: "research-team",
        title: "Is this package ready for production use?",
        category: "question",
        state: "closed",
        labels: ["experimental"],
        openedBy: "d.bondarenko",
        openedAt: at("2026-09-15T11:00:00Z"),
        closedAt: at("2026-09-16T10:20:00Z"),
        commentCount: NumberInt(1),
        comments: [
            {
                commentId: "c1",
                author: "research-bot",
                body: "No. This package is tagged experimental and has no support commitment.",
                postedAt: at("2026-09-16T10:20:00Z"),
                replies: []
            }
        ],
        createdAt: at("2026-09-15T11:00:00Z"),
        createdBy: "discussion-api"
    }
]);

const discussionIdOf = (packageKey, title) => db.discussions.findOne({ packageKey, title }, { _id: 1 })._id;

seed("crashReports", ["fingerprint", "occurredAt"], [
    {
        kind: "unhandledException",
        packageVersionId: NumberLong(2),
        versionKey: "platform-team/devhub-auth@2.0.0",
        packageId: NumberLong(2),
        packageKey: "platform-team/devhub-auth",
        discussionId: discussionIdOf("platform-team/devhub-auth", "Expired tokens throw instead of returning a validation result"),
        occurredAt: at("2026-09-13T22:41:00Z"),
        expiresAt: at("2027-09-13T22:41:00Z"),
        fingerprint: "a1b2c3d4e5f60718",
        environment: "production",
        severity: "high",
        exception: {
            type: "System.Security.SecurityTokenExpiredException",
            message: "The token is expired.",
            innerType: "System.ArgumentOutOfRangeException"
        },
        stackFrames: [
            { method: "ClaimValidator.Validate", file: "ClaimValidator.cs", line: NumberInt(88) },
            { method: "AuthMiddleware.InvokeAsync", file: "AuthMiddleware.cs", line: NumberInt(41) }
        ],
        context: { serviceName: "checkout-api", region: "eu-central-1", requestId: "c9f1a4" },
        createdAt: at("2026-09-13T22:41:05Z"),
        createdBy: "telemetry-agent"
    },
    {
        kind: "performanceDegradation",
        packageVersionId: NumberLong(3),
        versionKey: "platform-team/devhub-storage@1.4.2",
        packageId: NumberLong(3),
        packageKey: "platform-team/devhub-storage",
        occurredAt: at("2026-09-15T08:12:00Z"),
        expiresAt: at("2027-09-15T08:12:00Z"),
        fingerprint: "f0e1d2c3b4a59687",
        environment: "production",
        severity: "medium",
        metric: { name: "blobUploadLatencyP95", observed: 2840.5, threshold: 1200.0, unit: "ms" },
        context: { serviceName: "media-ingest", bucket: "user-uploads" },
        createdAt: at("2026-09-15T08:12:03Z"),
        createdBy: "telemetry-agent"
    },
    {
        kind: "healthCheckFailure",
        packageVersionId: NumberLong(4),
        versionKey: "payments-team/billing-sdk@0.9.1",
        packageId: NumberLong(1),
        packageKey: "payments-team/billing-sdk",
        occurredAt: at("2026-09-16T03:30:00Z"),
        expiresAt: at("2027-09-16T03:30:00Z"),
        fingerprint: "9876fedcba543210",
        environment: "staging",
        checkName: "billing-api-reachability",
        createdAt: at("2026-09-16T03:30:02Z"),
        createdBy: "telemetry-agent"
    }
]);

seed("packageInsights", ["packageKey", "windowStart"], [
    {
        packageId: NumberLong(2),
        packageKey: "platform-team/devhub-auth",
        packageName: "devhub-auth",
        publisherSlug: "platform-team",
        windowStart: at("2026-09-14T00:00:00Z"),
        windowEnd: at("2026-09-17T00:00:00Z"),
        totals: { downloads: NumberLong("1420"), crashes: NumberInt(1), openDiscussions: NumberInt(0) },
        daily: [
            { day: at("2026-09-14T00:00:00Z"), downloads: NumberLong("480"), crashes: NumberInt(1), buildSuccessRate: 0.5 },
            { day: at("2026-09-15T00:00:00Z"), downloads: NumberLong("465"), crashes: NumberInt(0), buildSuccessRate: 1.0 },
            { day: at("2026-09-16T00:00:00Z"), downloads: NumberLong("475"), crashes: NumberInt(0), buildSuccessRate: 1.0 }
        ],
        refreshedAt: at("2026-09-17T00:05:00Z"),
        createdAt: at("2026-09-17T00:05:00Z"),
        createdBy: "insights-job"
    },
    {
        packageId: NumberLong(3),
        packageKey: "platform-team/devhub-storage",
        packageName: "devhub-storage",
        publisherSlug: "platform-team",
        windowStart: at("2026-09-14T00:00:00Z"),
        windowEnd: at("2026-09-17T00:00:00Z"),
        totals: { downloads: NumberLong("910"), crashes: NumberInt(1), openDiscussions: NumberInt(1) },
        daily: [
            { day: at("2026-09-14T00:00:00Z"), downloads: NumberLong("300"), crashes: NumberInt(0), buildSuccessRate: 1.0 },
            { day: at("2026-09-15T00:00:00Z"), downloads: NumberLong("305"), crashes: NumberInt(1), buildSuccessRate: 1.0 },
            { day: at("2026-09-16T00:00:00Z"), downloads: NumberLong("305"), crashes: NumberInt(0), buildSuccessRate: 1.0 }
        ],
        refreshedAt: at("2026-09-17T00:05:00Z"),
        createdAt: at("2026-09-17T00:05:00Z"),
        createdBy: "insights-job"
    }
]);
