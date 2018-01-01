#include <Arduino.h>

#include "Config.h"
#include "AdcHandler.h"
#include "BatAnalog.h"
#include "Configurator.h"
#include <EEPROM.h>

#ifdef TB_DISPLAY
#include "Display.h"
#endif

BatAnalog *ba = new BatAnalog();

void setup() {

	pinMode(TB_PIN_LED_GREEN, OUTPUT);
	pinMode(TB_PIN_LED_YELLOW, OUTPUT);
	pinMode(TB_PIN_LED_RED, OUTPUT);

	pinMode(TB_PIN_CARD_PRESENT, INPUT_PULLUP);
	pinMode(TB_PIN_S1, INPUT_PULLUP);
	pinMode(TB_PIN_S2, INPUT_PULLUP);

#ifdef TB_DEBUG
	Serial.begin(57600);
	delay(500);
	Serial.println("Start!");
#else
	Serial.end();
#endif

#ifdef TB_DISPLAY
	InitDisplay();
#endif

	if (digitalReadFast(TB_PIN_S1) == LOW)
	{
		Configurator *c = new Configurator();
		c->Start();
	}

	ba->init();
	ba->start();

#ifdef TB_DEBUG
	Serial.print("Free MEM: ");
	Serial.println(FreeRam());
#endif
}


void loop()
{
	ba->process();
}
