#include "BatAnalog.h"

void BatAnalog::process()
{
	BatCall * currentCall = &_callLog[_currentCallIndex];

	noInterrupts();
	int16_t* readyBuffer = AdcHandler::readyBuffer;
	interrupts();

	if (readyBuffer == nullptr)
	{
		CheckLog();
		if (_msSinceLastInfoLog >= TB_INFO_LOG_INTERVAL_MS)
		{
			AddInfoLog();
		}
		return;
	}

	_lastSampleDuration = _usSinceLastSample;
	_usSinceLastSample = 0;

	copy_to_fft_buffer(_complexBuffer, readyBuffer);
	apply_window_to_fft_buffer(_complexBuffer);
	arm_cfft_radix4_q15(&_fft_inst, _complexBuffer);

	uint16_t p = AdcHandler::ReadEnvelope();
	uint32_t * binData = currentCall->data;


	if (p > TB_MIN_CALL_START_POWER || (currentCall->sampleCount > 0 && p > TB_MIN_CALL_POWER))
	{
		if (currentCall->sampleCount == 0)
		{
			_callDuration = 0;
			digitalWriteFast(TB_PIN_LED_GREEN, HIGH);
			currentCall->startTimeMs = millis();
			currentCall->maxPower = p;

			uint32_t tmp = *((uint32_t *)_complexBuffer);
			binData[0] = multiply_16tx16t_add_16bx16b(tmp, tmp);

			int index = 2;
			for (int i = 1; i < TB_QUART_FFT_SIZE; i++)
			{
				tmp = *((uint32_t *)_complexBuffer + index++);
				binData[i] = multiply_16tx16t_add_16bx16b(tmp, tmp) / 2;
				tmp = *((uint32_t *)_complexBuffer + index++);
				binData[i] += multiply_16tx16t_add_16bx16b(tmp, tmp) / 2;
			}
		}
		else
		{
			if (p > currentCall->maxPower) currentCall->maxPower = p;

			uint32_t tmp = *((uint32_t *)_complexBuffer);
			uint32_t magsq = multiply_16tx16t_add_16bx16b(tmp, tmp);
			binData[0] += magsq;

			int index = 2;
			for (int i = 1; i < TB_QUART_FFT_SIZE; i++)
			{
				tmp = *((uint32_t *)_complexBuffer + index++);
				magsq = multiply_16tx16t_add_16bx16b(tmp, tmp) / 2;
				tmp = *((uint32_t *)_complexBuffer + index++);
				magsq += multiply_16tx16t_add_16bx16b(tmp, tmp) / 2;
				binData[i] += magsq;
			}
		}
		currentCall->sampleCount++;
		noInterrupts();
		currentCall->clippedSamples = AdcHandler::ClippedSignalCount;
		currentCall->missedSamples = AdcHandler::MissedSamples;
		interrupts();
		_msSinceLastCall = 0;
	}
	else if (currentCall->sampleCount > 0)
	{
		currentCall->durationMicros = _callDuration;
		noInterrupts();
		AdcHandler::MissedSamples = 0;
		AdcHandler::ClippedSignalCount = 0;
		interrupts();

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
		
		_currentCallIndex++;
		CheckLog();
		_callLog[_currentCallIndex].sampleCount = 0;
		_msSinceLastCall = 0;
	} else
	{
		noInterrupts();
		AdcHandler::MissedSamples = 0;
		AdcHandler::ClippedSignalCount = 0;
		interrupts();
	}
	noInterrupts();
	AdcHandler::readyBuffer = nullptr;
	interrupts();
}

void BatAnalog::AddInfoLog()
{
	BatInfo * batInfo = &_infoLog[_currentInfoIndex];
	batInfo->time = Teensy3Clock.get();
	batInfo->startTimeMs = millis();
	batInfo->BatteryVoltage = (AdcHandler::ReadBatteryVoltage() * 2550) / 1000;
	batInfo->LastBufferDuration = _lastSampleDuration;
#ifdef TB_DEBUG
	Serial.print(F("Adding Info: "));
	Serial.printf("Bat: %u mV, Sample Duration: %u ms, Time: %ul, MS: %ul\n", batInfo->BatteryVoltage, batInfo->LastBufferDuration, batInfo->time, batInfo->startTimeMs);
#endif
	_currentInfoIndex++;
	if (_currentInfoIndex > TB_LOG_BUFFER_LENGTH)
	{
		_currentInfoIndex = 0;
	}
	_msSinceLastInfoLog = 0;
}

void BatAnalog::CheckLog()
{
	if (_currentCallIndex >= TB_LOG_BUFFER_LENGTH || (_currentCallIndex > 0 && _msSinceLastCall >= TB_TIME_BEFORE_AUTO_LOG_MS))
	{
		_log.LogCalls(_callLog, _currentCallIndex, _infoLog, _currentInfoIndex);
		_currentInfoIndex = 0;
		_currentCallIndex = 0;
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
	arm_cfft_radix4_init_q15(&_fft_inst, TB_FFT_SIZE, 0, 1);
}

void BatAnalog::start()
{
	_currentCallIndex = 0;
	_callLog[_currentCallIndex].sampleCount = 0;
	AdcHandler::Start();
}

void BatAnalog::stop()
{
	AdcHandler::Stop();
}

