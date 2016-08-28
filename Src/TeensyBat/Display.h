#ifndef _DISPLAY_h
#define _DISPLAY_h

#include "Config.h"
#include "ILI9341_t3.h"

#define TFT_DC       15
#define TFT_CS       9
#define TFT_RST    255
#define TFT_MOSI     7
#define TFT_SCLK    14
#define TFT_MISO     8

void InitDisplay();
void PrintPowerData(uint8_t * powerData, uint16_t length);


#endif

