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

	Serial.begin(57600 /* Rate does not matter on a Teensy... */);
	elapsedMillis timeout;

	bool hasUsbConfigured = !bitRead(USB0_OTGSTAT,5);
	if (hasUsbConfigured && digitalReadFast(TB_PIN_S1) == HIGH){
		digitalWrite(TB_PIN_LED_GREEN, HIGH);
		Configurator *c = new Configurator();
		c->Start();
		delete c;
	}

#ifdef TB_DEBUG
	delay(500);
	Serial.println("Start!");
#else
	Serial.end();
#endif

#ifdef TB_DISPLAY
	InitDisplay();
#endif

	ba->init();
	ba->start();
}


void loop()
{
	ba->process();
}
