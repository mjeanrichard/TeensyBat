// AdcHandler.h

#ifndef ADCHANDLER_h
#define ADCHANDLER_h

#include <ADC.h>
#include "Config.h"

class AdcHandler
{
private:
	static int16_t samples1[TB_FFT_SIZE];
	static int16_t samples2[TB_FFT_SIZE];

	static int16_t* sampleBuffer;

	static unsigned int volatile bufIndex;

	static ADC* adc;


public:
	static int16_t * readyBuffer;

	static volatile unsigned int MissedSamples;
	static volatile unsigned int ClippedSignalCount;

	static uint16_t ReadEnvelope();

	static void InitAdc();
	static void Start();
	static void Stop();

	static void HandleAdc0Isr();
};

#endif
