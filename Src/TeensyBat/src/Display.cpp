#include "Display.h"

#include "Config.h"

ILI9341_t3 tft = ILI9341_t3(TFT_CS, TFT_DC, TFT_RST, TFT_MOSI, TFT_SCLK, TFT_MISO);

// Display Size: 320 x 240

void InitDisplay()
{
	tft.begin();
	tft.fillScreen(ILI9341_WHITE);
	tft.fillScreen(ILI9341_BLACK);
	tft.setTextColor(ILI9341_WHITE, ILI9341_BLACK);
	tft.setRotation(3);
	tft.setTextSize(5);
	tft.println("TeensyBat");
}

void PrintPowerData(uint8_t * powerData, uint16_t length)
{
	tft.setTextSize(1);
	tft.fillScreen(ILI9341_BLACK);

	uint8_t sampleCount = (length / 320) + 1;

	uint32_t sum = 0;
	uint32_t sum2 = 0;
	uint16_t sumCount = 0;

	uint16_t avgSum = 0;
	uint8_t lastPoint = 239;
	for (uint16_t i = 0; i < length; i++) {
		uint8_t pwr = powerData[i];
		avgSum += pwr;
		sum += pwr;
		if (pwr >= TB_MIN_CALL_POWER)
		{
			sum2 += pwr;
			sumCount++;
		}
		if (i % sampleCount == 0)
		{
			uint8_t x = i / sampleCount;
			uint8_t p = 239 - ((avgSum * 0.9) / sampleCount);
			tft.drawLine(x, lastPoint, x + 1, p, ILI9341_WHITE);
			lastPoint = p;
			avgSum = 0;
		}
	}

	uint8_t minAvg = 239 - (TB_MIN_AVG_POWER * 0.9);
	tft.drawLine(0, minAvg, 310, minAvg, ILI9341_GREEN);

	uint8_t avgPower = 239 - ((sum * 0.9) / length);
	tft.drawLine(0, avgPower, 300, avgPower, ILI9341_RED);
	
	avgPower = 239 - ((sum2 * 0.9) / sumCount);
	tft.drawLine(0, avgPower, 300, avgPower, ILI9341_ORANGE);

	if (avgPower <= minAvg)
	{
		tft.fillCircle(300, 10, 8, ILI9341_GREEN);
	}
}

void PrintSpectrum(uint32_t data[TB_HALF_FFT_SIZE]) {
	
	tft.setTextSize(1);
	tft.fillScreen(ILI9341_BLACK);

	uint16_t m = 0;
	uint16_t mas = 0;
	uint16_t maC = data[0];
	uint16_t maN = data[0];

	bool prevUp = true;

	for (int i = 1; i < TB_HALF_FFT_SIZE-1; i++){
	    m = data[i];
	    mas = mas + data[i+1] - (mas >> 2);
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
	
}

