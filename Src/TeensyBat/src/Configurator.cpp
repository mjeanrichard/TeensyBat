#include "Configurator.h"

void Configurator::SetTime(byte buffer[USB_BUF_SIZE])
{
	elapsedMillis ms = 0;
	uint32_t time = (buffer[1] << 24) | (buffer[2] << 16) | (buffer[3] << 8) | (buffer[4]);
	uint16_t offset = (buffer[5] << 8) | buffer[6];
	while (ms < offset && ms < 3000){}
	Teensy3Clock.set(time);
	if (ms >= 3000){
		Serial.println(F("Error waiting for Offset!"));
	}
	Serial.print(F("OK: Die neue Zeit wurde gesetzt: "));
	Serial.print(time);
	Serial.print(F(" Offset: "));
	Serial.println(offset);
}

void Configurator::SetVoltage(byte buffer[USB_BUF_SIZE])
{
	elapsedMillis ms = 0;
	uint16_t newVoltage = (buffer[1] << 8) | buffer[2];
	uint16_t rawVoltage = AdcHandler::ReadRawBatteryVoltage();
	uint16_t factor = (newVoltage * 1000) / rawVoltage;
  	EEPROM.put(1, factor);
	_voltageFactor = factor;

	Serial.print(F("OK: Setze Spannungsfaktor: "));
	Serial.println(factor);
}

uint16_t Configurator::ReadVoltage()
{
	uint16_t rawVoltage = AdcHandler::ReadRawBatteryVoltage();
	return (rawVoltage * _voltageFactor) / 1000;
}

void Configurator::SetNodeId(byte buffer[USB_BUF_SIZE])
{
	uint8_t newNodeId = buffer[1];
	EEPROM.write(0, newNodeId);
	Serial.printf(F("OK: NodeId %hhu gesetzt."), newNodeId);
}

void Configurator::SendConfig(byte buffer[USB_BUF_SIZE])
{
	uint8_t nodeId = EEPROM.read(0);
	buffer[0] = DATA_DEVICE_INFO;
	buffer[1] = nodeId;

	uint32_t time = Teensy3Clock.get();
	uint16_t delta = _timeMs;
	buffer[2] = (byte)(time>>24); 
	buffer[3] = (byte)(time>>16); 
	buffer[4] = (byte)(time>>8); 
	buffer[5] = (byte)(time);

	buffer[6] = (byte)(delta>>8);
	buffer[7] = (byte)(delta);

	uint16_t voltage = ReadVoltage();
	buffer[8] = (byte)(voltage>>8);
	buffer[9] = (byte)(voltage);
}

void Configurator::Start()
{
	analogWrite(TB_PIN_LED_YELLOW, 5);
	analogWrite(TB_PIN_LED_GREEN, 5);

#ifndef TB_DEBUG
	//Start Serial if it was not already enabled.
	Serial.begin(57600);
	delay(500);
#endif
	Serial.println(F("OK: Entering Configuration Mode..."));
	
	EEPROM.get(1, _voltageFactor);

	byte outBuffer[64];
	byte inBuffer[64];
	bool exitConfigurator = false;
	bool sendBuffer = false;
	elapsedMillis timeTimer = 0;
	unsigned long lastTime = Teensy3Clock.get();
	_timeMs = 0;

	while(!exitConfigurator){
		uint8_t len = RawHID.recv(inBuffer, 0);
		if (len > 0){
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
			}
		}
		if (sendBuffer){
			RawHID.send(outBuffer, 0);
			sendBuffer = false;
		}

		if (lastTime != Teensy3Clock.get()){
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
	}


}
