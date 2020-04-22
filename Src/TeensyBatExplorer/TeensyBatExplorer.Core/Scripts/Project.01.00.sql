CREATE TABLE 'Projects' (
    'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
    'Name'	TEXT NOT NULL,
    'CreatedOn'	TEXT NOT NULL
);

CREATE TABLE 'DataFileEntries' (
    'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
    'FftCount'	INTEGER NOT NULL,
    'StartTimeMillis' INTEGER NOT NULL,
    'StartTimeMicros' INTEGER NOT NULL,
    'PauseFromPrevEntryMicros' INTEGER NULL,
    'MaxPeakFrequency' REAL NOT NULL,
    'AvgPeakFrequency' REAL NOT NULL,
    'IsBat' INTEGER NOT NULL,

    'HighFreqSampleCount' INTEGER NOT NULL,
    'HighPowerSampleCount' INTEGER NOT NULL,
    'MaxLevel' INTEGER NOT NULL,
   
    'DataFileId' INTEGER NOT NULL,
    'CallId' INTEGER NULL
);

CREATE TABLE 'BatteryData' (
    'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
    'Voltage'	INTEGER NOT NULL,
    'DateTime'    TEXT NOT NULL,
    'Timestamp'    INTEGER NOT NULL,

    'DataFileId' INTEGER NOT NULL
);

CREATE TABLE 'TemperatureData' (
    'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
    'Temperature'	INTEGER NOT NULL,
    'DateTime'    TEXT NOT NULL,
    'Timestamp'    INTEGER NOT NULL,
   
    'DataFileId' INTEGER NOT NULL
);

CREATE TABLE 'FftBlocks' (
    'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
    'Index'	INTEGER NOT NULL,
    'Loudness'	INTEGER NOT NULL,
    'SampleNr'	INTEGER NOT NULL,
    'Data'	BLOB NOT NULL,
   
    'DataFileEntryId' INTEGER NOT NULL
);

CREATE TABLE 'Nodes' (
    'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
    'NodeNumber' INTEGER NOT NULL,
    'StartTime' TEXT NOT NULL,
    'CallStartThreshold' INTEGER NOT NULL,
    'CallEndThreshold' INTEGER NOT NULL,
    'Longitude' REAL NULL,
    'Latitude' REAL NULL
);

CREATE TABLE 'Calls' (
    'Id'                INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
    'StartTime'         TEXT NOT NULL,
    'StartTimeMicros'   INTEGER NOT NULL,
    'DurationMicros'    INTEGER NOT NULL,
    'PeakFrequency'     INTEGER NOT NULL,
    'NodeId'            INTEGER NULL
);

CREATE TABLE 'DataFiles' (
    'Id'    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
    'NodeNumber'    INTEGER NOT NULL,
    'FirmwareVersion'    INTEGER NOT NULL,
    'HardwareVersion'    INTEGER NOT NULL,
    'Debug'    INTEGER NOT NULL,
    'OriginalReferenceTime'    TEXT NOT NULL,
    'ReferenceTime'    TEXT NOT NULL,
    'FileCreateTime'    TEXT NOT NULL,
    'PreCallBufferSize'    INTEGER NOT NULL,
    'AfterCallBufferSize'    INTEGER NOT NULL,
    'CallStartThreshold'    INTEGER NOT NULL,
    'CallEndThreshold'    INTEGER NOT NULL,
    'ErrorCountCallBuffFull'    INTEGER NOT NULL,
    'ErrorCountPointerBufferFull'    INTEGER NOT NULL,
    'ErrorCountDataBufferFull'    INTEGER NOT NULL,
    'ErrorCountProcessOverlap'    INTEGER NOT NULL,
    
    'Filename'    TEXT NOT NULL,
    'NodeId'    INTEGER NULL
);

CREATE TABLE 'ProjectMessages' (
    'Id'	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
    'Timestamp' TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    'DataFileId' INTEGER NULL,
    'NodeId' INTEGER NULL,
    'Message' TEXT NOT NULL,
    'MessageType' TEXT NOT NULL,
    'Level' TEXT NOT NULL,
    'Position' INTEGER NULL
);