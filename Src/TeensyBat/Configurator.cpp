#include "Configurator.h"
#include <EEPROM.h>
#include "Config.h"
#include <TimeLib.h>

void Configurator::SetTime()
{
	uint32_t time = 0;
	time = Serial.parseInt();
	Teensy3Clock.set(time);
	Serial.println(F("OK: Die neue Zeit wurde gesetzt."));
	ReadConfig();
}

void Configurator::SetNodeId()
{
	long nodeid = 0;
	nodeid = Serial.parseInt();
	if (nodeid > 255 || nodeid < 0)
	{
		Serial.printf(F("ERR: Die Node Id muss zwischen 0 und 255 liegen (%lu).\n"), nodeid);
	}
	else
	{
		Serial.printf(F("OK: Setze NodeId %hhu..."), (uint8_t)nodeid);
		EEPROM.write(0, (uint8_t)nodeid);
		Serial.println(F("Done."));
	}
}

void Configurator::ReadConfig()
{
	uint8_t nodeId = EEPROM.read(0);
	unsigned long t = Teensy3Clock.get();
	tmElements_t time;
	breakTime(t, time);

	Serial.printf(F("CFG:NodeId: %02hhu;Time: %02hhu.%02hhu.%04u %02hhu:%02hhu:%02hhu\n"), nodeId, time.Day, time.Month, 1970 + time.Year, time.Hour, time.Minute, time.Second);
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
	ReadConfig();
	while (true)
	{
		while (!Serial.find("C")) {};

		char command = Serial.read();
		switch (command)
		{
		case 'N':
			SetNodeId();
			break;
		case 'T':
			SetTime();
			break;
		case 'R':
		case 'P':
			ReadConfig();
			break;
		case 'X':
			Serial.println(F("RST: Restarting...\n"));
			delay(500);
			CPU_RESTART;
			break;
		default:
			Serial.printf(F("ERR: Unbekannter Befehl: '%c'.\nP      : Zeigt die aktuelle Konfiguration\nCN<nn> : Setzt die NodeId auf nn\nCX     : Startet den TeensyBat neu.\nCT<tt> : Aktualisiert die Zeit.\n"), command);
			break;
		}
	}
}
