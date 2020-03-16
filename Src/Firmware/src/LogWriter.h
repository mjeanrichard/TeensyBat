#ifndef LogWriter_h
#define LogWriter_h

#include "BatAudio.h"
#include "Helpers.h"
#include <TimeLib.h>
#include "CardFormatter.h"

//TBXXX-YYYYMMDDHHMM-N.DAT\0
#define TB_FILENAME_LEN 25

class LogWriter
{
private:

    uint8_t _nodeId;

    BatAudio * _batAudio;
    SdFat _sd;
    SdFile _file;

    uint32_t _firstBlock;
    uint32_t _lastBlock;
    uint16_t _blocksWritten;

    bool _isFileOpen;

    void GenerateFilename(char * filename, tmElements_t time);
    void EraseFile();
    void CardError();
    void OpenNewFile();
    void WriteFileHeader();
    void CloseFile();
public:
    LogWriter(uint8_t nodeId, BatAudio * b) : 
        _sd(),
        _file()
	{
		_batAudio = b;
        _nodeId = nodeId;
        _isFileOpen = false;
	};

    void Process();
    void InitializeCard(bool openFile);
    bool IsCardAvailable();
    void FormatCard();
};


#endif