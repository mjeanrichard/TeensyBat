#include "BatAudio.h"
#include "SdFat.h"
#include "RamMonitor.h"

#include "HardFault.h"

elapsedMillis d;

BatAudio * b;

SdFat _sd;
SdFile _file;

uint16_t blocksWritten = 0;
uint32_t bgnBlock = 0;
uint32_t endBlock = 0;

const uint16_t BLOCK_COUNT = 50000;

RamMonitor ram;

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
  
  delay(500);

  ram.initialize();

  Serial.println("Start!");
  Serial.println(ram.free());

  ram.run();
  b = new BatAudio();
  ram.run();
  Serial.println("Init");
  b->init();
  ram.run();

  Serial.print("Heap: ");
  Serial.println(ram.heap_free());
  Serial.print("Stack: ");
  Serial.println(ram.stack_free());
  Serial.println(ram.unallocated());
  Serial.println("delete..:");

  Serial.println(ram.free());

  ram.run();
  delete b;
  ram.run();

  Serial.println(ram.free());


  if (!_sd.begin(TB_PIN_SDCS, SPI_FULL_SPEED)) {
    _sd.initErrorPrint();
    err();
  }
}

void loop() {

  if (bgnBlock == 0 && endBlock == 0){
    _sd.remove("test.dat");
    if (!_file.createContiguous("test.dat", TB_SD_BUFFER_SIZE * BLOCK_COUNT)) {
      Serial.println(F("createContiguous failed"));
      err();
    }
    if (!_file.contiguousRange(&bgnBlock, &endBlock)) {
      Serial.println(F("contiguousRange failed"));
      err();
    }
    Serial.println(F("Ok, new file open."));

    if (_sd.card()->eraseSingleBlockEnable()){
      Serial.println(F("Ok, erasing..."));
      if (!_sd.card()->erase(bgnBlock, endBlock))
      {
        Serial.println(F("Err erasing..."));
        err();
      }
      Serial.println(F("Done."));
    } else {
      Serial.println(F("Ok, erasing manually..."));
      if (!_sd.card()->writeStart(bgnBlock, BLOCK_COUNT)){
        Serial.println(F("Block Start failed!"));
        err();
      }

      uint8_t * pCache = (uint8_t*)_sd.vol()->cacheClear();
      memset(pCache, 0x00, TB_SD_BUFFER_SIZE);
      for (int i=0;i < BLOCK_COUNT;i++){
        _sd.card()->writeData(pCache);
      }

      if (!_sd.card()->writeStop()){
        Serial.println(F("Block Stop failed!"));
        err();
      }
      Serial.println(F("Ok, done."));
    }

  }

   if (b->hasDataAvailable())
   {
     if (!_sd.card()->writeStart(bgnBlock+blocksWritten, BLOCK_COUNT))
     {
       Serial.println("Block Start failed!");
       err();
     }
     while(b->hasDataAvailable())
     {
       uint8_t * pCache = (uint8_t*)_sd.vol()->cacheClear();
       b->writeToCard(&blocksWritten, &_sd, BLOCK_COUNT, pCache);
     }

     if (!_sd.card()->writeStop()){
       Serial.println("Block Stop failed!");
       err();
     }
   }

  //b.debug();
  //b.sendOverUsb();

  //Serial.println(ram.free());
  
}


