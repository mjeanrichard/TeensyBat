#include "BatAudio.h"
#include "SdFat.h"
#include "RamMonitor.h"

#include "HardFault.h"
#include "Helpers.h"
#include "LogWriter.h"

#include "CardFormatter.h"

elapsedMillis d;

BatAudio *_b;
LogWriter *_logWriter;

uint16_t blocksWritten = 0;
uint32_t bgnBlock = 0;
uint32_t endBlock = 0;

RamMonitor ram;

void err()
{
  while (true)
  {
  }
}

void checkFormatRequested()
{
  elapsedMillis m = 0;
  while (digitalReadFast(TB_PIN_S1) == LOW &&
         digitalReadFast(TB_PIN_S2) == LOW &&
         m < 1000)
  {
  }
  if (digitalReadFast(TB_PIN_S1) == LOW && digitalReadFast(TB_PIN_S2) == LOW)
  {
    _logWriter->FormatCard();
  }
}

void setup()
{

  pinMode(TB_PIN_AUDIO, INPUT);
  pinMode(TB_PIN_LED_GREEN, OUTPUT);
  pinMode(TB_PIN_LED_YELLOW, OUTPUT);
  pinMode(TB_PIN_LED_RED, OUTPUT);
  pinMode(A8, OUTPUT);
  pinMode(A12, OUTPUT);
  pinMode(TB_PIN_SDCS, OUTPUT);

  pinMode(TB_PIN_S1, INPUT_PULLUP);
  pinMode(TB_PIN_S2, INPUT_PULLUP);
  pinMode(TB_PIN_CARD_PRESENT, INPUT_PULLUP);

  Serial.begin(115200);

  delay(500);

  ram.initialize();

  Serial.println("Start!");
  Serial.println(ram.free());

  _b = new BatAudio();

  _logWriter = new LogWriter(1, _b);
  _logWriter->InitializeCard();

  checkFormatRequested();

  _b->init();
  _b->start();
}

void loop()
{

  _logWriter->Process();

  //b.debug();
  //b.sendOverUsb();

  //Serial.println(ram.free());
}
