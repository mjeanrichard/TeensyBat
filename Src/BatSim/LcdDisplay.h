#pragma once
#include <Arduino.h>
#include <LiquidCrystal.h>
#include <math.h>

#define MENU_ITEM_MODE_HORIZONTAL 0
#define MENU_ITEM_MODE_VERTICAL   1
#define MENU_ITEM_MODE_VALUE      2

#define MENU_DIRECTION_UP_LEFT    0
#define MENU_DIRECTION_DOWN_RIGHT 1

class MenuItem
{
private:
public:
    MenuItem();

    byte _subMenuCount = 0;
    byte _currentIndex = 0;
    MenuItem * _parent;
    const char * name;
    MenuItem * * subMenus;
    char * value;
    byte mode;
    void (*updateFunc) (int);
    void (*enterFunc) (void);

    void createSubMenus(byte count);
};


class LcdDisplay
{
private:
    LiquidCrystal _lcd = LiquidCrystal(19, 18, 17, 16, 15, 14);

    MenuItem * _rootMenu;
    MenuItem * _currentMenu;

    int _lastEncoderValue = 0;
    byte _lastDirection = 1;

    void renderMenu();
    void renderMenuHorizontal();
    void renderMenuVertical();
public:
    LcdDisplay(MenuItem * rootMenu);
    
    void init();
    void update(int encoderValue);
    void enter();
    void exit();

    ~LcdDisplay();
};

