
PRAGMA foreign_keys = ON;

-- GO

CREATE TABLE [DbVersion] (
  [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  [Version] INTEGER NOT NULL,
  [Name] nvarchar(1) NOT NULL
);

-- GO

CREATE TABLE [BatNodeLog] (
  [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  [Longitude] float NULL, 
  [Latitude] float NULL, 
  [LogStart] datetime NOT NULL, 
  [Name] nvarchar(2147483647) NOT NULL, 
  [Description] nvarchar(2147483647) NULL,
  [CallCount] int NULL
);

-- GO

CREATE TABLE [BatCalls] (
  [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
  [BatNodeLogId] int NOT NULL CONSTRAINT FK_BatCalls_BatNodeLog REFERENCES BatNodeLog(Id), 
  [StartTimeMs] int NOT NULL, 
  [Duration] int NOT NULL, 
  [MaxFrequency] int NOT NULL, 
  [MaxIntensity] int NOT NULL, 
  [AvgFrequency] int NOT NULL, 
  [AvgIntensity] int NOT NULL, 
  [Enabled] bool NOT NULL DEFAULT 1
);

-- GO

CREATE INDEX [IFK_BatNodeLog_BatCalls] ON [BatCalls] ([BatNodeLogId]);
