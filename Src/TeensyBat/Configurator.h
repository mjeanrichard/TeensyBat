
#ifndef CONFIGURATOR_h
#define CONFIGURATOR_h

#include "Config.h"
#include <EEPROM.h>
#include <Time.h>


class Configurator
{
private:
	void SetNodeId();
	void SetTime();
	void ReadConfig();
public:
	void Start();
};
#endif