
#ifndef CONFIGURATOR_h
#define CONFIGURATOR_h

#include "WProgram.h"
#include "AdcHandler.h"
#include <EEPROM.h>
#include "Config.h"
#include <TimeLib.h>


#define USB_BUF_SIZE 64

#define CMD_SET_NODEID 1
#define CMD_SET_TIME 2
#define CMD_SET_VOLTAGE 3

#define DATA_DEVICE_INFO 1


class Configurator
{
private:
	elapsedMillis _timeMs;
	uint16_t _voltageFactor;
	
	void SetNodeId(byte buffer[USB_BUF_SIZE]);
	void SetTime(byte buffer[USB_BUF_SIZE]);
	void SetVoltage(byte buffer[USB_BUF_SIZE]);
	void SendConfig(byte buffer[USB_BUF_SIZE]);

	void SaveVoltageFactor();

	uint16_t ReadVoltage();

public:
	void Start();
};
#endif