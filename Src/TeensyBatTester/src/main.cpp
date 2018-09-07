#include <Arduino.h>

#include "ILI9341_t3.h"
#include "SPI.h"
#include <ADC.h>
#include "SdFat.h"
#include <EEPROM.h>

const int LED3 = 4;
const int LED2 = 5;
const int LED1 = 6;

const int S1 = 2;
const int S2 = 3;

const uint8_t SD_CARD = 20;
const uint8_t SD_CS = 10;

const int VBAT = 16;

SdFat _sd;

#define TFT_DC       15
#define TFT_CS       9
#define TFT_RST    255
#define TFT_MOSI     7
#define TFT_SCLK    14
#define TFT_MISO     8
ILI9341_t3 tft = ILI9341_t3(TFT_CS, TFT_DC, TFT_RST, TFT_MOSI, TFT_SCLK, TFT_MISO);

ADC *adc = new ADC();


void setup() {

  Serial.begin(9600);

  pinMode(VBAT, INPUT);

  adc->setReference(ADC_REFERENCE::REF_3V3, ADC_1);
  adc->setSamplingSpeed(ADC_SAMPLING_SPEED::MED_SPEED, ADC_1);
  adc->setConversionSpeed(ADC_CONVERSION_SPEED::MED_SPEED, ADC_1);
  adc->setAveraging(16, ADC_1);
  adc->setResolution(12, ADC_1);

  pinMode(LED1, OUTPUT);
  pinMode(LED2, OUTPUT);
  pinMode(LED3, OUTPUT);

  pinMode(S1, INPUT_PULLUP);
  pinMode(S2, INPUT_PULLUP);
  pinMode(SD_CARD, INPUT_PULLUP);
  pinMode(10, OUTPUT);

  digitalWrite(LED1, HIGH);
  digitalWrite(LED2, HIGH);
  digitalWrite(LED3, HIGH);
  delay(500);
  digitalWrite(LED1, LOW);
  digitalWrite(LED2, LOW);
  digitalWrite(LED3, LOW);
}

bool retry(){
  Serial.println("Test failed. Press S1 to retry, S2 to continue...");
  int s1 = digitalRead(S1);
  while(s1==HIGH && digitalRead(S2)==HIGH){
      s1 = digitalRead(S1);
  }
  delay(500);
  while(digitalRead(S2)==LOW || digitalRead(S2)==LOW){}
  delay(500);
  return s1 == LOW;
}

void FailStop(){
  Serial.println("FATAL: Halting.");
  while(true){}
}

void TestButtons(){
  Serial.print("Checking initial Button state...");
  if (digitalRead(S1)==LOW){
    Serial.println("Fail: S1 is low!");
    FailStop();
  }
  if (digitalRead(S2)==LOW){
    Serial.println("Fail: S2 is low!");
    FailStop();
  }
  Serial.println("OK.");

  Serial.print("Please press S1...");
  while(digitalRead(S1)==HIGH){}
  Serial.println("OK.");
  delay(500);
  while(digitalRead(S1)==LOW){}

  Serial.print("Please press S2...");
  while(digitalRead(S2)==HIGH){}
  Serial.println("OK.");
  delay(500);
  while(digitalRead(S2)==LOW){}
}

bool TestSD(){
  Serial.print("Checking SD Detection...");
  Serial.println(digitalRead(SD_CARD));
  while(digitalRead(SD_CARD)==LOW){
    Serial.println("SD-Card Detection is LOW! Please remove SD-Card.");
    delay(1000);
  }
  Serial.println("OK.");
  
  Serial.print("Please insert SD-Card...");
  while(digitalRead(SD_CARD)==HIGH){}
  Serial.println("OK.");
  delay(1000);

  Serial.print("Writing file...");
  digitalWrite(10, HIGH);
  if (!_sd.begin(SD_CS, SPI_FULL_SPEED)) {
    Serial.print("Fail: Could not open SDCard:");
    _sd.initErrorPrint();
    return false;
  }

  File file;
  if (!file.open("test.txt", O_RDWR | O_CREAT | O_TRUNC)) {
    Serial.print("Fail: Could not open File.");
    return false;
  }
  file.write("Test123...");
  file.close();
  Serial.println("OK.");

  Serial.print("Reading File...");
  if (!file.open("test.txt", O_RDWR)) {
    Serial.print("Fail: Could not open File.");
    return false;
  }
  int i=0;
  while (file.available()) {
    file.read();
    i++;
  }
  file.close();
  Serial.print(i);  
  Serial.println(" OK.");  
  return true;
}

int S1Pressed(){
  unsigned long t1 = millis();
  while(digitalRead(S1)==LOW)
  {
    delay(50);  
  }
  return millis()-t1;
}

int S2Pressed(){
  unsigned long t1 = millis();
  while(digitalRead(S2)==LOW)
  {
    delay(50);  
  }
  return millis()-t1;
}

void CheckVoltage(){
  uint16_t fact = 1500;
  EEPROM.get(1, fact);
  if (fact < 500 || fact > 2000){
    fact = 1500;
  }
  
  unsigned int t1 = 0;
  while(true){
    int raw = adc->analogRead(VBAT, ADC_1);
    int vin = (raw * fact)/1000;

    if (millis()-t1 > 1000)
    {
      Serial.print(vin);
      Serial.print(" mV (");
      Serial.print(raw);
      Serial.print(") Fact: ");
      Serial.println(fact);
      t1 = millis();
    }    
    if (S1Pressed()>10){
      fact += 10;
      t1 = 0;
    }
    int s1 = S2Pressed();
    if (s1 > 1000){
      break;
    }
    else if (s1>10){
      fact -= 10;
      t1 = 0;
    }
  }
  Serial.print("Writing to EEPROM ...");
  EEPROM.put(1, fact);
  Serial.println("Done.");
}

void TestLeds(){
  Serial.println("Press S1 to start LED testing...");
  while(digitalRead(S1)==HIGH){}

  Serial.print("Testing LEDs...");
  digitalWrite(LED1, HIGH);
  delay(500);
  digitalWrite(LED2, HIGH);
  delay(500);
  digitalWrite(LED3, HIGH);
  delay(500);
  digitalWrite(LED1, LOW);
  delay(500);
  digitalWrite(LED2, LOW);
  delay(500);
  digitalWrite(LED3, LOW);
  delay(500);
  Serial.println("OK.");
}

void TestLCD(){
  Serial.println("Please press S1 to test LCD, S2 to skip.");
  
  int s1 = HIGH;
  int s2 = HIGH;
  while(s1==HIGH && s2==HIGH){
    s1 = digitalRead(S1);
    s2 = digitalRead(S2);
  }
  delay(500);
  if (s2 == LOW){
    Serial.println("Skipping LCD Test.");
    return;
  }

  Serial.print("Testing LCD...");
  tft.begin();
  tft.fillScreen(ILI9341_WHITE);
  tft.fillScreen(ILI9341_BLACK);
  tft.setTextColor(ILI9341_WHITE, ILI9341_BLACK);
  tft.setRotation(3);
  tft.setTextSize(5);
  tft.println("TeensyBat");
  Serial.println("OK.");
}

void loop() {
  while(!Serial) {
  } 

  bool result;

  TestButtons();
  TestLeds();
  
  result = TestSD();
  while (!result && retry()){
    result = TestSD();
  }
  
  //TestLCD();
  CheckVoltage();

  Serial.println();
  Serial.println("----------------------------");
  Serial.println();
  Serial.println("Done. Press S2 to restart...");
  while(digitalRead(S2)==HIGH){}
  delay(500);
  while(digitalRead(S2)==LOW){}
  delay(500);
}
