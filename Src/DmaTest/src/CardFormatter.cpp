#include "CardFormatter.h"

// constants for file system structure
#define BU16 128
#define BU32 8192

//  strings needed in file system structures
char DriveName[] = "NO NAME    ";
char fat16str[] = "FAT16   ";
char fat32str[] = "FAT32   ";

#define SD_ERROR(m, c) FatalError(c, F(m));

//------------------------------------------------------------------------------
// write cached block to the card
uint8_t CardFormatter::WriteCache(uint32_t lbn)
{
    return _card->writeBlock(lbn, _cache.data);
}

//------------------------------------------------------------------------------
// zero cache and optionally set the sector signature
void CardFormatter::ClearCache(uint8_t addSig)
{
    memset(&_cache, 0, sizeof(_cache));
    if (addSig)
    {
        _cache.mbr.mbrSig0 = BOOTSIG0;
        _cache.mbr.mbrSig1 = BOOTSIG1;
    }
}

//------------------------------------------------------------------------------
// zero FAT and root dir area on SD
void CardFormatter::ClearFatDir(uint32_t bgn, uint32_t count)
{
    ClearCache(false);

    if (!_card->writeStart(bgn, count))
    {
        SD_ERROR("Clear FAT/DIR writeStart failed", TB_ERR_SD_FORMAT)
    }
    for (uint32_t i = 0; i < count; i++)
    {
        if ((i & 0XFF) == 0)
        {
            MESSAGE('.')
        }
        if (!_card->writeData(_cache.data))
        {
            SD_ERROR("Clear FAT/DIR writeData failed", TB_ERR_SD_FORMAT)
        }
    }
    if (!_card->writeStop())
    {
        SD_ERROR("Clear FAT/DIR writeStop failed", TB_ERR_SD_FORMAT)
    }
}

//------------------------------------------------------------------------------
// return cylinder number for a logical block number
uint16_t CardFormatter::LbnToCylinder(uint32_t lbn)
{
    return lbn / (_numberOfHeads * _sectorsPerTrack);
}

//------------------------------------------------------------------------------
// return head number for a logical block number
uint8_t CardFormatter::LbnToHead(uint32_t lbn)
{
    return (lbn % (_numberOfHeads * _sectorsPerTrack)) / _sectorsPerTrack;
}

//------------------------------------------------------------------------------
// return sector number for a logical block number
uint8_t CardFormatter::LbnToSector(uint32_t lbn)
{
    return (lbn % _sectorsPerTrack) + 1;
}

void CardFormatter::WriteMbr()
{
    ClearCache(true);
    part_t *p = _cache.mbr.part;
    p->boot = 0;
    uint16_t c = LbnToCylinder(_relSector);
    if (c > 1023)
    {
        SD_ERROR("MBR CHS", TB_ERR_SD_FORMAT)
    }
    p->beginCylinderHigh = c >> 8;
    p->beginCylinderLow = c & 0XFF;
    p->beginHead = LbnToHead(_relSector);
    p->beginSector = LbnToSector(_relSector);
    p->type = _partType;
    uint32_t endLbn = _relSector + _partSize - 1;
    c = LbnToCylinder(endLbn);
    if (c <= 1023)
    {
        p->endCylinderHigh = c >> 8;
        p->endCylinderLow = c & 0XFF;
        p->endHead = LbnToHead(endLbn);
        p->endSector = LbnToSector(endLbn);
    }
    else
    {
        // Too big flag, c = 1023, h = 254, s = 63
        p->endCylinderHigh = 3;
        p->endCylinderLow = 255;
        p->endHead = 254;
        p->endSector = 63;
    }
    p->firstSector = _relSector;
    p->totalSectors = _partSize;
    if (!WriteCache(0))
    {
        SD_ERROR("write MBR", TB_ERR_SD_FORMAT)
    }
}

//------------------------------------------------------------------------------
// generate serial number from card size and micros since boot
uint32_t CardFormatter::GetVolSerialNumber()
{
    return (_cardSizeBlocks << 8) + micros();
}

void CardFormatter::MakeFat16()
{
    uint32_t numberOfClusters;
    for (_dataStart = 2 * BU16;; _dataStart += BU16)
    {
        numberOfClusters = (_cardSizeBlocks - _dataStart) / _sectorsPerCluster;
        _fatSize = (numberOfClusters + 2 + 255) / 256;
        uint32_t r = BU16 + 1 + 2 * _fatSize + 32;
        if (_dataStart < r)
        {
            continue;
        }
        _relSector = _dataStart - r + BU16;
        break;
    }
    // check valid cluster count for FAT16 volume
    if (numberOfClusters < 4085 || numberOfClusters >= 65525)
    {
        SD_ERROR("Bad cluster count", TB_ERR_SD_FORMAT)
    }

    _reservedSectors = 1;
    _fatStart = _relSector + _reservedSectors;
    _partSize = numberOfClusters * _sectorsPerCluster + 2 * _fatSize + _reservedSectors + 32;

    if (_partSize < 32680)
    {
        _partType = 0X01;
    }
    else if (_partSize < 65536)
    {
        _partType = 0X04;
    }
    else
    {
        _partType = 0X06;
    }
    WriteMbr();

    ClearCache(true);
    fat_boot_t *pb = &_cache.fbs;
    pb->jump[0] = 0XEB;
    pb->jump[1] = 0X00;
    pb->jump[2] = 0X90;
    for (uint8_t i = 0; i < sizeof(pb->oemId); i++)
    {
        pb->oemId[i] = ' ';
    }
    pb->bytesPerSector = 512;
    pb->sectorsPerCluster = _sectorsPerCluster;
    pb->reservedSectorCount = _reservedSectors;
    pb->fatCount = 2;
    pb->rootDirEntryCount = 512;
    pb->mediaType = 0XF8;
    pb->sectorsPerFat16 = _fatSize;
    pb->sectorsPerTrack = _sectorsPerTrack;
    pb->headCount = _numberOfHeads;
    pb->hidddenSectors = _relSector;
    pb->totalSectors32 = _partSize;
    pb->driveNumber = 0X80;
    pb->bootSignature = EXTENDED_BOOT_SIG;
    pb->volumeSerialNumber = GetVolSerialNumber();
    memcpy(pb->volumeLabel, DriveName, sizeof(pb->volumeLabel));
    memcpy(pb->fileSystemType, fat16str, sizeof(pb->fileSystemType));
    // write partition boot sector
    if (!WriteCache(_relSector))
    {
        SD_ERROR("FAT16 write PBS failed", TB_ERR_SD_FORMAT)
    }
    // clear FAT and root directory
    ClearFatDir(_fatStart, _dataStart - _fatStart);
    ClearCache(false);
    _cache.fat16[0] = 0XFFF8;
    _cache.fat16[1] = 0XFFFF;
    // write first block of FAT and backup for reserved clusters
    if (!WriteCache(_fatStart) || !WriteCache(_fatStart + _fatSize))
    {
        SD_ERROR("FAT16 reserve failed", TB_ERR_SD_FORMAT)
    }
}

//------------------------------------------------------------------------------
// format the SD as FAT32
void CardFormatter::MakeFat32()
{
    uint32_t numberOfClusters;
    _relSector = BU32;
    for (_dataStart = 2 * BU32;; _dataStart += BU32)
    {
        numberOfClusters = (_cardSizeBlocks - _dataStart) / _sectorsPerCluster;
        _fatSize = (numberOfClusters + 2 + 127) / 128;
        uint32_t r = _relSector + 9 + 2 * _fatSize;
        if (_dataStart >= r)
        {
            break;
        }
    }
    // error if too few clusters in FAT32 volume
    if (numberOfClusters < 65525)
    {
        SD_ERROR("Bad cluster count", TB_ERR_SD_FORMAT)
    }
    _reservedSectors = _dataStart - _relSector - 2 * _fatSize;
    _fatStart = _relSector + _reservedSectors;
    _partSize = numberOfClusters * _sectorsPerCluster + _dataStart - _relSector;
    // type depends on address of end sector
    // max CHS has lbn = 16450560 = 1024*255*63
    if ((_relSector + _partSize) <= 16450560)
    {
        // FAT32
        _partType = 0X0B;
    }
    else
    {
        // FAT32 with INT 13
        _partType = 0X0C;
    }
    WriteMbr();
    ClearCache(true);

    fat32_boot_t *pb = &_cache.fbs32;
    pb->jump[0] = 0XEB;
    pb->jump[1] = 0X00;
    pb->jump[2] = 0X90;
    for (uint8_t i = 0; i < sizeof(pb->oemId); i++)
    {
        pb->oemId[i] = ' ';
    }
    pb->bytesPerSector = 512;
    pb->sectorsPerCluster = _sectorsPerCluster;
    pb->reservedSectorCount = _reservedSectors;
    pb->fatCount = 2;
    pb->mediaType = 0XF8;
    pb->sectorsPerTrack = _sectorsPerTrack;
    pb->headCount = _numberOfHeads;
    pb->hidddenSectors = _relSector;
    pb->totalSectors32 = _partSize;
    pb->sectorsPerFat32 = _fatSize;
    pb->fat32RootCluster = 2;
    pb->fat32FSInfo = 1;
    pb->fat32BackBootBlock = 6;
    pb->driveNumber = 0X80;
    pb->bootSignature = EXTENDED_BOOT_SIG;
    pb->volumeSerialNumber = GetVolSerialNumber();
    memcpy(pb->volumeLabel, DriveName, sizeof(pb->volumeLabel));
    memcpy(pb->fileSystemType, fat32str, sizeof(pb->fileSystemType));
    // write partition boot sector and backup
    if (!WriteCache(_relSector) || !WriteCache(_relSector + 6))
    {
        SD_ERROR("FAT32 write PBS failed", TB_ERR_SD_FORMAT)
    }
    ClearCache(true);
    // write extra boot area and backup
    if (!WriteCache(_relSector + 2) || !WriteCache(_relSector + 8))
    {
        SD_ERROR("FAT32 PBS ext failed", TB_ERR_SD_FORMAT)
    }
    fat32_fsinfo_t *pf = &_cache.fsinfo;
    pf->leadSignature = FSINFO_LEAD_SIG;
    pf->structSignature = FSINFO_STRUCT_SIG;
    pf->freeCount = 0XFFFFFFFF;
    pf->nextFree = 0XFFFFFFFF;
    // write FSINFO sector and backup
    if (!WriteCache(_relSector + 1) || !WriteCache(_relSector + 7))
    {
        SD_ERROR("FAT32 FSINFO failed", TB_ERR_SD_FORMAT)
    }
    ClearFatDir(_fatStart, 2 * _fatSize + _sectorsPerCluster);
    ClearCache(false);
    _cache.fat32[0] = 0x0FFFFFF8;
    _cache.fat32[1] = 0x0FFFFFFF;
    _cache.fat32[2] = 0x0FFFFFFF;
    // write first block of FAT and backup for reserved clusters
    if (!WriteCache(_fatStart) || !WriteCache(_fatStart + _fatSize))
    {
        SD_ERROR("FAT32 reserve failed", TB_ERR_SD_FORMAT)
    }
}

//------------------------------------------------------------------------------
// flash erase all data
#define ERASE_SIZE 262144L
void CardFormatter::EraseCard()
{
    uint32_t cardSizeBlocks = _card->cardSize();
    if (cardSizeBlocks == 0)
    {
        SD_ERROR("cardSize", TB_ERR_SD_FORMAT)
    }
    DEBUG("Erasing Card...\n");
    uint32_t firstBlock = 0;
    uint32_t lastBlock;

    do
    {
        lastBlock = firstBlock + ERASE_SIZE - 1;
        if (lastBlock >= cardSizeBlocks)
        {
            lastBlock = cardSizeBlocks - 1;
        }
        if (!_card->erase(firstBlock, lastBlock))
        {
            SD_ERROR("erase failed", TB_ERR_SD_FORMAT)
        }
        firstBlock += ERASE_SIZE;
    } while (firstBlock < cardSizeBlocks);

    DEBUG("Card successfully erased.\n");
}

void CardFormatter::Format()
{
    if (_card->type() != SD_CARD_TYPE_SDHC)
    {
        MESSAGEF("Starting format (%lu mb) with FAT16...\n", _cardCapacityMB);
        EraseCard();
        MakeFat16();
    }
    else
    {
        MESSAGEF("Starting format (%lu mb) with FAT32...\n", _cardCapacityMB);
        EraseCard();
        MakeFat32();
    }
    MESSAGE("Formatting completed.\n");
}

CardFormatter::CardFormatter(SdSpiCard *card)
{
    _cache = cache_t();
    _card = card;

    _cardSizeBlocks = _card->cardSize();
    if (_cardSizeBlocks == 0)
    {
        SD_ERROR("cardSize", TB_ERR_SD_FORMAT)
    }
    _cardCapacityMB = (_cardSizeBlocks + 2047) / 2048;

    if (_cardCapacityMB <= 6)
    {
        SD_ERROR("Card is too small.", TB_ERR_SD_FORMAT)
    }
    else if (_cardCapacityMB <= 16)
    {
        _sectorsPerCluster = 2;
    }
    else if (_cardCapacityMB <= 32)
    {
        _sectorsPerCluster = 4;
    }
    else if (_cardCapacityMB <= 64)
    {
        _sectorsPerCluster = 8;
    }
    else if (_cardCapacityMB <= 128)
    {
        _sectorsPerCluster = 16;
    }
    else if (_cardCapacityMB <= 1024)
    {
        _sectorsPerCluster = 32;
    }
    else if (_cardCapacityMB <= 32768)
    {
        _sectorsPerCluster = 64;
    }
    else
    {
        // SDXC cards
        _sectorsPerCluster = 128;
    }

    // set fake disk geometry
    _sectorsPerTrack = _cardCapacityMB <= 256 ? 32 : 63;

    if (_cardCapacityMB <= 16)
    {
        _numberOfHeads = 2;
    }
    else if (_cardCapacityMB <= 32)
    {
        _numberOfHeads = 4;
    }
    else if (_cardCapacityMB <= 128)
    {
        _numberOfHeads = 8;
    }
    else if (_cardCapacityMB <= 504)
    {
        _numberOfHeads = 16;
    }
    else if (_cardCapacityMB <= 1008)
    {
        _numberOfHeads = 32;
    }
    else if (_cardCapacityMB <= 2016)
    {
        _numberOfHeads = 64;
    }
    else if (_cardCapacityMB <= 4032)
    {
        _numberOfHeads = 128;
    }
    else
    {
        _numberOfHeads = 255;
    }
};
