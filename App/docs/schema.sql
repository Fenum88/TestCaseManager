CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE TABLE "AuditLog" (
        "Id" uuid NOT NULL,
        "Timestamp" timestamp with time zone NOT NULL,
        "UserName" character varying(100) NOT NULL,
        "Action" integer NOT NULL,
        "EntityName" character varying(100) NOT NULL,
        "EntityId" uuid NOT NULL,
        "Details" character varying(500),
        CONSTRAINT "PK_AuditLog" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE TABLE "Categories" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Description" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(100) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(100),
        CONSTRAINT "PK_Categories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE TABLE "Tags" (
        "Id" uuid NOT NULL,
        "Name" character varying(50) NOT NULL,
        CONSTRAINT "PK_Tags" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE TABLE "TestCases" (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Description" character varying(2000),
        "Version" character varying(50) NOT NULL,
        "Status" integer NOT NULL,
        "CategoryId" uuid,
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(100) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(100),
        CONSTRAINT "PK_TestCases" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TestCases_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE TABLE "Attachments" (
        "Id" uuid NOT NULL,
        "TestCaseId" uuid NOT NULL,
        "FileName" character varying(260) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "FileSize" bigint NOT NULL,
        "Content" bytea NOT NULL,
        "Description" character varying(500),
        "CreatedAt" timestamp with time zone NOT NULL,
        "CreatedBy" character varying(100) NOT NULL,
        "ModifiedAt" timestamp with time zone,
        "ModifiedBy" character varying(100),
        CONSTRAINT "PK_Attachments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Attachments_TestCases_TestCaseId" FOREIGN KEY ("TestCaseId") REFERENCES "TestCases" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE TABLE "ExpectedResults" (
        "Id" uuid NOT NULL,
        "TestCaseId" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "DataType" integer NOT NULL,
        "ExpectedValue" text NOT NULL,
        "Tolerance" character varying(50),
        "Description" character varying(500),
        "SortOrder" integer NOT NULL,
        CONSTRAINT "PK_ExpectedResults" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ExpectedResults_TestCases_TestCaseId" FOREIGN KEY ("TestCaseId") REFERENCES "TestCases" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE TABLE "TestCaseHistories" (
        "Id" uuid NOT NULL,
        "TestCaseId" uuid NOT NULL,
        "ChangeType" integer NOT NULL,
        "ChangedBy" character varying(100) NOT NULL,
        "ChangedAt" timestamp with time zone NOT NULL,
        "PropertyName" character varying(300),
        "OldValue" text,
        "NewValue" text,
        CONSTRAINT "PK_TestCaseHistories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TestCaseHistories_TestCases_TestCaseId" FOREIGN KEY ("TestCaseId") REFERENCES "TestCases" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE TABLE "TestCaseTags" (
        "TestCaseId" uuid NOT NULL,
        "TagId" uuid NOT NULL,
        CONSTRAINT "PK_TestCaseTags" PRIMARY KEY ("TestCaseId", "TagId"),
        CONSTRAINT "FK_TestCaseTags_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TestCaseTags_TestCases_TestCaseId" FOREIGN KEY ("TestCaseId") REFERENCES "TestCases" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE TABLE "TestParameters" (
        "Id" uuid NOT NULL,
        "TestCaseId" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "DataType" integer NOT NULL,
        "Value" text,
        "DefaultValue" text,
        "Description" character varying(500),
        "IsRequired" boolean NOT NULL,
        "SortOrder" integer NOT NULL,
        CONSTRAINT "PK_TestParameters" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TestParameters_TestCases_TestCaseId" FOREIGN KEY ("TestCaseId") REFERENCES "TestCases" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE INDEX "IX_Attachments_TestCaseId" ON "Attachments" ("TestCaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE INDEX "IX_AuditLog_Timestamp" ON "AuditLog" ("Timestamp");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Categories_Name" ON "Categories" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE INDEX "IX_ExpectedResults_TestCaseId" ON "ExpectedResults" ("TestCaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Tags_Name" ON "Tags" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE INDEX "IX_TestCaseHistories_TestCaseId_ChangedAt" ON "TestCaseHistories" ("TestCaseId", "ChangedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE INDEX "IX_TestCases_CategoryId" ON "TestCases" ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE INDEX "IX_TestCases_ModifiedAt" ON "TestCases" ("ModifiedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_TestCases_Name" ON "TestCases" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE INDEX "IX_TestCases_Status" ON "TestCases" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE INDEX "IX_TestCaseTags_TagId" ON "TestCaseTags" ("TagId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_TestParameters_TestCaseId_Name" ON "TestParameters" ("TestCaseId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260713174309_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260713174309_InitialCreate', '9.0.1');
    END IF;
END $EF$;
COMMIT;

