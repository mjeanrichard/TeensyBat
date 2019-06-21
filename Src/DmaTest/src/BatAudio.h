#ifndef BATAUDIO_h
#define BATAUDIO_h

#include "Config.h"
#include "DMAChannel.h"
#include "arm_math.h"
#include "arm_const_structs.h"
#include <ADC.h>
#include "sqrt_integer.h"
#include "dspinst.h"
#include "SdFat.h"

struct CallPointer
{
	uint8_t * startOfData;
	uint16_t length;
	uint32_t startTime;
};

class BatAudio
{
private:
	static BatAudio * _instance;
	static DMAChannel _dma;
	static ADC _adc;

	static void adc0_isr();
	friend void software_isr(void);
	friend void adc1_isr(void);

	volatile uint16_t _lastEnvelopeValue;
	
	bool _isEnabled = false;

	// 3 Sample Buffers, configured as circular Buffer, to mangage the 50% overlap for the FFT
	// _currentSamplingBuffer is being filled by DMA while the other two are used for FFT
	uint16_t _sampleBuffer[TB_AUDIO_SAMPLE_BUFFER_SIZE*3] __attribute__((aligned(4)));
	uint16_t * volatile _currentSamplingBuffer = _sampleBuffer;
	uint16_t * volatile _completedSamplingBuffer = &(_sampleBuffer[TB_AUDIO_SAMPLE_BUFFER_SIZE]);
	uint16_t * volatile _lastSamplingBuffer = &_sampleBuffer[TB_AUDIO_SAMPLE_BUFFER_SIZE * 2];
	void rotateSamplingBuffer();

	// The PreCallBuffer is a circular Buffer for computed FFT results.
	// It is used to record a few samples bevor the actuall call started.
	uint8_t _preCallBuffer[TB_CALL_DATA_SIZE * TB_PRE_CALL_BUFFER_COUNT] __attribute__((aligned(4)));
	uint8_t _preCallBufferIndex = 0;
	uint8_t _preCallBufferCount = 0;
	uint8_t _afterCallSampleCount = 0;

	// A counter that is increased for every sample (AUDIO_SAMPLE_BUFFER_SIZE) taken (will overflow).
	uint16_t _sampleCounter = 0;

	// Ring Buffer with the recorded calls. Each CallPointer point to a section in the following 
	// _callBuffer for its raw FFT Data.
	CallPointer _callPointers[TB_CALL_POINTER_COUNT];
	CallPointer * _currentCall;
	volatile uint8_t _callPointerIndexHead = 0;
	volatile uint8_t _callPointerIndexTail = 0;

	// Ring Buffer for the raw call data waiting to be written to the sd card.
	uint8_t _callBuffer[TB_CALL_DATA_SIZE * TB_CALL_BUFFER_COUNT];
	const uint8_t * _callBufferEnd = &_callBuffer[TB_CALL_DATA_SIZE * TB_CALL_BUFFER_COUNT - 1];
	uint8_t * volatile _callBufferNextByte = &_callBuffer[0];
	uint8_t * volatile _callBufferFirstByte = &_callBuffer[0];
	volatile uint16_t _callBufferEntries = 0;

	// Indicates if we are currently recording a call
	volatile uint8_t _isCallInProgress = false;

	// True if the Software interrupt routine is busy processing the last sample. If so,
	// the next softwre interrupt will not be fired.
	volatile bool _isProcessingSample = false;

	int16_t _fftBuffer[TB_FFT_RESULT_SIZE*4] __attribute__((aligned(4)));
	void computeFFT(uint8_t * dest);
	void copyToCallBuffer(uint8_t * src);
	void increaseCallBuffer();

	void sample_complete_isr();

public:
	BatAudio()
	{
		_instance = this;
	}

	void init();
	void start();
	void stop();

	bool hasDataAvailable();

	// This Method can only be used if this object is stopped.
	int16_t readRawBatteryVoltage();
	int16_t readTempC();

	void debug();
	void sendOverUsb();
	void writeToCard(uint16_t * blocksWritten, SdFat * sd, uint16_t blockCount, uint8_t * sdBuffer);
};

const uint16_t AudioWindowHanning256[] __attribute__ ((aligned (4))) = {
     0,     5,    20,    45,    80,   124,   179,   243,   317,   401,
   495,   598,   711,   833,   965,  1106,  1257,  1416,  1585,  1763,
  1949,  2145,  2349,  2561,  2782,  3011,  3249,  3494,  3747,  4008,
  4276,  4552,  4834,  5124,  5421,  5724,  6034,  6350,  6672,  7000,
  7334,  7673,  8018,  8367,  8722,  9081,  9445,  9812, 10184, 10560,
 10939, 11321, 11707, 12095, 12486, 12879, 13274, 13672, 14070, 14471,
 14872, 15275, 15678, 16081, 16485, 16889, 17292, 17695, 18097, 18498,
 18897, 19295, 19692, 20086, 20478, 20868, 21255, 21639, 22019, 22397,
 22770, 23140, 23506, 23867, 24224, 24576, 24923, 25265, 25602, 25932,
 26258, 26577, 26890, 27196, 27496, 27789, 28076, 28355, 28627, 28892,
 29148, 29398, 29639, 29872, 30097, 30314, 30522, 30722, 30913, 31095,
 31268, 31432, 31588, 31733, 31870, 31997, 32115, 32223, 32321, 32410,
 32489, 32558, 32618, 32667, 32707, 32737, 32757, 32767, 32767, 32757,
 32737, 32707, 32667, 32618, 32558, 32489, 32410, 32321, 32223, 32115,
 31997, 31870, 31733, 31588, 31432, 31268, 31095, 30913, 30722, 30522,
 30314, 30097, 29872, 29639, 29398, 29148, 28892, 28627, 28355, 28076,
 27789, 27496, 27196, 26890, 26577, 26258, 25932, 25602, 25265, 24923,
 24576, 24224, 23867, 23506, 23140, 22770, 22397, 22019, 21639, 21255,
 20868, 20478, 20086, 19692, 19295, 18897, 18498, 18097, 17695, 17292,
 16889, 16485, 16081, 15678, 15275, 14872, 14471, 14070, 13672, 13274,
 12879, 12486, 12095, 11707, 11321, 10939, 10560, 10184,  9812,  9445,
  9081,  8722,  8367,  8018,  7673,  7334,  7000,  6672,  6350,  6034,
  5724,  5421,  5124,  4834,  4552,  4276,  4008,  3747,  3494,  3249,
  3011,  2782,  2561,  2349,  2145,  1949,  1763,  1585,  1416,  1257,
  1106,   965,   833,   711,   598,   495,   401,   317,   243,   179,
   124,    80,    45,    20,     5,     0,
};

#endif