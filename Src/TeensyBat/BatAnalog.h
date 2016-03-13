
#ifndef BATAUDIO_h
#define BATAUDIO_h

#include "AdcHandler.h"
#define ARM_MATH_CM4
#include <arm_math.h>
#include <SPI.h>
#include "SdFat/SdFat.h"

const int TB_ERROR_OPEN_CARD = 2;
const int TB_ERROR_OPEN_FILE = 3;
const int TB_ERROR_LOG_WRITE = 4;


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

class BatAnalog
{
private:
	static void copy_to_fft_buffer(void *destination, const void *source);
	static void apply_window_to_fft_buffer(void *buffer);

	int _currentCallIndex;
	BatCall _callLog[10];

	SdFat _sd;
	SdFile _file;

	elapsedMicros _callDuration;

	//FFT
	int16_t _complexBuffer[TB_DOUBLE_FFT_SIZE] __attribute__((aligned(4)));

	arm_cfft_radix4_instance_q15 _fft_inst;

	bool _isFileReady = false;
	char _filename[11];

	byte _nodeId = 1;

	void LogCalls();
	void WriteLogHeader();
	void InitLogFile(bool forceReset);
	bool WriteLog();

	void Error(int code);

public:
	void init();
	void start();
	void stop();
	
	void process();
};
#endif