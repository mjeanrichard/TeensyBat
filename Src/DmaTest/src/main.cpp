#include "BatAudio.h"
#include "SdFat.h"

BatAudio b;

SdFat _sd;
SdFile _file;

uint8_t dummyBlock[512];


void err(){
  while(true){}
}

void setup() {
  pinMode(TB_PIN_AUDIO, INPUT);
  pinMode(TB_PIN_LED_GREEN, OUTPUT);
  pinMode(TB_PIN_LED_YELLOW, OUTPUT);
  pinMode(TB_PIN_LED_RED, OUTPUT);
  pinMode(A8, OUTPUT);
  pinMode(A12, OUTPUT);
  pinMode(10, OUTPUT);
  Serial.begin(115200);
  // while (!Serial) {
    
  // }
  Serial.println("Start!");
  b.init();

  if (!_sd.begin(TB_PIN_SDCS, SPI_FULL_SPEED)) {
    _sd.initErrorPrint();
    err();
  }

  memset(dummyBlock, 0x00, sizeof(dummyBlock));
}

uint16_t blocksWritten = 0;
uint32_t bgnBlock = 0;
uint32_t endBlock = 0;

const uint16_t BLOCK_COUNT = 50000;

void loop() {

  if (bgnBlock == 0 && endBlock == 0){
    _sd.remove("test.dat");
    if (!_file.createContiguous("test.dat", 512UL * BLOCK_COUNT)) {
      Serial.println("createContiguous failed");
      err();
    }
    if (!_file.contiguousRange(&bgnBlock, &endBlock)) {
      Serial.println("contiguousRange failed");
      err();
    }
    Serial.println("Ok, new file open.");

    if (_sd.card()->eraseSingleBlockEnable()){
      Serial.println("Ok, erasing...");
      if (!_sd.card()->erase(bgnBlock, endBlock))
      {
        Serial.println("Err erasing...");
        err();
      }
      Serial.println("Done.");
    } else {
      Serial.println("Ok, erasing manually...");
      if (!_sd.card()->writeStart(bgnBlock, BLOCK_COUNT)){
        Serial.println("Block Start failed!");
        err();
      }

      for (int i=0;i < BLOCK_COUNT;i++){
        _sd.card()->writeData(dummyBlock);
      }

      if (!_sd.card()->writeStop()){
        Serial.println("Block Stop failed!");
        err();
      }
      Serial.println("Ok, done.");
    }

  }

  if (b.hasDataAvailable())
  {
    if (!_sd.card()->writeStart(bgnBlock+blocksWritten, BLOCK_COUNT)){
      Serial.println("Block Start failed!");
      err();
    }

    while(b.hasDataAvailable())
    {
      b.writeToCard(&blocksWritten, &_sd, BLOCK_COUNT);
    }

    if (!_sd.card()->writeStop()){
      Serial.println("Block Stop failed!");
      err();
    }
  }

  //b.debug();
  //b.sendOverUsb();
}


