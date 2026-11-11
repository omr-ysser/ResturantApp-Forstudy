IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [DiningTables] (
        [Id] uniqueidentifier NOT NULL,
        [Number] int NOT NULL,
        [Capacity] int NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_DiningTables] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_DiningTables_Capacity] CHECK ([Capacity] > 0),
        CONSTRAINT [CK_DiningTables_Number] CHECK ([Number] > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] uniqueidentifier NOT NULL,
        [Key] nvarchar(150) NOT NULL,
        [NormalizedKey] nvarchar(150) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [NormalizedName] nvarchar(100) NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [NormalizedEmail] nvarchar(256) NOT NULL,
        [PasswordHash] nvarchar(1000) NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [MenuItems] (
        [Id] uniqueidentifier NOT NULL,
        [CategoryId] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Price] decimal(18,2) NOT NULL,
        [IsAvailable] bit NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MenuItems_Price] CHECK ([Price] > 0),
        CONSTRAINT [FK_MenuItems_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [Reservations] (
        [Id] uniqueidentifier NOT NULL,
        [DiningTableId] uniqueidentifier NOT NULL,
        [CustomerName] nvarchar(150) NOT NULL,
        [CustomerPhone] nvarchar(30) NOT NULL,
        [GuestCount] int NOT NULL,
        [StartsAtUtc] datetimeoffset NOT NULL,
        [EndsAtUtc] datetimeoffset NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Reservations] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Reservations_GuestCount] CHECK ([GuestCount] > 0),
        CONSTRAINT [CK_Reservations_TimeRange] CHECK ([EndsAtUtc] > [StartsAtUtc]),
        CONSTRAINT [FK_Reservations_DiningTables_DiningTableId] FOREIGN KEY ([DiningTableId]) REFERENCES [DiningTables] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [RoleId] uniqueidentifier NOT NULL,
        [PermissionId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] uniqueidentifier NOT NULL,
        [OrderNumber] nvarchar(50) NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [DiningTableId] uniqueidentifier NULL,
        [CustomerName] nvarchar(150) NULL,
        [CustomerPhone] nvarchar(30) NULL,
        [DeliveryAddress] nvarchar(500) NULL,
        [Notes] nvarchar(1000) NULL,
        [CompletedAtUtc] datetimeoffset NULL,
        [CancellationReason] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_DiningTables_DiningTableId] FOREIGN KEY ([DiningTableId]) REFERENCES [DiningTables] ([Id]),
        CONSTRAINT [FK_Orders_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(128) NOT NULL,
        [ExpiresAtUtc] datetimeoffset NOT NULL,
        [RevokedAtUtc] datetimeoffset NULL,
        [ReplacedByTokenId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_RefreshTokens_ReplacedByTokenId] FOREIGN KEY ([ReplacedByTokenId]) REFERENCES [RefreshTokens] ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [MenuItemId] uniqueidentifier NOT NULL,
        [ItemName] nvarchar(150) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [Quantity] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OrderItems_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_OrderItems_UnitPrice] CHECK ([UnitPrice] > 0),
        CONSTRAINT [FK_OrderItems_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Method] nvarchar(30) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [TransactionReference] nvarchar(200) NULL,
        [FailureReason] nvarchar(1000) NULL,
        [PaidAtUtc] datetimeoffset NULL,
        [RefundedAtUtc] datetimeoffset NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Payments_Amount] CHECK ([Amount] > 0),
        CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DiningTables_Number] ON [DiningTables] ([Number]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MenuItems_CategoryId_Name] ON [MenuItems] ([CategoryId], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_MenuItemId] ON [OrderItems] ([MenuItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_CreatedByUserId] ON [Orders] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_DiningTableId] ON [Orders] ([DiningTableId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [Orders] ([OrderNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_Status_CreatedAtUtc] ON [Orders] ([Status], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_OrderId] ON [Payments] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Payments_TransactionReference] ON [Payments] ([TransactionReference]) WHERE [TransactionReference] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_NormalizedKey] ON [Permissions] ([NormalizedKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_ReplacedByTokenId] ON [RefreshTokens] ([ReplacedByTokenId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId_ExpiresAtUtc] ON [RefreshTokens] ([UserId], [ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reservations_DiningTableId_StartsAtUtc_EndsAtUtc] ON [Reservations] ([DiningTableId], [StartsAtUtc], [EndsAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_NormalizedName] ON [Roles] ([NormalizedName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_NormalizedEmail] ON [Users] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108142331_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108142331_InitialCreate', N'9.0.22');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109113805_AddAuthenticationSessions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261109113805_AddAuthenticationSessions', N'9.0.22');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109114917_CompleteAuthenticationSessionModel'
)
BEGIN
    ALTER TABLE [RefreshTokens] ADD [SessionId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109114917_CompleteAuthenticationSessionModel'
)
BEGIN
    CREATE TABLE [AuthenticationSessions] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [StartedAtUtc] datetimeoffset NOT NULL,
        [LastUsedAtUtc] datetimeoffset NOT NULL,
        [ExpiresAtUtc] datetimeoffset NOT NULL,
        [RevokedAtUtc] datetimeoffset NULL,
        [RevocationReason] nvarchar(40) NULL,
        [RefreshCount] bigint NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_AuthenticationSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_AuthenticationSessions_Id_UserId] UNIQUE ([Id], [UserId]),
        CONSTRAINT [CK_AuthenticationSessions_Expiry] CHECK ([ExpiresAtUtc] > [StartedAtUtc]),
        CONSTRAINT [CK_AuthenticationSessions_RefreshCount] CHECK ([RefreshCount] >= 0),
        CONSTRAINT [CK_AuthenticationSessions_Revocation] CHECK (([RevokedAtUtc] IS NULL AND [RevocationReason] IS NULL) OR ([RevokedAtUtc] IS NOT NULL AND [RevocationReason] IS NOT NULL)),
        CONSTRAINT [FK_AuthenticationSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109114917_CompleteAuthenticationSessionModel'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_SessionId_UserId_ExpiresAtUtc] ON [RefreshTokens] ([SessionId], [UserId], [ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109114917_CompleteAuthenticationSessionModel'
)
BEGIN
    CREATE INDEX [IX_AuthenticationSessions_UserId_RevokedAtUtc_ExpiresAtUtc] ON [AuthenticationSessions] ([UserId], [RevokedAtUtc], [ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109114917_CompleteAuthenticationSessionModel'
)
BEGIN
    ALTER TABLE [RefreshTokens] ADD CONSTRAINT [FK_RefreshTokens_AuthenticationSessions_SessionId_UserId] FOREIGN KEY ([SessionId], [UserId]) REFERENCES [AuthenticationSessions] ([Id], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261109114917_CompleteAuthenticationSessionModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261109114917_CompleteAuthenticationSessionModel', N'9.0.22');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110131824_AddOrderOwnershipHistoryAndIdempotency'
)
BEGIN
    ALTER TABLE [Orders] ADD [CustomerUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110131824_AddOrderOwnershipHistoryAndIdempotency'
)
BEGIN
    CREATE TABLE [OrderCreationReceipts] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RequestKey] uniqueidentifier NOT NULL,
        [RequestHash] char(64) NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_OrderCreationReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderCreationReceipts_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
        CONSTRAINT [FK_OrderCreationReceipts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110131824_AddOrderOwnershipHistoryAndIdempotency'
)
BEGIN
    CREATE TABLE [OrderStatusHistories] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [FromStatus] nvarchar(30) NULL,
        [ToStatus] nvarchar(30) NOT NULL,
        [ActorUserId] uniqueidentifier NOT NULL,
        [OccurredAtUtc] datetimeoffset NOT NULL,
        [Reason] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_OrderStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_OrderStatusHistories_Reason] CHECK ((
        [ToStatus] = N'Cancelled'
        AND [Reason] IS NOT NULL
        AND LEN(LTRIM(RTRIM([Reason]))) > 0
    )
    OR
    (
        [ToStatus] <> N'Cancelled'
        AND [Reason] IS NULL
    )),
        CONSTRAINT [CK_OrderStatusHistories_Transition] CHECK ((
        [FromStatus] IS NULL
        AND [ToStatus] = N'Pending'
    )
    OR
    (
        [FromStatus] IS NOT NULL
        AND
        (
            ([FromStatus] = N'Pending'
                AND [ToStatus] = N'Confirmed')
            OR
            ([FromStatus] = N'Confirmed'
                AND [ToStatus] = N'Preparing')
            OR
            ([FromStatus] = N'Preparing'
                AND [ToStatus] = N'Ready')
            OR
            ([FromStatus] = N'Ready'
                AND [ToStatus] = N'Completed')
            OR
            ([FromStatus] IN (N'Pending', N'Confirmed')
                AND [ToStatus] = N'Cancelled')
        )
    )),
        CONSTRAINT [FK_OrderStatusHistories_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
        CONSTRAINT [FK_OrderStatusHistories_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110131824_AddOrderOwnershipHistoryAndIdempotency'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerUserId_CreatedAtUtc_Id] ON [Orders] ([CustomerUserId], [CreatedAtUtc], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110131824_AddOrderOwnershipHistoryAndIdempotency'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrderCreationReceipts_OrderId] ON [OrderCreationReceipts] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110131824_AddOrderOwnershipHistoryAndIdempotency'
)
BEGIN
    CREATE UNIQUE INDEX [UX_OrderCreationReceipts_UserId_RequestKey] ON [OrderCreationReceipts] ([UserId], [RequestKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110131824_AddOrderOwnershipHistoryAndIdempotency'
)
BEGIN
    CREATE INDEX [IX_OrderStatusHistories_ActorUserId] ON [OrderStatusHistories] ([ActorUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110131824_AddOrderOwnershipHistoryAndIdempotency'
)
BEGIN
    CREATE INDEX [IX_OrderStatusHistories_OrderId_OccurredAtUtc_Id] ON [OrderStatusHistories] ([OrderId], [OccurredAtUtc], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110131824_AddOrderOwnershipHistoryAndIdempotency'
)
BEGIN
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_Users_CustomerUserId] FOREIGN KEY ([CustomerUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110131824_AddOrderOwnershipHistoryAndIdempotency'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261110131824_AddOrderOwnershipHistoryAndIdempotency', N'9.0.22');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    DROP INDEX [IX_Payments_OrderId] ON [Payments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    ALTER TABLE [Payments] ADD [CollectedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    ALTER TABLE [Payments] ADD [CollectionRequestKey] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    ALTER TABLE [Payments] ADD [RefundReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    ALTER TABLE [Payments] ADD [RefundedByUserId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    CREATE INDEX [IX_Payments_CollectedByUserId] ON [Payments] ([CollectedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    CREATE INDEX [IX_Payments_RefundedByUserId] ON [Payments] ([RefundedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Payments_OnePaidPaymentPerOrder] ON [Payments] ([OrderId]) WHERE [Status] = N''Paid''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Payments_OrderId_CollectionRequestKey] ON [Payments] ([OrderId], [CollectionRequestKey]) WHERE [CollectionRequestKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    EXEC(N'ALTER TABLE [Payments] ADD CONSTRAINT [CK_Payments_CashCollectionAudit] CHECK ((
        [CollectedByUserId] IS NULL
        AND [CollectionRequestKey] IS NULL
    )
    OR
    (
        [CollectedByUserId] IS NOT NULL
        AND [CollectionRequestKey] IS NOT NULL
        AND [Method] = N''Cash''
        AND [PaidAtUtc] IS NOT NULL
        AND [Status] IN (N''Paid'', N''Refunded'')
    ))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    EXEC(N'ALTER TABLE [Payments] ADD CONSTRAINT [CK_Payments_RefundAudit] CHECK ((
        [RefundedByUserId] IS NULL
        AND [RefundReason] IS NULL
    )
    OR
    (
        [RefundedByUserId] IS NOT NULL
        AND [Status] = N''Refunded''
        AND [RefundedAtUtc] IS NOT NULL
        AND [PaidAtUtc] IS NOT NULL
        AND [RefundedAtUtc] >= [PaidAtUtc]
        AND [RefundReason] IS NOT NULL
        AND LEN(LTRIM(RTRIM([RefundReason]))) > 0
    ))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Users_CollectedByUserId] FOREIGN KEY ([CollectedByUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Users_RefundedByUserId] FOREIGN KEY ([RefundedByUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261110144427_AddCashPaymentAuditAndCollectionKeys'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261110144427_AddCashPaymentAuditAndCollectionKeys', N'9.0.22');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111104652_AddOrderReviews'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [AuthorUserId] uniqueidentifier NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(2000) NULL,
        [IsHidden] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAtUtc] datetimeoffset NULL,
        [LastModeratedByUserId] uniqueidentifier NULL,
        [LastModeratedAtUtc] datetimeoffset NULL,
        [LastModerationReason] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Reviews_Deletion] CHECK ((
        [IsDeleted] = 0
        AND [DeletedAtUtc] IS NULL
    )
    OR
    (
        [IsDeleted] = 1
        AND [DeletedAtUtc] IS NOT NULL
        AND [Comment] IS NULL
    )),
        CONSTRAINT [CK_Reviews_DeletionTimestamp] CHECK ([DeletedAtUtc] IS NULL
    OR [DeletedAtUtc] >= [CreatedAtUtc]),
        CONSTRAINT [CK_Reviews_Moderation] CHECK ((
        [IsHidden] = 0
        AND [LastModeratedByUserId] IS NULL
        AND [LastModeratedAtUtc] IS NULL
        AND [LastModerationReason] IS NULL
    )
    OR
    (
        [LastModeratedByUserId] IS NOT NULL
        AND [LastModeratedAtUtc] IS NOT NULL
        AND [LastModerationReason] IS NOT NULL
        AND LEN(LTRIM(RTRIM([LastModerationReason]))) > 0
    )),
        CONSTRAINT [CK_Reviews_ModerationTimestamp] CHECK ([LastModeratedAtUtc] IS NULL
    OR [LastModeratedAtUtc] >= [CreatedAtUtc]),
        CONSTRAINT [CK_Reviews_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
        CONSTRAINT [FK_Reviews_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]),
        CONSTRAINT [FK_Reviews_Users_AuthorUserId] FOREIGN KEY ([AuthorUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_Reviews_Users_LastModeratedByUserId] FOREIGN KEY ([LastModeratedByUserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111104652_AddOrderReviews'
)
BEGIN
    CREATE INDEX [IX_Reviews_AuthorUserId_CreatedAtUtc_Id] ON [Reviews] ([AuthorUserId], [CreatedAtUtc], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111104652_AddOrderReviews'
)
BEGIN
    CREATE INDEX [IX_Reviews_IsDeleted_IsHidden_CreatedAtUtc_Id] ON [Reviews] ([IsDeleted], [IsHidden], [CreatedAtUtc], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111104652_AddOrderReviews'
)
BEGIN
    CREATE INDEX [IX_Reviews_LastModeratedByUserId] ON [Reviews] ([LastModeratedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111104652_AddOrderReviews'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Reviews_OrderId] ON [Reviews] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111104652_AddOrderReviews'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111104652_AddOrderReviews', N'9.0.22');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111120358_AddCustomerProfilesAndDeliveryAddresses'
)
BEGIN
    CREATE TABLE [CustomerProfiles] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [PhoneNumber] nvarchar(30) NOT NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_CustomerProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomerProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111120358_AddCustomerProfilesAndDeliveryAddresses'
)
BEGIN
    CREATE TABLE [DeliveryAddresses] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerProfileId] uniqueidentifier NOT NULL,
        [Label] nvarchar(50) NOT NULL,
        [NormalizedLabel] nvarchar(100) NOT NULL,
        [RecipientName] nvarchar(150) NOT NULL,
        [RecipientPhone] nvarchar(30) NOT NULL,
        [City] nvarchar(80) NOT NULL,
        [District] nvarchar(80) NULL,
        [StreetAddress] nvarchar(180) NOT NULL,
        [BuildingDetails] nvarchar(80) NULL,
        [DeliveryInstructions] nvarchar(300) NULL,
        [RowVersion] rowversion NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_DeliveryAddresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DeliveryAddresses_CustomerProfiles_CustomerProfileId] FOREIGN KEY ([CustomerProfileId]) REFERENCES [CustomerProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111120358_AddCustomerProfilesAndDeliveryAddresses'
)
BEGIN
    CREATE UNIQUE INDEX [UX_CustomerProfiles_UserId] ON [CustomerProfiles] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111120358_AddCustomerProfilesAndDeliveryAddresses'
)
BEGIN
    CREATE UNIQUE INDEX [UX_DeliveryAddresses_Profile_Label] ON [DeliveryAddresses] ([CustomerProfileId], [NormalizedLabel]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111120358_AddCustomerProfilesAndDeliveryAddresses'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111120358_AddCustomerProfilesAndDeliveryAddresses', N'9.0.22');
END;

COMMIT;
GO

