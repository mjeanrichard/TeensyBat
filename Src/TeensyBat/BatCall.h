#ifndef BATCALL_h
#define BATCALL_h

#include "Config.h"

class BatCall
{
private:
public:
	uint32_t startTimeMs = 0;
	uint32_t durationMicros = 0;
	uint16_t sampleCount = 0;
	uint16_t missedSamples = 0;
	uint16_t clippedSamples = 0;
	uint16_t maxPower = 0;
	uint32_t data[TB_QUART_FFT_SIZE] __attribute__((aligned(4)));
};

class BatInfo
{
private:
public:
	uint32_t time = 0;
	uint32_t startTimeMs = 0;
	uint16_t BatteryVoltage = 0;
	uint16_t LastBufferDuration = 0;
};

#endif
