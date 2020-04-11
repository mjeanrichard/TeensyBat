#include "BatAudio.h"
#include "SdFat.h"
#include "RamMonitor.h"

#include "HardFault.h"
#include "Helpers.h"
#include "LogWriter.h"

#include "Configurator.h"

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

void startConfiguratorIfReqested()
{
  bool hasUsbConfigured = !bitRead(USB0_OTGSTAT, 5);
  if (hasUsbConfigured && digitalReadFast(TB_PIN_S1) == HIGH)
  {
    digitalWrite(TB_PIN_LED_GREEN, HIGH);
    Configurator *c = new Configurator(_b);
    c->Start();
    delete c;
    analogWrite(TB_PIN_LED_GREEN, LOW);
    analogWrite(TB_PIN_LED_YELLOW, LOW);
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

  #if TB_DEBUG > 0
  EnableSerial();
  #endif

  #if TB_DEBUG > 0
  // This delay is needed somehow. Otherwise a Hardfault is generated
  delay(500);
  ram.initialize();
  Serial.println(ram.free());
  #endif

  DEBUG_LN("Start!");

  byte nodeId = 0;
  EEPROM.get(TB_EEPROM_NODE_ID, nodeId);

  _b = new BatAudio();
  _b->init();

  _logWriter = new LogWriter(nodeId, _b);
  _logWriter->InitializeCard(false);

  startConfiguratorIfReqested();
  checkFormatRequested();

  #if TB_DEBUG > 0
  Serial.println(ram.free());
  #endif

  _b->start();
}

void loop()
{
  _logWriter->Process();

  //_b->debug();
  //b.sendOverUsb();
}
