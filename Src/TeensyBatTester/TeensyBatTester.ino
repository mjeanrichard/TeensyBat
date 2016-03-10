#include "ILI9341_t3.h"
#include "SPI.h"
#include <ADC.h>

const int LED3 = 4;
const int LED2 = 5;
const int LED1 = 6;

const int S1 = 2;
const int S2 = 3;

const int VBAT = 16;

#define TFT_DC       15
#define TFT_CS       9
#define TFT_RST    255
#define TFT_MOSI     7
#define TFT_SCLK    14
#define TFT_MISO     8
ILI9341_t3 tft = ILI9341_t3(TFT_CS, TFT_DC, TFT_RST, TFT_MOSI, TFT_SCLK, TFT_MISO);

ADC *adc = new ADC();


void setup() {

  Serial.begin(57600);

  pinMode(VBAT, INPUT);

  adc->setReference(ADC_REF_3V3, ADC_1);
  adc->setSamplingSpeed(ADC_MED_SPEED, ADC_1);
  adc->setConversionSpeed(ADC_MED_SPEED, ADC_1);
  adc->setAveraging(16, ADC_1);
  adc->setResolution(12, ADC_1);

  pinMode(LED1, OUTPUT);
  pinMode(LED2, OUTPUT);
  pinMode(LED3, OUTPUT);

  pinMode(S1, INPUT_PULLUP);
  pinMode(S2, INPUT_PULLUP);


  tft.begin();
  tft.fillScreen(ILI9341_WHITE);
  tft.fillScreen(ILI9341_BLACK);
  tft.setTextColor(ILI9341_WHITE, ILI9341_BLACK);
  tft.setRotation(3);
  tft.setTextSize(5);
  tft.println("TeensyBat");
}

void loop() {

  tft.begin();
  tft.fillScreen(ILI9341_WHITE);
  tft.fillScreen(ILI9341_BLACK);
  tft.setTextColor(ILI9341_WHITE, ILI9341_BLACK);
  tft.setRotation(3);
  tft.setTextSize(5);
  tft.println("TeensyBat");

  TestLeds();
  CheckVoltage();

}

void CheckVoltage(){
  tft.fillScreen(ILI9341_BLACK);
  tft.setTextSize(1);
  
  tft.setCursor(0,0);
  tft.println("S2: -  S1: +  S2 Long: Exit");

  tft.setTextSize(4);

  //int fact = 950;
  int fact = 2550;
  while(true){
    int raw = adc->analogRead(VBAT, ADC_1);
    int vin = (raw * fact)/1000;

    if (vin < 1900){
      digitalWrite(LED2, HIGH);
    } else {
      digitalWrite(LED2, LOW);
    }

    tft.setTextSize(4);
    tft.setCursor(10,50);
    tft.print(vin);
    tft.println(" mV");

    tft.setCursor(10,100);
    tft.println(raw);

    tft.setTextSize(2);
    tft.setCursor(50,200);
    tft.println(fact);
    

    Serial.print(vin);
    Serial.print(" mV (");
    Serial.print(raw);
    Serial.print(") Fact: ");
    Serial.println(fact);
    
    unsigned int t1 = millis();
    while(millis()-t1 < 200)
    {
      if (S1Pressed()>10){
        fact += 10;
        break;
      }
      int s1 = S2Pressed();
      if (s1 > 1000){
        return;
      }
       else if (s1>10){
        fact -= 10;
        break;
      }
    }
  }
}

int S1Pressed(){
  unsigned long t1 = millis();
  while(digitalRead(S1)==LOW)
  {}
  return millis()-t1;
}

int S2Pressed(){
  unsigned long t1 = millis();
  while(digitalRead(S2)==LOW)
  {}
  return millis()-t1;
}



void TestLeds(){
  while(digitalRead(S1)==HIGH){
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
  }
}

