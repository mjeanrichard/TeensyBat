#ifndef BATLOG_h
#define BATLOG_h

#include "SdFat.h"
#include "BatCall.h"

const int TB_ERROR_OPEN_CARD = 2;
const int TB_ERROR_OPEN_FILE = 3;
const int TB_ERROR_LOG_WRITE = 4;
const int TB_ERROR_SYNC_FILE = 5;


class BatLog
{
private:
	SdFat _sd;
	File _file;

	char _filename[21];

	byte _nodeId = 1;

	bool InitLogFile();
	void WriteLogHeader();
	void WriteInfo(BatInfo* info);
	void WriteCall(BatCall* call);

	void FatalError(int code);

public:
	void LogCalls(BatCall calls[], uint8_t callsLength, BatInfo infos[], uint8_t infoLength);
	void SetNodeId(uint8_t nodeId);
};


#endif

