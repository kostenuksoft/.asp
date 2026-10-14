const packageKeyPattern = "^[a-z0-9-]{2,}/[A-Za-z0-9._-]{2,}$";
const versionKeyPattern = "^[a-z0-9-]{2,}/[A-Za-z0-9._-]{2,}@.+$";

const collections = [
    {
        name: "buildEvents",
        indexes: [
            { name: "ix_runKey_occurredAt", keys: { runKey: 1, occurredAt: 1 } },
            { name: "ix_packageKey_occurredAt", keys: { packageKey: 1, occurredAt: -1 }, options: { sparse: true } },
            { name: "ix_eventType_occurredAt", keys: { eventType: 1, occurredAt: -1 } }
        ],
        schema: {
            bsonType: "object",
            description: "CI/CD build log events. The shape of payload depends on eventType.",
            required: ["eventType", "buildRunId", "runKey", "pipelineName", "occurredAt", "payload"],
            properties: {
                _id: { bsonType: "objectId" },
                eventType: { enum: ["runQueued", "agentLeased", "stepFinished", "runFinished"] },
                buildRunId: { bsonType: "long", minimum: 1, description: "Reference to cicd.BuildRun by its Id. Plain field, no foreign key across databases." },
                runKey: { bsonType: "string", maxLength: 200, description: "Copy: readable \"<pipeline>#<run number>\" of the run in buildRunId. Owner: CI/CD. Policy: snapshot at event time." },
                pipelineName: { bsonType: "string", maxLength: 200, description: "Copy of cicd.Pipeline.Name. Owner: CI/CD. Policy: snapshot at event time." },
                runNumber: { bsonType: "int", minimum: 1, description: "Copy of cicd.BuildRun.RunNumber. Owner: CI/CD. Policy: snapshot." },
                packageId: { bsonType: "long", minimum: 1, description: "Reference to registry.Package by its Id. Plain field, no foreign key across databases." },
                packageKey: { bsonType: "string", pattern: packageKeyPattern, description: "Copy: readable \"<publisher>/<name>\" of the package in packageId. Owner: Registry. Policy: snapshot at event time." },
                occurredAt: { bsonType: "date" },
                payload: { bsonType: "object", description: "Embedded, shape varies by eventType." },
                createdAt: { bsonType: "date" },
                createdBy: { bsonType: "string", maxLength: 128 }
            },
            additionalProperties: false
        }
    },
    {
        name: "crashReports",
        indexes: [
            { name: "ix_fingerprint_occurredAt", keys: { fingerprint: 1, occurredAt: -1 } },
            { name: "ix_versionKey_kind", keys: { versionKey: 1, kind: 1 } },
            { name: "ix_severity_occurredAt", keys: { severity: 1, occurredAt: -1 } },
            { name: "ix_discussionId", keys: { discussionId: 1 }, options: { sparse: true } },
            { name: "ix_expiresAt_ttl", keys: { expiresAt: 1 }, options: { expireAfterSeconds: 0 } }
        ],
        schema: {
            bsonType: "object",
            description: "Crash and diagnostic reports. Polymorphic: discriminator kind, each kind requires its own fields.",
            required: ["kind", "packageVersionId", "versionKey", "packageId", "packageKey", "occurredAt", "fingerprint"],
            oneOf: [
                { properties: { kind: { enum: ["unhandledException"] } }, required: ["exception", "stackFrames"] },
                { properties: { kind: { enum: ["performanceDegradation"] } }, required: ["metric"] },
                { properties: { kind: { enum: ["healthCheckFailure"] } }, required: ["checkName"] }
            ],
            properties: {
                _id: { bsonType: "objectId" },
                kind: { enum: ["unhandledException", "performanceDegradation", "healthCheckFailure"] },
                packageVersionId: { bsonType: "long", minimum: 1, description: "Reference to registry.PackageVersion by its Id. Plain field, no foreign key across databases." },
                versionKey: { bsonType: "string", pattern: versionKeyPattern, description: "Copy: readable \"<publisher>/<name>@<semver>\" of the version in packageVersionId. Owner: Registry. Policy: snapshot; a published version never changes." },
                packageId: { bsonType: "long", minimum: 1, description: "Reference to registry.Package by its Id. Plain field, no foreign key across databases." },
                packageKey: { bsonType: "string", pattern: packageKeyPattern, description: "Copy: readable \"<publisher>/<name>\" of the package in packageId. Owner: Registry. Policy: snapshot." },
                discussionId: { bsonType: "objectId", description: "Reference to the discussions document that tracks this crash; resolved with $lookup, not embedded." },
                occurredAt: { bsonType: "date" },
                expiresAt: { bsonType: "date", description: "Retention deadline; the TTL index removes the report after it." },
                fingerprint: { bsonType: "string", maxLength: 64 },
                environment: { enum: ["development", "staging", "production"] },
                severity: { enum: ["low", "medium", "high", "critical"] },
                exception: {
                    bsonType: "object",
                    required: ["type", "message"],
                    properties: {
                        type: { bsonType: "string" },
                        message: { bsonType: "string" },
                        innerType: { bsonType: "string" }
                    }
                },
                stackFrames: {
                    bsonType: "array",
                    maxItems: 200,
                    items: {
                        bsonType: "object",
                        required: ["method"],
                        properties: {
                            method: { bsonType: "string" },
                            file: { bsonType: "string" },
                            line: { bsonType: "int", minimum: 0 }
                        }
                    }
                },
                metric: {
                    bsonType: "object",
                    required: ["name", "observed", "threshold"],
                    properties: {
                        name: { bsonType: "string" },
                        observed: { bsonType: "number" },
                        threshold: { bsonType: "number" },
                        unit: { bsonType: "string" }
                    }
                },
                checkName: { bsonType: "string" },
                context: { bsonType: "object" },
                createdAt: { bsonType: "date" },
                createdBy: { bsonType: "string", maxLength: 128 },
                updatedAt: { bsonType: "date", description: "Time of the last change made through the Telemetry service." },
                version: { bsonType: "int", minimum: 0, description: "Optimistic concurrency: raised by every update, which succeeds only at the expected version." }
            },
            additionalProperties: false
        }
    },
    {
        name: "discussions",
        indexes: [
            { name: "ux_packageKey_title", keys: { packageKey: 1, title: 1 }, options: { unique: true } },
            { name: "ix_packageKey_state_openedAt", keys: { packageKey: 1, state: 1, openedAt: -1 } },
            { name: "ix_labels", keys: { labels: 1 } },
            { name: "tx_title_commentBody", keys: { title: "text", "comments.body": "text" } }
        ],
        schema: {
            bsonType: "object",
            description: "Package discussion threads. Hybrid model: comments and replies embedded, package and runs referenced by key, package name copied for reads.",
            required: ["packageId", "packageKey", "packageName", "publisherSlug", "title", "state", "openedBy", "openedAt", "comments"],
            properties: {
                _id: { bsonType: "objectId" },
                packageId: { bsonType: "long", minimum: 1, description: "Reference to registry.Package by its Id. Plain field, no foreign key across databases." },
                packageKey: { bsonType: "string", pattern: packageKeyPattern, description: "Copy: readable \"<publisher>/<name>\" of the package in packageId. Owner: Registry. Policy: refreshed by Registry events." },
                packageName: { bsonType: "string", maxLength: 200, description: "Copy of registry.Package.Name. Owner: Registry. Policy: refreshed by Registry events." },
                publisherSlug: { bsonType: "string", maxLength: 100, description: "Copy of registry.Publisher.Slug. Owner: Registry. Policy: refreshed by Registry events." },
                title: { bsonType: "string", maxLength: 300 },
                category: { enum: ["bug", "question", "featureRequest", "announcement"] },
                state: { enum: ["open", "answered", "closed"] },
                labels: { bsonType: "array", items: { bsonType: "string" }, maxItems: 20 },
                affectedVersions: { bsonType: "array", items: { bsonType: "string" }, maxItems: 50 },
                linkedBuildRunIds: { bsonType: "array", items: { bsonType: "long", minimum: 1 }, maxItems: 50, description: "References to cicd.BuildRun by Id. Plain values, no foreign key across databases." },
                openedBy: { bsonType: "string", maxLength: 128 },
                openedAt: { bsonType: "date" },
                closedAt: { bsonType: "date" },
                commentCount: { bsonType: "int", minimum: 0 },
                comments: {
                    bsonType: "array",
                    maxItems: 500,
                    description: "Embedded comments; each comment embeds its replies.",
                    items: {
                        bsonType: "object",
                        required: ["commentId", "author", "body", "postedAt"],
                        properties: {
                            commentId: { bsonType: "string" },
                            author: { bsonType: "string", maxLength: 128 },
                            body: { bsonType: "string", maxLength: 8000 },
                            postedAt: { bsonType: "date" },
                            editedAt: { bsonType: "date" },
                            reactions: { bsonType: "object" },
                            replies: { bsonType: "array", maxItems: 100 }
                        }
                    }
                },
                createdAt: { bsonType: "date" },
                createdBy: { bsonType: "string", maxLength: 128 },
                updatedAt: { bsonType: "date", description: "Time of the last change made through the Telemetry service." },
                version: { bsonType: "int", minimum: 0, description: "Optimistic concurrency: raised by every update, which succeeds only at the expected version." }
            },
            additionalProperties: false
        }
    },
    {
        name: "packageInsights",
        indexes: [
            { name: "ux_packageKey_windowStart", keys: { packageKey: 1, windowStart: -1 }, options: { unique: true } }
        ],
        schema: {
            bsonType: "object",
            description: "Denormalised per-package rollups. Read model: holds nothing authoritative, recomputed by a job.",
            required: ["packageId", "packageKey", "packageName", "windowStart", "windowEnd", "daily"],
            properties: {
                _id: { bsonType: "objectId" },
                packageId: { bsonType: "long", minimum: 1, description: "Reference to registry.Package by its Id. Plain field, no foreign key across databases." },
                packageKey: { bsonType: "string", pattern: packageKeyPattern, description: "Copy: readable \"<publisher>/<name>\" of the package in packageId. Owner: Registry. Policy: refreshed by Registry events." },
                packageName: { bsonType: "string", maxLength: 200, description: "Copy of registry.Package.Name. Owner: Registry. Policy: refreshed by Registry events." },
                publisherSlug: { bsonType: "string", maxLength: 100, description: "Copy of registry.Publisher.Slug. Owner: Registry. Policy: refreshed by Registry events." },
                windowStart: { bsonType: "date" },
                windowEnd: { bsonType: "date" },
                totals: {
                    bsonType: "object",
                    properties: {
                        downloads: { bsonType: "long", minimum: 0 },
                        crashes: { bsonType: "int", minimum: 0 },
                        openDiscussions: { bsonType: "int", minimum: 0 }
                    }
                },
                daily: {
                    bsonType: "array",
                    maxItems: 400,
                    items: {
                        bsonType: "object",
                        required: ["day"],
                        properties: {
                            day: { bsonType: "date" },
                            downloads: { bsonType: "long", minimum: 0 },
                            crashes: { bsonType: "int", minimum: 0 },
                            buildSuccessRate: { bsonType: "number", minimum: 0, maximum: 1 }
                        }
                    }
                },
                refreshedAt: { bsonType: "date" },
                createdAt: { bsonType: "date" },
                createdBy: { bsonType: "string", maxLength: 128 }
            },
            additionalProperties: false
        }
    }
];

const existing = db.getCollectionNames();

for (const collection of collections) {
    const options = {
        validator: { $jsonSchema: collection.schema },
        validationLevel: "strict",
        validationAction: "error"
    };

    if (existing.includes(collection.name)) {
        db.runCommand({ collMod: collection.name, ...options });
        print(`updated   ${collection.name}`);
    } else {
        db.createCollection(collection.name, options);
        print(`created   ${collection.name}`);
    }

    for (const index of collection.indexes) {
        db.getCollection(collection.name).createIndex(index.keys, { name: index.name, ...(index.options || {}) });
    }
}

for (const name of ["discussions", "crashReports"]) {
    const result = db.getCollection(name).updateMany({ version: { $exists: false } }, { $set: { version: NumberInt(0) } });
    print(`versioned ${name}: ${result.modifiedCount} document(s) without version`);
}
