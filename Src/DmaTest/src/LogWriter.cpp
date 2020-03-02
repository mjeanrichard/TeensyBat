#include "LogWriter.h"

void LogWriter::Process()
{
    if (!_isFileOpen)
    {
        OpenNewFile();
    }
    if (_batAudio->hasDataAvailable())
    {
        if (_blocksWritten > TB_FILE_BLOCK_COUNT)
        {
            FatalError(TB_ERR_SD_WRITE_BLOCK, F("Ups, should have opened new file..."));
        }
        if (!_sd.card()->writeStart(_firstBlock + _blocksWritten, TB_FILE_BLOCK_COUNT))
        {
            FatalError(TB_ERR_SD_WRITE_BLOCK, F("Could not write Block (writeStart failed)."));
        }

        bool lastResult = true;
        while (_batAudio->hasDataAvailable())
        {
            uint8_t *pCache = (uint8_t *)_sd.vol()->cacheClear();
            lastResult = _batAudio->writeToCard(&_blocksWritten, &_sd, TB_FILE_BLOCK_COUNT, pCache);
            if (!lastResult){
                break;
            }
            DEBUG_F(". %u\n", _blocksWritten)
        }

        if (!_sd.card()->writeStop())
        {
            FatalError(TB_ERR_SD_WRITE_BLOCK, F("Could not write Block (writeStop failed)."));
        }
        if (!lastResult)
        {
            CloseFile();
        }
    }
}

void LogWriter::CloseFile()
{
    if (_isFileOpen)
    {
        _file.close();
        _isFileOpen = false;
    }
}

void LogWriter::GenerateFilename(char *filename, tmElements_t time)
{
    for (uint8_t i = 0; i < 10; i++)
    {
        sprintf(filename, "TB%03hhu-%04u%02hhu%02hhu%02hhu%02hhu-%01hhu.DAT", _nodeId, 1970 + time.Year, time.Month, time.Day, time.Hour, time.Minute, i);
        if (!_sd.exists(filename))
        {
            DEBUG("Filename: ")
            DEBUG_LN(filename)
            return;
        }
    }
    FatalError(TB_ERR_SD_NO_FILENAME, F("Could not find a free filename."));
}

void LogWriter::InitializeCard(bool openFile)
{
    if (!IsCardAvailable())
    {
        MESSAGE("No Card detected.\n")
        while (!IsCardAvailable())
        {
            digitalWriteFast(TB_PIN_LED_YELLOW, HIGH);
            digitalWriteFast(TB_PIN_LED_RED, LOW);
            delay(200);
            digitalWriteFast(TB_PIN_LED_YELLOW, LOW);
            digitalWriteFast(TB_PIN_LED_RED, HIGH);
            delay(200);
        }
        delay(500);
        MESSAGE("Ok, got card.\n")
    }

    if (!_sd.begin(TB_PIN_SDCS, SPI_FULL_SPEED))
    {
        _sd.initErrorPrint();
        FatalError(TB_ERR_SD_INIT, F("Could not initialize Card."));
    }
    MESSAGE("Card initialized successfully.\n")
    
    if (openFile)
    {
        OpenNewFile();
    }
}

void LogWriter::OpenNewFile()
{
    char filename[TB_FILENAME_LEN];

    tmElements_t time;
    breakTime(Teensy3Clock.get(), time);
    GenerateFilename(filename, time);

    if (!_file.createContiguous(filename, TB_SD_BUFFER_SIZE * TB_FILE_BLOCK_COUNT))
    {
        MESSAGE("Ups!")
        _file.close();
        _sd.card()->spiStop();
        delay(100);
        if (!_sd.begin(TB_PIN_SDCS, SPI_FULL_SPEED))
        {
            _sd.initErrorPrint();
            FatalError(TB_ERR_SD_INIT, F("Could not initialize Card."));
        }

        if (!_file.createContiguous(filename, TB_SD_BUFFER_SIZE * TB_FILE_BLOCK_COUNT))
        {
            FatalError(TB_ERR_SD_CREATE_FILE, F("Could not create contiguous file."));
        }
    }
    if (!_file.contiguousRange(&_firstBlock, &_lastBlock))
    {
        FatalError(TB_ERR_SD_CREATE_FILE, F("Could not create contiguous range."));
    }
    
    _file.timestamp(T_CREATE | T_ACCESS | T_WRITE, 1970 + time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
    
    EraseFile();
    WriteFileHeader();
    _blocksWritten = 1;
    _isFileOpen = true;
    DEBUG("Created File.\n");
}

void LogWriter::WriteFileHeader()
{
    uint8_t *pBuffer = (uint8_t *)_sd.vol()->cacheClear();
    
    pBuffer[0] = 0xFF;
    pBuffer[1] = 0xCC;
    pBuffer[2] = TB_HW_VERSION;
    pBuffer[3] = TB_FW_VERSION;
    
    pBuffer[4] = _nodeId;
    pBuffer[5] = TB_DEBUG;
    pBuffer[6] = TB_PRE_CALL_BUFFER_COUNT;
    pBuffer[7] = TB_AFTER_CALL_SAMPLES;

    WriteInt16(pBuffer + 8, TB_CALL_START_THRESHOLD);
    WriteInt16(pBuffer + 10, TB_CALL_STOP_THRESHOLD);

    pBuffer[13] = 0;
    pBuffer[14] = 0;
    pBuffer[15] = 0;
    pBuffer[16] = 0;

  	uint32_t time = Teensy3Clock.get();
    uint32_t offset = micros();
    WriteInt32(pBuffer + 16, time);
    WriteInt32(pBuffer + 24, offset);
}

void LogWriter::CardError()
{
    elapsedMillis tBlink = 0;
    elapsedMillis tButton;
    bool s1Pressed = false;
    while (true)
    {
        if (tBlink > 400)
        {
            digitalWriteFast(TB_PIN_LED_YELLOW, LOW);
            tBlink = 0;
        }
        else if (tBlink > 200)
        {
            digitalWriteFast(TB_PIN_LED_YELLOW, HIGH);
        }
        if (s1Pressed == false && digitalReadFast(TB_PIN_S1) == LOW)
        {
            // S1 first pressed
            tButton = 0;
            s1Pressed = true;
        }
        else if (s1Pressed == true && digitalReadFast(TB_PIN_S1) == HIGH)
        {
            // S1 not pressed anymore
            s1Pressed = false;
        }
        else if (s1Pressed == true && tButton > 5000)
        {
            // S1 pressed for over 5 secs
            FormatCard();
            s1Pressed = false;
        }
    }
}

void LogWriter::FormatCard()
{
    digitalWriteFast(TB_PIN_LED_GREEN, HIGH);
    CardFormatter cf = CardFormatter(_sd.card());
    cf.Format();
    digitalWriteFast(TB_PIN_LED_GREEN, LOW);
}

bool LogWriter::IsCardAvailable()
{
    return !digitalReadFast(TB_PIN_CARD_PRESENT);
}

void LogWriter::EraseFile()
{
    SD_ACTIVE_ON()
    // if (_sd.card()->eraseSingleBlockEnable())
    // {
    //     DEBUG("Block-Erasing File...")
    //     if (!_sd.card()->erase(_firstBlock, _lastBlock))
    //     {
    //         FatalError(TB_ERR_SD_CREATE_FILE, F("Failed to erase file."));
    //     }
    //     DEBUG(" done.\n");
    // }
    // else
    // {
        DEBUG("Erasing manually...");
        if (!_sd.card()->writeStart(_firstBlock, TB_FILE_BLOCK_COUNT))
        {
            FatalError(TB_ERR_SD_CREATE_FILE, F("Block Start failed!"));
        }

        uint8_t *pCache = (uint8_t *)_sd.vol()->cacheClear();
        memset(pCache, 0x00, TB_SD_BUFFER_SIZE);
        for (int i = 0; i < TB_FILE_BLOCK_COUNT; i++)
        {
            _sd.card()->writeData(pCache);
        }

        if (!_sd.card()->writeStop())
        {
            FatalError(TB_ERR_SD_CREATE_FILE, F("Failed to erase file manually."));
        }

        DEBUG(" done.\n");
    // }
    SD_ACTIVE_OFF()
}
