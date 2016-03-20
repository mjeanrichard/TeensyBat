#include "AdcHandler.h"
#include "Config.h"


int16_t AdcHandler::samples1[TB_FFT_SIZE] __attribute__((aligned(4)));
int16_t AdcHandler::samples2[TB_FFT_SIZE] __attribute__((aligned(4)));

int16_t* AdcHandler::sampleBuffer = samples1;

unsigned int volatile AdcHandler::bufIndex = 0;
int16_t* AdcHandler::readyBuffer = nullptr;
volatile unsigned int AdcHandler::MissedSamples = 0;
volatile unsigned int AdcHandler::ClippedSignalCount = 0;

ADC* AdcHandler::adc = new ADC();

void AdcHandler::Start()
{
	adc->startContinuous(TB_PIN_AUDIO, ADC_0);
}

void AdcHandler::Stop()
{
	adc->stopContinuous(ADC_0);
}

void AdcHandler::InitAdc()
{
	pinMode(TB_PIN_AUDIO, INPUT);
	pinMode(TB_PIN_ENVELOPE, INPUT);

	// ADC_0 is used for sampling the Audio
	adc->setAveraging(1, ADC_0);
	adc->setResolution(12, ADC_0);
	adc->setConversionSpeed(ADC_MED_SPEED, ADC_0);
	adc->setSamplingSpeed(ADC_MED_SPEED, ADC_0);
	adc->enableInterrupts(ADC_0);

	// ADC_1 is used for everthing else.
	adc->setAveraging(10, ADC_1);
	adc->setResolution(12, ADC_1);
	adc->setConversionSpeed(ADC_MED_SPEED, ADC_1);
	adc->setSamplingSpeed(ADC_MED_SPEED, ADC_1);
}

uint16_t AdcHandler::ReadEnvelope()
{
	return adc->analogRead(TB_PIN_ENVELOPE, ADC_1);
}

uint16_t AdcHandler::ReadBatteryVoltage()
{
	return adc->analogRead(TB_PIN_BATTERY, ADC_1);
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
		if (sampleBuffer == samples1)
		{
			sampleBuffer = samples2;
		}
		else
		{
			sampleBuffer = samples1;
		}
		bufIndex = 0;
	}
	sampleBuffer[bufIndex] = s;
	bufIndex++;
	if (s >= 4094 || s <= 2)
	{
		ClippedSignalCount++;
	}
}

void adc0_isr(void)
{
	AdcHandler::HandleAdc0Isr();
}
