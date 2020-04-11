#include "LcdDisplay.h"

byte arrowLeft[8] = {
  B00000,
  B00100,
  B01100,
  B11100,
  B01100,
  B00100,
  B00000,
};

byte arrowRight[8] = {
  B00000,
  B00100,
  B00110,
  B00111,
  B00110,
  B00100,
  B00000,
};

#define CHAR_ARROW_LEFT (char)1
#define CHAR_ARROW_RIGHT (char)2

LcdDisplay::LcdDisplay(MenuItem * rootMenu)
{
    _rootMenu = rootMenu;
    _currentMenu = _rootMenu;
}

void LcdDisplay::init()
{
    _lcd.begin(16, 2);
    _lcd.createChar(1, arrowLeft);
    _lcd.createChar(2, arrowRight);
}

void LcdDisplay::update(int encoderValue)
{
    if (_lastEncoderValue == encoderValue)
    {
        renderMenu();
        return;
    }

    if (_currentMenu->updateFunc != nullptr)
    {
        _currentMenu->updateFunc(_lastEncoderValue - encoderValue);
    }

    if (_lastEncoderValue > encoderValue)
    {
        _lastDirection = MENU_DIRECTION_DOWN_RIGHT;
    } else
    {
        _lastDirection = MENU_DIRECTION_UP_LEFT;
    }

    if (_currentMenu->_subMenuCount > 0) 
    {
        int newVal = (_currentMenu->_currentIndex + (_lastEncoderValue - encoderValue)) % _currentMenu->_subMenuCount;
        if (newVal < 0)
        {
            newVal += _currentMenu->_subMenuCount;
        }
        _currentMenu->_currentIndex = newVal;
    }
    _lastEncoderValue = encoderValue;
    renderMenu();
}

void LcdDisplay::enter()
{
    MenuItem * newMenu = _currentMenu->subMenus[_currentMenu->_currentIndex];

    if (newMenu->mode == MENU_ITEM_MODE_SELECT) 
    {
        if (newMenu->enterFunc != nullptr)
        {
            newMenu->enterFunc();
        }
        return;
    } 
    
    if (_currentMenu->_subMenuCount == 0)
    {
        if (_currentMenu->enterFunc != nullptr)
        {
            _currentMenu->enterFunc();
        }
        return;
    }
    _currentMenu = newMenu;
    _lastDirection = 0;
    renderMenu();
}

void LcdDisplay::exit()
{
    if (_currentMenu == _rootMenu)
    {
        return;
    }
    _currentMenu = _currentMenu->_parent;
    renderMenu();
}

MenuItem::MenuItem()
{
    _currentIndex = 0;
    _subMenuCount = 0;
    mode = MENU_ITEM_MODE_VERTICAL;
    value = nullptr;
    updateFunc = nullptr;
    enterFunc = nullptr;
}

void MenuItem::createSubMenus(byte count)
{
    subMenus = new MenuItem*[count];
    _subMenuCount = count;
    for (byte i = 0; i < count; i++)
    {
        subMenus[i] = new MenuItem();
        subMenus[i]->_parent = this;
    }
}

void LcdDisplay::renderMenu()
{
    switch (_currentMenu->mode )
    {
    case MENU_ITEM_MODE_HORIZONTAL:
    case MENU_ITEM_MODE_VALUE:
        renderMenuHorizontal();
        break;
    case MENU_ITEM_MODE_VERTICAL:
    case MENU_ITEM_MODE_SELECT:
        renderMenuVertical();
        break;
    }
}

void chrCpy(char* dest, byte destIndex, const char* source, byte length)
{
    for (byte i = 0; i < length; i++)
    {
        dest[destIndex + i] = source[i];
    }
}

void LcdDisplay::renderMenuHorizontal()
{
    MenuItem * menu;
    if (_currentMenu->mode == MENU_ITEM_MODE_HORIZONTAL)
    {
        menu = _currentMenu;
    } else
    {
        menu = _currentMenu->_parent;
    }


    int charIndex = 1;

    char line1[] = "                ";
    char line2[] = "                ";
    for (byte i = 0; i < menu->_subMenuCount; i++)
    {
        MenuItem * item = menu->subMenus[i];
        byte nameLen = strlen(item->name);

        byte valLen = 0;
        if (item->value != nullptr)
        {
            valLen = strlen(item->value);
        }
        
        byte len = max(nameLen, valLen);

        if (charIndex + len <= 15)
        {
            chrCpy(line1, charIndex, item->name, nameLen);
            chrCpy(line2, charIndex, item->value, valLen);

            if (i == menu->_currentIndex)
            {
                if (_currentMenu->mode == MENU_ITEM_MODE_HORIZONTAL)
                {
                    line1[charIndex-1] = CHAR_ARROW_RIGHT;
                    line1[charIndex+nameLen] = CHAR_ARROW_LEFT;
                } 
                else if (valLen > 0)
                {
                    line2[charIndex-1] = CHAR_ARROW_RIGHT;
                    line2[charIndex+valLen] = CHAR_ARROW_LEFT;
                }
            }
            charIndex += len+1;
        }
        else
        {
            break;
        }
    }

    _lcd.setCursor(0, 0);
    _lcd.print(line1);
    _lcd.setCursor(0, 1);
    _lcd.print(line2);
}


void LcdDisplay::printHorizontalMenuItem(MenuItem * menuItem)
{
    PrintEx p = _lcd; 
    if (menuItem->mode == MENU_ITEM_MODE_SELECT) {
        if (menuItem->value[0] == ' '){
            p.printf("  %-13s", menuItem->name);
        } else {
            p.printf(" +%-13s", menuItem->name);
        }
    } else {
        p.printf(" %-14s", menuItem->name);
    }
}

void LcdDisplay::renderMenuVertical()
{
    byte idx = _currentMenu->_currentIndex;
    if (_lastDirection == MENU_DIRECTION_UP_LEFT || _currentMenu->_subMenuCount == 1)
    {
        _lcd.setCursor(0,0);
        _lcd.print(CHAR_ARROW_RIGHT);
        printHorizontalMenuItem(_currentMenu->subMenus[idx]);
        _lcd.setCursor(0,1);
        if (_currentMenu->_subMenuCount > 1)
        {
            idx = (idx + 1) % _currentMenu->_subMenuCount;
            _lcd.print(' ');
            printHorizontalMenuItem(_currentMenu->subMenus[idx]);
        }
        _lcd.print("                ");
    }
    else
    {
        _lcd.setCursor(0,1);
        _lcd.print(CHAR_ARROW_RIGHT);
        printHorizontalMenuItem(_currentMenu->subMenus[idx]);
        _lcd.setCursor(0,0);
        idx = (_currentMenu->_subMenuCount + idx - 1) % _currentMenu->_subMenuCount;
        _lcd.print(' ');
        printHorizontalMenuItem(_currentMenu->subMenus[idx]);
    }

}





LcdDisplay::~LcdDisplay()
{
}
