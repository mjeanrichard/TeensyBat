
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

	int currentCallIndex;
	BatCall callLog[10];

	SdFat sd;
	SdFile file;

	elapsedMicros callDuration;

	//FFT
	int16_t complexBuffer[TB_DOUBLE_FFT_SIZE] __attribute__((aligned(4)));

	arm_cfft_radix4_instance_q15 fft_inst;

	bool isFileReady = false;

	void LogCalls();
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