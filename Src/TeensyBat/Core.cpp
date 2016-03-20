#include <Arduino.h>

#include "AdcHandler.h"
#include "BatAnalog.h"
#include <SdFatUtil.h>

#define TFT_DC       15
#define TFT_CS       9
#define TFT_RST    255
#define TFT_MOSI     7
#define TFT_SCLK    14
#define TFT_MISO     8
//ILI9341_t3 tft = ILI9341_t3(TFT_CS, TFT_DC, TFT_RST, TFT_MOSI, TFT_SCLK, TFT_MISO);

BatAnalog *ba = new BatAnalog();

void setup() {

	pinMode(TB_PIN_LED_GREEN, OUTPUT);
	pinMode(TB_PIN_LED_YELLOW, OUTPUT);
	pinMode(TB_PIN_LED_RED, OUTPUT);

	pinMode(TB_PIN_CARD_PRESENT, INPUT_PULLUP);
	pinMode(TB_PIN_S1, INPUT_PULLUP);
	pinMode(TB_PIN_S2, INPUT_PULLUP);

	Serial.begin(57600);
	delay(1000);
	Serial.println("Start!");

	ba->init();

	/*tft.begin();
	tft.fillScreen(ILI9341_YELLOW);
	tft.fillScreen(ILI9341_BLACK);
	tft.setTextColor(ILI9341_WHITE, ILI9341_BLACK);
	tft.setTextSize(1);
	*/

	delay(500);

	ba->start();

	Serial.print("Free MEM: ");
	Serial.println(FreeRam());
}


void loop()
{
	ba->process();
}


void printSpectrum() {
	/*
	tft.setTextSize(1);
	tft.fillScreen(ILI9341_BLACK);

	uint16_t m = 0;
	uint16_t mas = 0;
	uint16_t maC = bins[0];
	uint16_t maN = bins[0];

	bool prevUp = true;

	for (int i = 1; i < QUART_FFT_SIZE-1; i++){
	m = bins[i];
	mas = mas + bins[i+1] - (mas >> 2);
	maN = mas >> 2;
	if (maC > 3 && prevUp && maC > maN){
	tft.drawLine(0, i, m, i, ILI9341_GREEN);
	tft.setCursor(210, i);
	tft.print((i*452)/1000);
	} else {
	tft.drawLine(0, i, m, i, ILI9341_WHITE);
	}
	//tft.drawLine(m, i, 200, i, ILI9341_BLACK);
	tft.drawLine(maC+1, i, maC, i, ILI9341_RED);
	prevUp = maC <= maN;
	maC = maN;
	//Serial.print(m);
	//Serial.print(", ");
	}
	tft.setTextSize(3);
	tft.setCursor(10, 260);
	//tft.println(maxPower);
	//tft.println(adp);
	//tft.drawLine(maxPower, 0, maxPower,260, ILI9341_BLUE);
	//tft.print(":");
	//tft.print((uint16_t)sqrt_uint32_approx(maxPower));
	//Serial.println();
	*/
}





