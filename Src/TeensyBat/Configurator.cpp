#include "Configurator.h"

void Configurator::SetTime()
{
	uint32_t time = 0;
	time = Serial.parseInt();
	Teensy3Clock.set(time);
	Serial.println(F("Die neue Zeit wurde gesetzt."));
	ReadConfig();
}

void Configurator::SetNodeId()
{
	long nodeid = 0;
	nodeid = Serial.parseInt();
	if (nodeid > 255 || nodeid < 0)
	{
		Serial.printf(F("Die Node Id muss zwischen 0 und 255 liegen (%lu).\n"), nodeid);
	}
	else
	{
		Serial.printf(F("Setze NodeId %hhu..."), (uint8_t)nodeid);
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

	Serial.printf(F("=====\nAktuelle Konfiguration:\nNodeId:  %02hhu\nZeit:    %02hhu.%02hhu.%04u %02hhu:%02hhu:%02hhu\n=====\n"), nodeId, time.Day, time.Month, 1970 + time.Year, time.Hour, time.Minute, time.Second);
}

void Configurator::Start()
{
	digitalWriteFast(TB_PIN_LED_YELLOW, HIGH);
	digitalWriteFast(TB_PIN_LED_GREEN, HIGH);

#ifndef TB_DEBUG
	//Start Serial if it was not already enabled.
	Serial.begin(57600);
	delay(500);
#endif
	Serial.println(F("Entering Configuration Mode..."));
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
			Serial.println(F("Restarting...\n"));
			delay(500);
			CPU_RESTART;
			break;
		default:
			Serial.printf(F("Unbekannter Befehl: '%c'.\nP      : Zeigt die aktuelle Konfiguration\nCN<nn> : Setzt die NodeId auf nn\nCX     : Startet den TeensyBat neu.\nCT<tt> : Aktualisiert die Zeit.\n"), command);
			break;
		}
	}
}
