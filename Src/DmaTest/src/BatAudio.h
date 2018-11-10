#ifndef BATAUDIO_h
#define BATAUDIO_h

#include "DMAChannel.h"
#include "arm_math.h"
#include "arm_const_structs.h"
#include <ADC.h>
#include "sqrt_integer.h"
#include "dspinst.h"
#include "SdFat.h"


#define AUDIO_SAMPLE_BUFFER_SIZE 128


// TEMP
static const uint8_t TB_PIN_LED_GREEN = 6;
static const uint8_t TB_PIN_LED_YELLOW = 5;
static const uint8_t TB_PIN_LED_RED = 4;

static const uint8_t TB_PIN_AUDIO = A9;
static const uint8_t TB_PIN_ENVELOPE = A3;
static const uint8_t TB_PIN_BATTERY = A2;

static const uint8_t TB_PIN_SDCS = 10;



static const uint16_t FFT_RESULT_SIZE = AUDIO_SAMPLE_BUFFER_SIZE / 2;
static const uint16_t CALL_DATA_SIZE = FFT_RESULT_SIZE + 4;

static const uint8_t PRE_CALL_BUFFER_COUNT = 4;
static const uint8_t AFTER_CALL_SAMPLES = 4;

static const uint16_t CALL_START_THRESHOLD = 500;
static const uint16_t CALL_STOP_THRESHOLD = 100;

static const uint16_t CALL_BUFFER_COUNT = 600;
static const uint16_t CALL_POINTER_COUNT = 50;



// END TEMP

struct CallPointer
{
	uint8_t * startOfData;
	uint16_t length;
	uint32_t startTime;
};


class BatAudio
{
private:
	static BatAudio * _self;

	ADC * _adc;

	static DMAChannel _dma;

	const arm_cfft_instance_q15 * _cfftData;
	int16_t _fftBuffer[AUDIO_SAMPLE_BUFFER_SIZE*2] __attribute__((aligned(4)));
	void computeFFT(uint8_t * dest);
	void copyToCallBuffer(uint8_t * src);
	void increaseCallBuffer();

	volatile uint16_t _lastEnvelopeValue;

	static void adc0_isr();
	void sample_complete_isr();

	friend void software_isr(void);
	friend void adc1_isr(void);

public:
	BatAudio()
	{
		_self = this;
		_adc = new ADC();
	}

	void init();
	void start();
	void stop();

	bool hasDataAvailable();

	void debug();
	void sendOverUsb();
	void writeToCard(uint16_t * blocksWritten, SdFat * sd, uint16_t blockCount);
};
#endif