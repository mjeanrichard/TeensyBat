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
extern void EnableSerial();

extern void WriteInt16(uint8_t * buffer, uint16_t data);
extern void WriteInt32(uint8_t * buffer, uint32_t data);


#define MESSAGE(m) if (SerialEnabled) Serial.print(F(m));
#define MESSAGEF(m, ...) if (SerialEnabled) Serial.printf(F(m), __VA_ARGS__);

#if TB_DEBUG > 0
#define DEBUG_F(m, ...) Serial.printf(F(m), __VA_ARGS__);
#define DEBUG_LN(m) Serial.println(F(m));
#define DEBUG(m) Serial.print(F(m));
#else
#define DEBUG_F(m, ...) ;
#define DEBUG_LN(m) ;
#define DEBUG(m) ;
#endif

#ifdef TB_SD_LED
#define SD_ACTIVE_ON() digitalWriteFast(TB_SD_LED, HIGH);
#define SD_ACTIVE_OFF() digitalWriteFast(TB_SD_LED, LOW);
#else
#define SD_ACTIVE_ON() ;
#define SD_ACTIVE_OFF()  ;
#endif

#ifdef TB_CALL_LED
#define CALL_INPROGRESS() digitalWriteFast(TB_CALL_LED, HIGH);
#define CALL_NOT_INPROGRESS() digitalWriteFast(TB_CALL_LED, LOW);
#else
#define CALL_INPROGRESS() ;
#define CALL_NOT_INPROGRESS()  ;
#endif

#ifdef TB_CALL_BUFFER_FULL_LED
#define CALL_BUFFER_FULL() digitalWriteFast(TB_CALL_BUFFER_FULL_LED, HIGH);
#define CALL_BUFFER_NOT_FULL() digitalWriteFast(TB_CALL_BUFFER_FULL_LED, LOW);
#else
#define CALL_BUFFER_FULL() ;
#define CALL_BUFFER_NOT_FULL()  ;
#endif



#endif