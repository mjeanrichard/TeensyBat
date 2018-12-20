#ifndef Helpers_h
#define Helpers_h

#include "elapsedMillis.h"
#include "Config.h"

#define TB_ERR_SD_INIT          2
#define TB_ERR_SD_CREATE_FILE   3
#define TB_ERR_SD_NO_FILENAME   4
#define TB_ERR_SD_WRITE_BLOCK   5
#define TB_ERR_SD_FORMAT        6

void FatalError(uint8_t errorCode, const __FlashStringHelper *f);

extern bool SerialEnabled;

#define MESSAGE(m) if (SerialEnabled) Serial.print(F(m));
#define MESSAGEF(m, ...) if (SerialEnabled) Serial.printf(F(m), __VA_ARGS__);

#ifdef TB_DEBUG
#define DEBUG_F(m, ...) Serial.printf(F(m), __VA_ARGS__);
#define DEBUG_LN(m) Serial.println(F(m));
#define DEBUG(m) Serial.print(F(m));
#else
#define DEBUG_F(m, ...) ;
#define DEBUG_LN(m) ;
#define DEBUG(m) ;
#endif

#endif