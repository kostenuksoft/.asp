BEGIN TRANSACTION;
CREATE TABLE [registry].[OutboxMessage] (
    [OutboxMessageId] bigint NOT NULL IDENTITY,
    [EventId] uniqueidentifier NOT NULL,
    [EventType] nvarchar(200) NOT NULL,
    [Exchange] nvarchar(200) NOT NULL,
    [RoutingKey] nvarchar(200) NOT NULL,
    [CorrelationId] nvarchar(100) NULL,
    [OccurredAt] datetime2(3) NOT NULL,
    [SchemaVersion] int NOT NULL,
    [Payload] nvarchar(max) NOT NULL,
    [TraceParent] varchar(55) NULL,
    [PublishedAt] datetime2(3) NULL,
    [Attempts] int NOT NULL,
    [LastError] nvarchar(1000) NULL,
    CONSTRAINT [PK_OutboxMessage] PRIMARY KEY ([OutboxMessageId])
);

CREATE INDEX [IX_OutboxMessage_Unpublished] ON [registry].[OutboxMessage] ([OutboxMessageId]) INCLUDE ([OccurredAt]) WHERE [PublishedAt] IS NULL;

CREATE UNIQUE INDEX [UQ_OutboxMessage_EventId] ON [registry].[OutboxMessage] ([EventId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261025200116_AddOutbox', N'9.0.20');

COMMIT;
GO

