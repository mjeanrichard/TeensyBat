#ifndef CONFIGURATOR_h
#define CONFIGURATOR_h

#include "Config.h"
#include "Helpers.h"
#include <TimeLib.h>
#include <EEPROM.h>
#include "BatAudio.h"

#define USB_BUF_SIZE 64

#define CMD_SET_NODEID 1
#define CMD_SET_TIME 2
#define CMD_SET_VOLTAGE 3
#define CMD_EXIT_CONFIG 250

#define DATA_DEVICE_INFO 1
#define DATA_CALL 2

#define DATA_CALL_HEADER 1
#define DATA_CALL_DATA1 2
#define DATA_CALL_DATA2 3


class Configurator
{
private:
	BatAudio * _batAudio;

	elapsedMillis _timeMs;
	uint16_t _voltageFactor;
	
	void SetNodeId(uint8_t buffer[USB_BUF_SIZE]);
	void SetTime(uint8_t buffer[USB_BUF_SIZE]);
	void SetVoltage(uint8_t buffer[USB_BUF_SIZE]);
	void SendConfig(uint8_t buffer[USB_BUF_SIZE]);

	void SaveVoltageFactor();

	uint16_t ReadVoltage();

public:
	Configurator(BatAudio * batAudio)
	{
		_batAudio = batAudio;
	}

	void Start();
};
#endif