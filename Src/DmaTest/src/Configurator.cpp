#include "Configurator.h"

void Configurator::SetTime(uint8_t buffer[USB_BUF_SIZE])
{
	elapsedMillis ms = 0;
	uint32_t time = (buffer[1] << 24) | (buffer[2] << 16) | (buffer[3] << 8) | (buffer[4]);
	uint16_t offset = (buffer[5] << 8) | buffer[6];
	while (ms < offset && ms < 3000)
	{
	}
	Teensy3Clock.set(time);
	if (ms >= 3000)
	{
		Serial.println(F("Error waiting for Offset!"));
	}
	EEPROM.put(TB_EEPROM_TIME, time);
	Serial.print(F("OK: Die neue Zeit wurde gesetzt: "));
	Serial.print(time);
	Serial.print(F(" Offset: "));
	Serial.println(offset);
}

void Configurator::SetVoltage(uint8_t buffer[USB_BUF_SIZE])
{
	elapsedMillis ms = 0;
	uint16_t newVoltage = (buffer[1] << 8) | buffer[2];
	uint16_t rawVoltage = _batAudio->readRawBatteryVoltage();
	uint16_t factor = (newVoltage * 1000) / rawVoltage;
	EEPROM.put(TB_EEPROM_V_FACT, factor);
	_voltageFactor = factor;

	Serial.print(F("OK: Setze Spannungsfaktor: "));
	Serial.println(factor);
}

uint16_t Configurator::ReadVoltage()
{
	uint16_t rawVoltage = _batAudio->readRawBatteryVoltage();
	return (rawVoltage * _voltageFactor) / 1000;
}

void Configurator::SetNodeId(uint8_t buffer[USB_BUF_SIZE])
{
	uint8_t newNodeId = buffer[1];
	EEPROM.write(TB_EEPROM_NODE_ID, newNodeId);
	Serial.printf(F("OK: NodeId %hhu gesetzt."), newNodeId);
}

void Configurator::SendConfig(uint8_t buffer[USB_BUF_SIZE])
{
	uint8_t nodeId = EEPROM.read(TB_EEPROM_NODE_ID);
	buffer[0] = DATA_DEVICE_INFO;
	buffer[1] = nodeId;

	uint32_t time = Teensy3Clock.get();
	uint16_t delta = _timeMs;
	buffer[2] = (uint8_t)(time >> 24);
	buffer[3] = (uint8_t)(time >> 16);
	buffer[4] = (uint8_t)(time >> 8);
	buffer[5] = (uint8_t)(time);

	buffer[6] = (uint8_t)(delta >> 8);
	buffer[7] = (uint8_t)(delta);

	uint16_t voltage = ReadVoltage();
	buffer[8] = (uint8_t)(voltage >> 8);
	buffer[9] = (uint8_t)(voltage);

	int16_t temp = _batAudio->readTempC();
	buffer[10] = (uint8_t)(temp >> 8);
	buffer[11] = (uint8_t)(temp);

	DEBUG_F("Id: %hhu, V: %hu mV, %hi C, %lu\n", nodeId, voltage, temp, time)
}

void Configurator::Start()
{
	uint8_t fadeValues[] = {
		1, 2, 2, 3, 4, 5, 6, 7, 8, 9,
		10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
		20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
		30, 31, 32, 33, 34, 35, 37, 38, 40, 44,
		45, 46, 48, 50, 54, 55, 56, 58, 60, 64,
		65, 66, 68, 70, 74, 75, 76, 78, 80, 84,
		84, 80, 78, 76, 75, 74, 70, 68, 66, 65,
		64, 60, 58, 56, 55, 54, 50, 48, 46, 45,
		44, 40, 38, 37, 35, 34, 33, 32, 31, 30,
		29, 28, 27, 26, 25, 24, 23, 22, 21, 20,
		19, 18, 17, 16, 15, 14, 13, 12, 11, 10,
		9, 8, 7, 6, 5, 4, 3, 2, 2, 1};
	elapsedMillis fadeTimer = 0;

#ifndef TB_DEBUG
	//Start Serial if it was not already enabled.
	Serial.begin(57600);
	delay(500);
#endif
	Serial.println(F("OK: Entering Configuration Mode..."));

	EEPROM.get(TB_EEPROM_V_FACT, _voltageFactor);

	uint8_t outBuffer[64];
	uint8_t inBuffer[64];
	bool exitConfigurator = false;
	bool sendBuffer = false;
	elapsedMillis timeTimer = 0;
	unsigned long lastTime = Teensy3Clock.get();
	_timeMs = 0;

	while (!exitConfigurator)
	{
		uint8_t len = RawHID.recv(inBuffer, 0);
		if (len > 0)
		{
			Serial.print(F("Got Command: "));
			Serial.print(F("  "));
			Serial.print(len);
			Serial.print(F("  "));
			Serial.println(inBuffer[0]);
			memset(outBuffer, 0, sizeof(outBuffer));
			switch (inBuffer[0])
			{
			case CMD_SET_TIME:
				SetTime(inBuffer);
				break;
			case CMD_SET_VOLTAGE:
				SetVoltage(inBuffer);
				break;
			case CMD_SET_NODEID:
				SetNodeId(inBuffer);
				break;
			case CMD_EXIT_CONFIG:
				Serial.println(F("OK: Exiting Configuration Mode..."));
				return;
			}
		}
		if (sendBuffer)
		{
			RawHID.send(outBuffer, 0);
			sendBuffer = false;
		}

		if (lastTime != Teensy3Clock.get())
		{
			_timeMs = 0;
			lastTime = Teensy3Clock.get();
		}

		if (timeTimer > 490)
		{
			memset(outBuffer, 0, sizeof(outBuffer));
			SendConfig(outBuffer);
			RawHID.send(outBuffer, 0);
			timeTimer = 0;
		}

		if (digitalReadFast(TB_PIN_S2) == LOW)
		{
			exitConfigurator = true;
			Serial.print(F("Exiting Config mode..."));
		}

		uint8_t idx = fadeTimer / 10;
		if (idx >= sizeof(fadeValues)){
			idx = 0;
			fadeTimer = 0;
		}
		analogWrite(TB_PIN_LED_YELLOW, fadeValues[idx]);
		analogWrite(TB_PIN_LED_GREEN, fadeValues[idx]);
	}
}
