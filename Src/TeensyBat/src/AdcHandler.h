// AdcHandler.h

#ifndef ADCHANDLER_h
#define ADCHANDLER_h

#include "WProgram.h"
#include <ADC.h>
#include "Config.h"

class AdcHandler
{
private:
	static int16_t samples1[TB_FFT_SIZE];
	static int16_t samples2[TB_FFT_SIZE];

	static int16_t* sampleBuffer;
	static uint16_t bufIndex;

	static uint8_t power1[POWER_BUF_SIZE];
	static uint8_t power2[POWER_BUF_SIZE];

	static uint8_t* powerSampleBuffer;
	static uint16_t powerIndex;

	static ADC* adc;
	static IntervalTimer envelopeTimer;

	static void envelopeTimerIsr(void);

public:
	static int16_t * volatile readyBuffer;
	static uint8_t * volatile powerReadyBuffer;

	static volatile uint16_t powerReadyCount;

	static volatile uint16_t MissedSamples;
	static volatile uint16_t ClippedSignalCount;

	static uint16_t ReadRawBatteryVoltage();
	
	static void InitAdc();
	static void Start();
	static void Stop();

	static void HandleAdc0Isr();
	static void HandleAdc1Isr();
};

#endif
