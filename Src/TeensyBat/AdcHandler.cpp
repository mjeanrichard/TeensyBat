#include "AdcHandler.h"
#include "Config.h"

ADC* AdcHandler::adc = new ADC();
IntervalTimer AdcHandler::envelopeTimer;

// Audio Samples for FFT
int16_t AdcHandler::samples1[TB_FFT_SIZE] __attribute__((aligned(4)));
int16_t AdcHandler::samples2[TB_FFT_SIZE] __attribute__((aligned(4)));
int16_t* AdcHandler::sampleBuffer = samples1;

// Power (Envelope) Samples
uint8_t AdcHandler::power1[POWER_BUF_SIZE];
uint8_t AdcHandler::power2[POWER_BUF_SIZE];
uint8_t* AdcHandler::powerSampleBuffer = power1;

uint16_t AdcHandler::bufIndex = 0;
uint16_t AdcHandler::powerIndex = 0;

int16_t * volatile AdcHandler::readyBuffer = nullptr;
uint8_t * volatile AdcHandler::powerReadyBuffer = nullptr;

volatile uint16_t AdcHandler::MissedSamples = 0;
volatile uint16_t AdcHandler::ClippedSignalCount = 0;
volatile uint16_t AdcHandler::powerReadyCount = 0;

void AdcHandler::Start()
{
	adc->startContinuous(TB_PIN_AUDIO, ADC_0);
	envelopeTimer.begin(envelopeTimerIsr, 250);
}

void AdcHandler::Stop()
{
	envelopeTimer.end();
	adc->stopContinuous(ADC_0);
}

void AdcHandler::InitAdc()
{
	pinMode(TB_PIN_AUDIO, INPUT);
	pinMode(TB_PIN_ENVELOPE, INPUT);

	// ADC_0 is used for sampling the Audio
	adc->setAveraging(0, ADC_0);
	adc->setResolution(12, ADC_0);
	adc->setConversionSpeed(ADC_MED_SPEED, ADC_0);
	adc->setSamplingSpeed(ADC_MED_SPEED, ADC_0);
	adc->enableInterrupts(ADC_0);

	// ADC_1 is used for everthing else.
	adc->setAveraging(8, ADC_1);
	adc->setResolution(8, ADC_1);
	adc->setConversionSpeed(ADC_MED_SPEED, ADC_1);
	adc->setSamplingSpeed(ADC_MED_SPEED, ADC_1);
	adc->enableInterrupts(ADC_1);
}


uint16_t AdcHandler::ReadBatteryVoltage()
{
	adc->disableInterrupts(ADC_1);
	noInterrupts();
	adc->setResolution(12, ADC_1);

	int16_t voltage = adc->analogRead(TB_PIN_BATTERY, ADC_1);

	adc->setResolution(8, ADC_1);
	interrupts();
	adc->enableInterrupts(ADC_1);

	return (voltage * 2550) / 1000;
}

void AdcHandler::HandleAdc0Isr()
{
	int16_t s = adc->analogReadContinuous(ADC_0);
	if (bufIndex >= TB_FFT_SIZE)
	{
		if (readyBuffer != nullptr)
		{
			//FFT was slower that ADC -> we are loosing Samples!
			MissedSamples++;
			return;
		}

		readyBuffer = sampleBuffer;
		powerReadyBuffer = powerSampleBuffer;
		if (sampleBuffer == samples1)
		{
			sampleBuffer = samples2;
			powerSampleBuffer = power1;
		}
		else
		{
			sampleBuffer = samples1;
			powerSampleBuffer = power2;
		}
		bufIndex = 0;

		powerReadyCount = powerIndex;
		powerIndex = 0;
	}
	sampleBuffer[bufIndex] = s;
	bufIndex++;
	if (s >= 4094 || s <= 2)
	{
		ClippedSignalCount++;
	}
}

void AdcHandler::HandleAdc1Isr()
{
	int8_t s = adc->readSingle(ADC_1);
	if (powerIndex < POWER_BUF_SIZE) {
		powerSampleBuffer[powerIndex] = s;
		powerIndex++;
	}
}

void AdcHandler::envelopeTimerIsr(void)
{
	adc->startSingleRead(TB_PIN_ENVELOPE, ADC_1);
}

void adc0_isr(void)
{
	AdcHandler::HandleAdc0Isr();
}

void adc1_isr(void)
{
	AdcHandler::HandleAdc1Isr();
}
