#ifndef FORMATCARD_h
#define FORMATCARD_h

#include <SPI.h>
#include "SdFat.h"
#include "Helpers.h"


class CardFormatter
{
  private:
    uint32_t _cardSizeBlocks;
    uint32_t _cardCapacityMB;

    // MBR information
    uint8_t _partType;
    uint32_t _relSector;
    uint32_t _partSize;

    // Fake disk geometry
    uint8_t _numberOfHeads;
    uint8_t _sectorsPerTrack;

    // FAT parameters
    uint16_t _reservedSectors;
    uint8_t _sectorsPerCluster;
    uint32_t _fatStart;
    uint32_t _fatSize;
    uint32_t _dataStart;

    SdSpiCard *_card;
    cache_t _cache;

    uint32_t GetVolSerialNumber();
    uint16_t LbnToCylinder(uint32_t lbn);
    uint8_t LbnToHead(uint32_t lbn);
    uint8_t LbnToSector(uint32_t lbn);

    uint8_t WriteCache(uint32_t lbn);
    void ClearCache(uint8_t addSig);

    void MakeFat16();
    void MakeFat32();

    void WriteMbr();
    void ClearFatDir(uint32_t bgn, uint32_t count);

    void EraseCard();

  public:
    CardFormatter(SdSpiCard *card);
    void Format();
};


#endif