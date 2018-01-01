#include "BatLog.h"
#include <TimeLib.h>

void BatLog::LogCalls(BatCall calls[], uint8_t callsLength, BatInfo infos[], uint8_t infoLength)
{
#ifdef TB_DEBUG
	Serial.printf(F("Logging Calls (%hhu) and Infos (%hhu).\n"), callsLength, infoLength);
#endif

	if (InitLogFile())
	{
		int idx = 0;
		while (idx < callsLength && _file.getError() == 0)
		{
			BatCall * currentCall = &calls[idx];
			WriteCall(currentCall);
			idx++;
		}
		idx = 0;
		while(idx < infoLength && _file.getError() == 0)
		{
			BatInfo *currentInfo = &infos[idx];
			WriteInfo(currentInfo);
			idx++;
		}

		if (_file.getError() == 0)
		{
			if (!_file.sync())
			{
#ifdef TB_DEBUG
				Serial.printf(F("Failed to Sync File: %d.\n"), _file.getError());
#endif
				FatalError(TB_ERROR_SYNC_FILE);
			}
		} else
		{
#ifdef TB_DEBUG
			Serial.printf(F("Failed to Write Log-Data: %d.\n"), _file.getError());
#endif
			FatalError(TB_ERROR_LOG_WRITE);
		}
	}
}

void BatLog::SetNodeId(uint8_t nodeId)
{
	_nodeId = nodeId;
}

void BatLog::WriteLogHeader()
{
	_file.write("TBL");
	_file.write(2);
	_file.write(_nodeId);
	uint32_t time = Teensy3Clock.get();
	_file.write(&time, 4);
#ifdef TB_DEBUG
	Serial.printf(F("Writing Header at %lu ms for Node %02hhu.\n"), time, _nodeId);
#endif
}

void BatLog::WriteInfo(BatInfo * info)
{
	_file.write(255);
	_file.write(244);

	_file.write(&info->time, 4);
	_file.write(&info->startTimeMs, 4);
	_file.write(&info->BatteryVoltage, 2);
	_file.write(&info->LastBufferDuration, 2);
}

void BatLog::WriteCall(BatCall * call)
{
	_file.write(255);
	_file.write(255);

	_file.write(&call->durationMicros, 4);
	_file.write(&call->startTimeMs, 4);
	_file.write(&call->clippedSamples, 2);
	_file.write(&call->missedSamples, 2);

	_file.write(&call->powerDataLength, 2);
	_file.write(call->powerData, call->powerDataLength);

	uint16_t * val = ((uint16_t *)call->data);
	for (int i = 0; i < TB_QUART_FFT_SIZE; i++)
	{
		_file.write(val, 2);
		val += 2;
	}
}

bool BatLog::InitLogFile()
{
	if (digitalReadFast(TB_PIN_CARD_PRESENT) == HIGH)
	{
#ifdef TB_DEBUG
		Serial.println(F("No SD-Card present!"));
#endif
		return false;
	}

	bool isFileOpen = _file.isOpen();
	uint8_t error = _file.getError();

	if (isFileOpen && error == 0)
	{
		return true;
	}

#ifdef TB_DEBUG
	Serial.printf(F("Initializing SD Card (IsOpen %d, Error %d).\n"), isFileOpen, error);
#endif

	if (!_sd.begin(TB_PIN_SDCS, SPI_FULL_SPEED)) {
#ifdef TB_DEBUG 
		_sd.initErrorPrint();
#endif
		FatalError(TB_ERROR_OPEN_CARD);
	}
	if (isFileOpen)
	{
		_file.close();
	}
	_file.clearError();

	tmElements_t time;
	breakTime(Teensy3Clock.get(), time);

	//TBXX-YYYYMMDDHHMM.DAT
	sprintf(_filename, "TB%02hhu-%04u%02hhu%02hhu%02hhu%02hhu.DAT", _nodeId, 1970 + time.Year, time.Month, time.Day, time.Hour, time.Minute);

	bool createdNewFile = !_file.exists(_filename);

	if (!_file.open(_filename, O_RDWR | O_CREAT | O_AT_END)) {
#ifdef TB_DEBUG
		Serial.printf(F("Could not create/open File (Error %d)!\n"), _file.getError());
#endif
		FatalError(TB_ERROR_OPEN_FILE);
	}

	if (createdNewFile) {
		_file.timestamp(T_CREATE | T_ACCESS | T_WRITE, 1970 + time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
		WriteLogHeader();
	}
	return true;
}

void BatLog::FatalError(int code)
{
#ifdef TB_DEBUG
	Serial.printf(F("Fatal Error: %d.\n"), code);
#endif

	while (true)
	{
		for (int i = 0; i < code; i++)
		{
			digitalWriteFast(TB_PIN_LED_RED, HIGH);
			delay(500);
			digitalWriteFast(TB_PIN_LED_RED, LOW);
			delay(500);
		}
		delay(1000);
	}
}

