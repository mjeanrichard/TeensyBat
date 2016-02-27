#include "ILI9341_t3.h"
#include "SPI.h"

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


void setup() {

  Serial.begin(57600);

  analogReference(DEFAULT);

  pinMode(LED1, OUTPUT);
  pinMode(LED2, OUTPUT);
  pinMode(LED3, OUTPUT);

  pinMode(S1, INPUT_PULLUP);
  pinMode(S2, INPUT_PULLUP);

  pinMode(VBAT, INPUT);

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
  
  CheckVoltage();

}

void CheckVoltage(){
  tft.fillScreen(ILI9341_BLACK);
  tft.setTextSize(1);
  
  tft.setCursor(0,0);
  tft.println("S2: -  S1: +  S2 Long: Exit");

  tft.setTextSize(4);

  int fact = 3290;
  while(true){
    unsigned int vin = analogRead(VBAT) * fact * 2;
    vin = (vin / 1023);
    
    tft.setTextSize(4);
    tft.setCursor(10,50);
    tft.print(vin);
    tft.println(" mV");

    tft.setTextSize(2);
    tft.setCursor(50,100);
    tft.println(fact);
    
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

