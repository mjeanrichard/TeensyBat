
#ifndef BATAUDIO_h
#define BATAUDIO_h

#define ARM_MATH_CM4
#include <arm_math.h>
#include <SPI.h>
#include "Config.h"
#include "BatCall.h"
#include "BatLog.h"
#include "dspinst.h"
#include "sqrt_integer.h"
#include "AdcHandler.h"
#include <EEPROM.h>

#ifdef TB_DISPLAY
#include "Display.h"
#endif

class BatAnalog
{
private:
	static void copy_to_fft_buffer(void *destination, const void *source);
	static void apply_window_to_fft_buffer(void *buffer);

	uint8_t _currentCallIndex = 0;
	BatCall _callLog[TB_LOG_BUFFER_LENGTH];
	uint8_t _currentInfoIndex = 0;
	BatInfo _infoLog[TB_LOG_BUFFER_LENGTH];

	elapsedMicros _callDuration = 0;

	elapsedMillis _msSinceLastInfoLog = 0;
	elapsedMillis _msSinceLastCall = 0;

	elapsedMicros _usSinceLastSample = 0;
	uint32_t _lastSampleDuration = 0;

	//FFT
	int16_t _complexBuffer[TB_DOUBLE_FFT_SIZE] __attribute__((aligned(4)));

	arm_cfft_radix4_instance_q15 _fft_inst;

	byte _nodeId = 1;
	BatLog _log;

	void AddInfoLog();
	void CheckLog();

public:
	void init();
	void start();
	void stop();

	void ApplyFftSample(uint32_t* binData);
	void process();
};
#endif