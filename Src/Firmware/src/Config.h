#ifndef CONFIG_h
#define CONFIG_h

#include "WProgram.h"

#define TB_HW_VERSION 5
#define TB_FW_VERSION 3

#define TB_DEBUG 1
#define TB_CALL_LED TB_PIN_LED_GREEN
#define TB_SD_LED TB_PIN_LED_YELLOW
#define TB_CALL_BUFFER_FULL_LED TB_PIN_LED_RED

// Seconds after startup for wich the LEDs are enabled.
static const uint32_t TB_AUTO_SWITCH_OFF_MS = 5 * 60 * 1000;

// Audio Sampling Configuration
#define TB_AUDIO_SAMPLE_BUFFER_SIZE 128

static const uint16_t TB_FFT_RESULT_SIZE = TB_AUDIO_SAMPLE_BUFFER_SIZE;
static const uint16_t TB_CALL_DATA_SIZE = TB_FFT_RESULT_SIZE + 4;

static const uint8_t TB_PRE_CALL_BUFFER_COUNT = 4;
static const uint8_t TB_AFTER_CALL_SAMPLES = 4;

static const uint16_t TB_CALL_START_THRESHOLD = 2400; // Max Value is 4095
static const uint16_t TB_CALL_STOP_THRESHOLD = 1000;

static const uint8_t TB_CALL_MIN_FREQ_BIN = 21; // Min FFT Bin to include in measurement
static const uint8_t TB_CALL_MIN_POWER = 50; // Min Power in a bin above TB_CALL_MIN_FREQ_BIN for a Call to be logged
static const uint16_t TB_CALL_POWERCOUNTER_THRESHOLD = 2400;

static const uint16_t TB_CALL_BUFFER_COUNT = 340;
static const uint16_t TB_CALL_POINTER_COUNT = 50;

static const uint32_t TB_MS_BETWEEN_BATTERY_READS = 2 * 60 * 1000;
static const uint32_t TB_MS_BETWEEN_TEMP_READS = 2 * 60 * 1000;

static const uint8_t TB_ADDITIONAL_DATA_COUNT = 128;

// PIN Assignment
static const uint8_t TB_PIN_AUDIO = A9;
static const uint8_t TB_PIN_ENVELOPE = A3;
static const uint8_t TB_PIN_BATTERY = A2;

static const uint8_t TB_PIN_LED_GREEN = 6;
static const uint8_t TB_PIN_LED_YELLOW = 5;
static const uint8_t TB_PIN_LED_RED = 4;

static const uint8_t TB_PIN_S1 = 2;
static const uint8_t TB_PIN_S2 = 3;

// SD Card Access
static const uint8_t TB_PIN_SDCS = SS;
static const uint8_t TB_PIN_CARD_PRESENT = 20;

// EEPROM
static const uint8_t TB_EEPROM_NODE_ID = 0; // Node Id
static const uint8_t TB_EEPROM_V_FACT = 1;  // Voltage Calibration Factor
static const uint8_t TB_EEPROM_TIME = 2;    // Timestamp of last set time (can be used to calibrate the clock). Uses 4 bytes!
// Adress 3, 4, 5 are also used by TB_EEPROM_TIME!

// SD Card Write Buffer Size
#define TB_SD_BUFFER_SIZE 512UL
#define TB_FILE_BLOCK_COUNT 5000



#define CPU_RESTART_ADDR (uint32_t *)0xE000ED0C
#define CPU_RESTART_VAL 0x5FA0004
#define CPU_RESTART (*CPU_RESTART_ADDR = CPU_RESTART_VAL);

#endif