#include "BatAudio.h"

BatAudio b;


void setup() {
  pinMode(TB_PIN_AUDIO, INPUT);
  pinMode(TB_PIN_LED_GREEN, OUTPUT);
  pinMode(TB_PIN_LED_YELLOW, OUTPUT);
  pinMode(TB_PIN_LED_RED, OUTPUT);
  pinMode(A8, OUTPUT);
  pinMode(A12, OUTPUT);
  pinMode(10, OUTPUT);
  Serial.begin(115200);
  while (!Serial) {
    
  }
  Serial.println("Start!");
  b.init();
 
}

void loop() {

  b.debug();
}


