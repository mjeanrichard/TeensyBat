#include "BatAnalog.h"
#include "Config.h"

#include "dspinst.h"
#include "sqrt_integer.h"


void BatAnalog::process()
{
	BatCall * currentCall = &callLog[currentCallIndex];

	if (AdcHandler::readyBuffer == nullptr)
	{
		/*if (currentCallIndex > 0 && currentCall->sampleCount == 0) {
			LogCall();
		}*/
		return;
	}

	copy_to_fft_buffer(complexBuffer, AdcHandler::readyBuffer);
	apply_window_to_fft_buffer(complexBuffer);
	arm_cfft_radix4_q15(&fft_inst, complexBuffer);

	uint16_t p = AdcHandler::ReadEnvelope();

	uint32_t * binData = currentCall->data;


	if (p > 1000 || (currentCall->sampleCount > 0 && p > 1000))
	{
		if (currentCall->sampleCount == 0)
		{
			callDuration = 0;
			digitalWriteFast(TB_PIN_LED_GREEN, HIGH);
			currentCall->startTimeMs = millis();
			currentCall->maxPower = p;

			uint32_t tmp = *((uint32_t *)complexBuffer);
			binData[0] = multiply_16tx16t_add_16bx16b(tmp, tmp);

			int index = 2;
			for (int i = 1; i < TB_QUART_FFT_SIZE; i++)
			{
				tmp = *((uint32_t *)complexBuffer + index++);
				binData[i] = multiply_16tx16t_add_16bx16b(tmp, tmp) / 2;
				tmp = *((uint32_t *)complexBuffer + index++);
				binData[i] += multiply_16tx16t_add_16bx16b(tmp, tmp) / 2;
			}
		}
		else
		{
			if (p > currentCall->maxPower) currentCall->maxPower = p;

			uint32_t tmp = *((uint32_t *)complexBuffer);
			uint32_t magsq = multiply_16tx16t_add_16bx16b(tmp, tmp);
			binData[0] += magsq;

			int index = 2;
			for (int i = 1; i < TB_QUART_FFT_SIZE; i++)
			{
				tmp = *((uint32_t *)complexBuffer + index++);
				magsq = multiply_16tx16t_add_16bx16b(tmp, tmp) / 2;
				tmp = *((uint32_t *)complexBuffer + index++);
				magsq += multiply_16tx16t_add_16bx16b(tmp, tmp) / 2;
				binData[i] += magsq;
			}
		}
		currentCall->sampleCount++;
		currentCall->clippedSamples = AdcHandler::ClippedSignalCount;
		currentCall->missedSamples = AdcHandler::MissedSamples;
	}
	else if (currentCall->sampleCount > 0)
	{
		currentCall->durationMicros = callDuration;
		AdcHandler::MissedSamples = 0;
		AdcHandler::ClippedSignalCount = 0;

#ifdef TB_DEBUG
		if (currentCall->missedSamples > 0)
		{
			Serial.print(F("Missed Samples: "));
			Serial.println(currentCall->missedSamples);
		}
		if (currentCall->clippedSamples > 0)
		{
			Serial.print(F("Clipped Samples: "));
			Serial.println(currentCall->clippedSamples);
		}
#endif 

		for (int i = 0; i < TB_QUART_FFT_SIZE; i++)
		{
			binData[i] = (uint16_t)sqrt_uint32_approx(binData[i] / currentCall->sampleCount);
		}

		digitalWriteFast(TB_PIN_LED_GREEN, LOW);
		
		currentCallIndex++;
		if (currentCallIndex >= 10)
		{
			LogCalls();
		}
		callLog[currentCallIndex].sampleCount = 0;
	}
	AdcHandler::readyBuffer = nullptr;
}

void BatAnalog::LogCalls()
{
#ifdef TB_DEBUG
	Serial.print(F("Logging Calls: "));
	Serial.println(currentCallIndex);
#endif

	InitLogFile(false);

	if (isFileReady)
	{
		if (!WriteLog())
		{
#ifdef TB_DEBUG
			Serial.println(F("First attempt to write to log failed, trying again."));
#endif
			//Reinitialize SDCard and try again.
			InitLogFile(true);
			if (isFileReady) {
				if (!WriteLog())
				{
					Error(TB_ERROR_LOG_WRITE);
				}
			}
#ifdef TB_DEBUG
			else
			{
				Serial.println(F("SD not available."));
			}
#endif
		}
	}
	callLog[0].sampleCount = 0;
	currentCallIndex = 0;
}

bool BatAnalog::WriteLog()
{
	for (int callIndex = 0; callIndex < currentCallIndex; callIndex++) 
	{
		BatCall *currentCall = &callLog[callIndex];

		int retVal = file.write(255);
		if (retVal <= 0)
		{
#ifdef TB_DEBUG
			Serial.print(F("Failed to Write to File: "));
			Serial.println(file.getError());
#endif
			return false;
		}
		file.write(255);

		file.write(&currentCall->durationMicros, 4);
		file.write(&currentCall->startTimeMs, 4);
		file.write(&currentCall->clippedSamples, 2);
		file.write(&currentCall->maxPower, 2);
		file.write(&currentCall->missedSamples, 2);

		uint16_t * val = ((uint16_t *)currentCall->data);
		for (int i = 0; i < TB_QUART_FFT_SIZE; i++)
		{
			file.write(val, 2);
			val += 2;
		}
	}
	if (!file.sync())
	{
#ifdef TB_DEBUG
		Serial.print(F("Failed to Sync File: "));
		Serial.println(file.getError());
#endif
		return false;
	}
	return true;
}

void BatAnalog::InitLogFile(bool forceReset)
{
	if (digitalReadFast(TB_PIN_CARD_PRESENT) == HIGH)
	{
#ifdef TB_DEBUG
		Serial.println(F("No SD-Card present!"));
#endif
		isFileReady = false;
		return;
	}

	if (isFileReady && !forceReset)
	{
		return;
	}

#ifdef TB_DEBUG
	Serial.println(F("Initializing SD Card..."));
#endif

	if (!sd.begin(TB_PIN_SDCS, SPI_FULL_SPEED)) {
#ifdef TB_DEBUG
		sd.initErrorPrint();
#endif
		Error(TB_ERROR_OPEN_CARD);
	}
	if (file.isOpen())
	{
		file.close();
	}
	file.clearError();
	if (!file.open("out.txt", O_RDWR | O_CREAT | O_AT_END)) {
#ifdef TB_DEBUG
		Serial.println(F("Could not create/open File!"));
#endif
		Error(TB_ERROR_OPEN_FILE);
	}
	isFileReady = true;
}

void BatAnalog::Error(int code)
{
#ifdef TB_DEBUG
	Serial.print(F("Fatal Error: "));
	Serial.println(code);
#endif

	while(true)
	{
		for (int i = 0; i < code; i++)
		{
			digitalWriteFast(TB_PIN_LED_RED, HIGH);
			delay(500);
			digitalWriteFast(TB_PIN_LED_RED, LOW);
			delay(500);
		}
		delay(1000);
	}
}

void BatAnalog::copy_to_fft_buffer(void* destination, const void* source)
{
	const uint16_t* src = (const uint16_t *)source;
	uint32_t* dst = (uint32_t *)destination;

	for (int i = 0; i < TB_FFT_SIZE; i++)
	{
		*dst++ = *src++;
	}
}

void BatAnalog::apply_window_to_fft_buffer(void* buffer)
{
	int16_t* buf = (int16_t *)buffer;
	const int16_t* win = (int16_t *)AudioWindowHanning1024;

	for (int i = 0; i < TB_FFT_SIZE; i++)
	{
		int32_t val = *buf * *win++;
		*buf = val >> 15;
		buf += 2;
	}
}


void BatAnalog::init()
{
	AdcHandler::InitAdc();
	arm_cfft_radix4_init_q15(&fft_inst, TB_FFT_SIZE, 0, 1);
}

void BatAnalog::start()
{
	currentCallIndex = 0;
	callLog[currentCallIndex].sampleCount = 0;
	AdcHandler::Start();
}

void BatAnalog::stop()
{
	AdcHandler::Stop();
}

