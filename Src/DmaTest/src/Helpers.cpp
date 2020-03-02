#include "Helpers.h"

bool SerialEnabled = false;

void EnableSerial()
{
    if (!SerialEnabled)
    {
        Serial.begin(115200);
        delay(500);
    }
    SerialEnabled = true;
}

void FatalError(uint8_t errorCode, const __FlashStringHelper *f)
{
    digitalWriteFast(TB_PIN_LED_RED, HIGH);

    if (SerialEnabled) 
    {
        Serial.println();
        Serial.println();
        Serial.println(F("!!!!!!!!"));
        Serial.printf(F("Fatal Error: %u\n"), errorCode);
        Serial.println(f);
        Serial.println(F("!!!!!!!!"));
    }
    
    elapsedMillis t = 0;
    while(true)
    {
        if (t > 1000){
            for(uint8_t i = 0; i < errorCode; i++)
            {
                digitalWriteFast(TB_PIN_LED_YELLOW, HIGH);
                delay(200);
                digitalWriteFast(TB_PIN_LED_YELLOW, LOW);
                delay(200);
            }
            t = 0;
        }
    }
}

void WriteInt16(uint8_t * buffer, uint16_t data)
{
    buffer[0] = (uint8_t)(data);
    buffer[1] = (uint8_t)(data >> 8);
}

void WriteInt32(uint8_t * buffer, uint32_t data)
{
    buffer[0] = (uint8_t)(data);
    buffer[1] = (uint8_t)(data >> 8);
    buffer[2] = (uint8_t)(data >> 16);
    buffer[3] = (uint8_t)(data >> 24);

}
