#include <Arduino.h>
#include "LcdDisplay.h"

#include <Arduino.h>
#include <LiquidCrystal.h>
#include <Encoder.h>
#include <elapsedMillis.h>
#include <math.h>


#define BTN_NO_PRESS    0
#define BTN_LONG_PRESS  1
#define BTN_SHORT_PRESS 2

#define PIN_SPEAKER   11
#define PIN_LED        9
#define PIN_ROT_1      2
#define PIN_ROT_2      8
#define PIN_BTN        7

Encoder _enc(2, 8);

LcdDisplay * _lcd;

elapsedMillis _waitTime = 0;
elapsedMillis _debounceTime = 0;

byte _lastBtnState = 0;
byte _preScaler = 8;
byte _timerRegister = 0;

int _freqValue = 50;
int _durationValue = 10;

MenuItem * root;
MenuItem * mnuFrq;
MenuItem * mnuDuration;
MenuItem * mnuMode;

#define SIM_MODE_PULSE     0
#define SIM_MODE_MANUAL    1
#define SIM_MODE_CONTINOUS 2

const char * modes = "PMC";
byte _currentMode = SIM_MODE_PULSE;


void pwmOn()
{
    PORTB |= _BV(PB1);
    TCCR2B = _preScaler;
}

void pwmOff()
{
    TCCR2B = 0;

    // Set to Clear on Compare Match
    TCCR2A = _BV(COM2A1) | _BV(WGM21);
    // Force Compare
    TCCR2B |= _BV(FOC2A);
    // Set back to Toggle on Compare Match
    TCCR2A = _BV(COM2A0) | _BV(WGM21);

    PORTB &= ~_BV(PB1);
}

void pulse()
{
    pwmOn();
    delay(_durationValue);
    pwmOff();
}


void updateFreq(int delta)
{
    int val = min(max(_freqValue + delta, 1), 90);
    sprintf(mnuFrq->value, "%2ikHz", val);
    _freqValue = val;
}

void updateDuration(int delta)
{
    int val = min(max(_durationValue + delta, 1), 900);
    sprintf(mnuDuration->value, "%2ims", val);
    _durationValue = val;
}

void updateMode(int delta)
{
    byte val = ((_currentMode + delta % 3) + 3) % 3;
    mnuMode->value[0] = modes[val];
    _currentMode = val;
}

void exitOnEnter()
{
    _lcd->exit();
}

void setup() {
    Serial.begin(115200);

    pinMode(PIN_SPEAKER, OUTPUT);
    pinMode(PIN_LED, OUTPUT);
    pinMode(PIN_BTN, INPUT_PULLUP);

    TCCR2A = _BV(COM2A0) | _BV(WGM21);
    TCCR2B = 0;
    OCR2A = 128;

    _lastBtnState = digitalRead(PIN_BTN);

    root = new MenuItem();
    root->createSubMenus(1);

    MenuItem * simulateMenu = root->subMenus[0];
    simulateMenu->name = "Simulate";
    simulateMenu->mode = MENU_ITEM_MODE_HORIZONTAL;

    simulateMenu->createSubMenus(3);

    mnuFrq = simulateMenu->subMenus[0];
    mnuFrq->name = "Freq";
    mnuFrq->value = new char[10];
    mnuFrq->updateFunc = &updateFreq;
    mnuFrq->enterFunc = &exitOnEnter;
    mnuFrq->mode = MENU_ITEM_MODE_VALUE;

    mnuDuration = simulateMenu->subMenus[1];
    mnuDuration->name = "Time ";
    mnuDuration->value = new char[10];
    mnuDuration->updateFunc = &updateDuration;
    mnuDuration->enterFunc = &exitOnEnter;
    mnuDuration->mode = MENU_ITEM_MODE_VALUE;

    mnuMode = simulateMenu->subMenus[2];
    mnuMode->name = "M";
    mnuMode->value = new char[2];
    mnuMode->updateFunc = &updateMode;
    mnuMode->enterFunc = &pulse;
    mnuMode->mode = MENU_ITEM_MODE_VALUE;
    mnuMode->value[0] = 'P';
    mnuMode->value[1] = 0;

    updateFreq(0);
    updateDuration(0);

    _lcd = new LcdDisplay(root);
    _lcd->init();
}


float setFrequency(int setPointKhz) {
    int preScalerValue = 8;
    if (setPointKhz >= 32)
    {
        preScalerValue = 1;
        _preScaler = _BV(CS20);
    }
    else if (setPointKhz >= 4)
    {
        preScalerValue = 8;
        _preScaler = _BV(CS21);
    }
    else
    {
        preScalerValue = 32;
        _preScaler = _BV(CS21) | _BV(CS20);
    }
    _timerRegister = (byte)round(8000.0 / (setPointKhz * preScalerValue) - 1);
    OCR2A = _timerRegister;

    return round(80000.0 / (preScalerValue * (1 + _timerRegister))) / 10.0;
}

byte _lastButtonPress = BTN_NO_PRESS;

byte isButtonPressed()
{
    byte btn = digitalRead(PIN_BTN);
    if (btn == LOW && _debounceTime > 50 && _lastButtonPress == BTN_NO_PRESS)
    {
        _debounceTime = 0;
        while ((_debounceTime < 5 || btn == LOW) && _debounceTime < 500)
        {
            btn = digitalRead(PIN_BTN);
        }
        if (_debounceTime >= 500)
        {
            _lastButtonPress = BTN_LONG_PRESS;
        }
        else
        {
            _lastButtonPress = BTN_SHORT_PRESS;
        }
        _debounceTime = 0;
    }
    else if (btn == HIGH && _lastButtonPress != BTN_NO_PRESS && _debounceTime > 50)
    {
        _lastButtonPress = BTN_NO_PRESS;
        _debounceTime = 0;
    }
    else
    {
        return BTN_NO_PRESS;
    }
    return _lastButtonPress;
}


void loop()
{
    byte btnState = isButtonPressed();
    if (btnState == BTN_SHORT_PRESS)
    {
        _lcd->enter();
    }
    else if (btnState == BTN_LONG_PRESS)
    {
        _lcd->exit();
    }

    setFrequency(_freqValue);
    _lcd->update(_enc.read() / 4);

    switch (_currentMode)
    {
    case SIM_MODE_PULSE:
        if (_waitTime > 500)
        {
            pulse();
            _waitTime = 0;
        }
        break;
    case SIM_MODE_MANUAL:
        pwmOff();
        break;
    case SIM_MODE_CONTINOUS:
        pwmOn();
        break;
    }
}

