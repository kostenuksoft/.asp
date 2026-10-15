const results = [];

const check = (name, passed, detail) => {
    results.push({ name, passed: Boolean(passed), detail });
};

const infos = db.getCollectionInfos({ name: { $in: ["buildEvents", "crashReports", "discussions", "packageInsights"] } });
check(
    "3 to 4 collections, each with JSON Schema validation",
    infos.length >= 3 && infos.length <= 4 && infos.every((info) => info.options.validator && info.options.validator.$jsonSchema),
    `${infos.length} collections with validators`
);

const nested = db.discussions.countDocuments({ "comments.replies.0": { $exists: true } });
check("embedding: comments embed their replies", nested >= 1, `${nested} discussion(s) with nested replies`);

const referencing = db.discussions.countDocuments({ packageId: { $type: "long" }, "linkedBuildRunIds.0": { $type: "long" } });
check("referencing: package and runs referenced by Id, not embedded", referencing >= 1, `${referencing} discussion(s) with packageId and linkedBuildRunIds`);

const idFields = { buildEvents: ["buildRunId", "packageId"], crashReports: ["packageVersionId", "packageId"], discussions: ["packageId"], packageInsights: ["packageId"] };
const withoutId = Object.entries(idFields).map(([name, fields]) => ({
    name,
    missing: db.getCollection(name).countDocuments({ $or: fields.map((field) => ({ [field]: { $not: { $type: "long" } } })) })
}));
check(
    "cross-context references are plain Id fields in every document",
    withoutId.every((entry) => entry.missing === 0),
    withoutId.map((entry) => `${entry.name} ${entry.missing} without Id`).join(", ")
);

const tracked = db.crashReports.aggregate([
    { $match: { discussionId: { $exists: true } } },
    { $lookup: { from: "discussions", localField: "discussionId", foreignField: "_id", as: "discussion" } },
    { $project: { fingerprint: 1, title: { $first: "$discussion.title" }, matches: { $size: "$discussion" } } }
]).toArray();
check(
    "referencing between collections: crashReports.discussionId resolves to one discussion via $lookup",
    tracked.length >= 1 && tracked.every((row) => row.matches === 1),
    tracked.map((row) => `${row.fingerprint} -> "${row.title}"`).join(", ")
);

const hybrid =db.discussions.countDocuments({ packageKey: { $exists: true }, packageName: { $exists: true }, "comments.0": { $exists: true } });
check("hybrid: reference + denormalised copy + embedded array in one document", hybrid === db.discussions.countDocuments({}), `${hybrid} hybrid discussion(s)`);

const shapes = db.crashReports.aggregate([
    { $project: { keys: { $map: { input: { $objectToArray: "$$ROOT" }, as: "field", in: "$$field.k" } } } },
    { $project: { keys: { $sortArray: { input: "$keys", sortBy: 1 } } } },
    { $group: { _id: "$keys" } }
]).toArray();
check("flexible schema: crashReports holds documents with different field sets", shapes.length >= 3, `${shapes.length} distinct field sets`);

const payloads = db.buildEvents.aggregate([
    { $project: { keys: { $map: { input: { $objectToArray: "$payload" }, as: "field", in: "$$field.k" } } } },
    { $group: { _id: "$keys" } }
]).toArray();
check("flexible schema: buildEvents payload shape varies by eventType", payloads.length >= 3, `${payloads.length} distinct payload shapes`);

const withoutCopy = db.discussions.countDocuments({ packageName: { $exists: false } }) + db.packageInsights.countDocuments({ packageName: { $exists: false } });
check("denormalised packageName present for reads", withoutCopy === 0, `${withoutCopy} document(s) missing the copy`);

let rejection = 0;
try {
    db.crashReports.insertOne({
        kind: "unhandledException",
        versionKey: "platform-team/devhub-auth@2.1.0",
        packageKey: "platform-team/devhub-auth",
        occurredAt: new Date(),
        fingerprint: "verify-missing-stack"
    });
} catch (error) {
    rejection = error.code;
}
check("validator rejects an exception report without stackFrames", rejection === 121, `error ${rejection}`);

let malformedKey = 0;
try {
    db.discussions.insertOne({
        packageKey: "no-slash",
        packageName: "x",
        publisherSlug: "x",
        title: "verify malformed key",
        state: "open",
        openedBy: "verify",
        openedAt: new Date(),
        comments: []
    });
} catch (error) {
    malformedKey = error.code;
}
check("validator rejects a malformed packageKey reference", malformedKey === 121, `error ${malformedKey}`);

const expectedIndexes = { buildEvents: 3, crashReports: 5, discussions: 4, packageInsights: 1 };
const indexReport = Object.entries(expectedIndexes).map(([name, count]) => {
    const actual = db.getCollection(name).getIndexes().filter((index) => index.name !== "_id_").length;
    return { name, count, actual };
});
check(
    "indexes created for the query patterns",
    indexReport.every((entry) => entry.actual === entry.count),
    indexReport.map((entry) => `${entry.name} ${entry.actual}/${entry.count}`).join(", ")
);

const ttl = db.crashReports.getIndexes().find((index) => index.name === "ix_expiresAt_ttl");
check("TTL index expires crash reports at expiresAt", ttl && ttl.expireAfterSeconds === 0, ttl ? "expireAfterSeconds 0" : "missing");

const plan = db.discussions.find({ packageKey: "platform-team/devhub-auth", state: "answered" }).sort({ openedAt: -1 }).explain("queryPlanner");
const planText = JSON.stringify(plan.queryPlanner.winningPlan);
check("thread list by package and state uses its compound index", planText.includes("ix_packageKey_state_openedAt"), "IXSCAN ix_packageKey_state_openedAt");

const crashesByPackage = db.crashReports.aggregate([
    { $group: { _id: "$packageKey", crashes: { $sum: 1 }, kinds: { $addToSet: "$kind" } } },
    { $sort: { crashes: -1, _id: 1 } }
]).toArray();
check("aggregation: crashes per package", crashesByPackage.length === 3, crashesByPackage.map((row) => `${row._id}=${row.crashes}`).join(", "));

const failureRate = db.buildEvents.aggregate([
    { $match: { eventType: "runFinished" } },
    { $group: { _id: "$pipelineName", runs: { $sum: 1 }, failed: { $sum: { $cond: [{ $eq: ["$payload.status", "Failed"] }, 1, 0] } } } },
    { $project: { _id: 0, pipeline: "$_id", runs: 1, failureRate: { $divide: ["$failed", "$runs"] } } },
    { $sort: { pipeline: 1 } }
]).toArray();
check("aggregation: failure rate per pipeline from run events", failureRate.length === 2, failureRate.map((row) => `${row.pipeline}=${row.failureRate}`).join(", "));

const unwound = db.discussions.aggregate([
    { $unwind: "$comments" },
    { $group: { _id: "$comments.author", comments: { $sum: 1 } } },
    { $sort: { comments: -1, _id: 1 } },
    { $limit: 1 }
]).toArray();
check("aggregation: unwind embedded comments to rank authors", unwound.length === 1, unwound.length ? `${unwound[0]._id}=${unwound[0].comments}` : "none");

const found = db.discussions.find({ $text: { $search: "token" } }).count();
check("text search over titles and comment bodies", found >= 1, `${found} thread(s) mention token`);

const page1 = db.buildEvents.find({}).sort({ occurredAt: -1, _id: -1 }).limit(2).toArray();
const last = page1[page1.length - 1];
const page2 = db.buildEvents.find({
    $or: [{ occurredAt: { $lt: last.occurredAt } }, { occurredAt: last.occurredAt, _id: { $lt: last._id } }]
}).sort({ occurredAt: -1, _id: -1 }).limit(2).toArray();
check(
    "keyset pagination over events returns disjoint pages",
    page1.length === 2 && page2.length >= 1 && page2.every((document) => !page1.some((other) => other._id.equals(document._id))),
    `page 1 ${page1.length}, page 2 ${page2.length}`
);

const unversioned = db.discussions.countDocuments({ version: { $not: { $type: "int" } } })
    + db.crashReports.countDocuments({ version: { $not: { $type: "int" } } });
check("optimistic concurrency: discussions and crash reports carry an int version", unversioned === 0, `${unversioned} document(s) without version`);

results.forEach((result, index) => print(`${String(index + 1).padStart(2)} ${result.passed ? "PASS" : "FAIL"} ${result.name} | ${result.detail}`));

const failed = results.filter((result) => !result.passed).length;
print(`${results.length - failed}/${results.length} checks passed`);

if (failed > 0) {
    quit(1);
}
