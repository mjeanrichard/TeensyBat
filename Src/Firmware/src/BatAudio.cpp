#include "BatAudio.h"

DMAChannel BatAudio::_dma(false);
DMAMEM static uint16_t dmaSampleBuffer[TB_AUDIO_SAMPLE_BUFFER_SIZE];

ADC BatAudio::_adc;
BatAudio *BatAudio::_instance;

#define PDB_CONFIG (PDB_SC_TRGSEL(15) | PDB_SC_PDBEN | PDB_SC_CONT | PDB_SC_PDBIE | PDB_SC_DMAEN)

void BatAudio::init()
{
	EEPROM.get(TB_EEPROM_V_FACT, _voltageFactor);

    _adc.adc0->setAveraging(4);
    _adc.adc0->setResolution(12);
    _adc.adc0->setConversionSpeed(ADC_CONVERSION_SPEED::VERY_HIGH_SPEED);
    _adc.adc0->setSamplingSpeed(ADC_SAMPLING_SPEED::VERY_HIGH_SPEED);
    _adc.adc0->setReference(ADC_REFERENCE::REF_3V3);
    _adc.adc0->startSingleRead(TB_PIN_AUDIO);

    _adc.adc1->setAveraging(4);
    _adc.adc1->setResolution(12);
    _adc.adc1->setConversionSpeed(ADC_CONVERSION_SPEED::MED_SPEED);
    _adc.adc1->setSamplingSpeed(ADC_SAMPLING_SPEED::MED_SPEED);
    _adc.adc1->setReference(ADC_REFERENCE::REF_3V3);
    _adc.adc1->startSingleRead(TB_PIN_ENVELOPE);
    _adc.adc1->enableInterrupts(adc1_isr, 150);

    // set the programmable delay block to trigger the ADC at (120 CPU Clock, 60 Mhz Bus)
    // 60Mhz / 600 = 100kHz
    // 60Mhz / 234 = 256.4kHz
    SIM_SCGC6 |= SIM_SCGC6_PDB;
    PDB0_IDLY = 1;
    PDB0_MOD = 234 - 1;
    PDB0_SC = PDB_CONFIG | PDB_SC_LDOK;
    PDB0_CH0C1 = 0x0101;

    // enable the ADC for hardware trigger and DMA
    ADC0_SC2 |= ADC_SC2_ADTRG | ADC_SC2_DMAEN;

    NVIC_SET_PRIORITY(IRQ_SOFTWARE, 208); // 255 = lowest priority
    NVIC_ENABLE_IRQ(IRQ_SOFTWARE);
}

void BatAudio::start()
{
    _isEnabled = true;
    // set up a DMA channel to store the ADC data
    _dma.clearError();
    _dma.begin(true);

    _dma.TCD->SADDR = &ADC0_RA;
    _dma.TCD->SOFF = 0;
    _dma.TCD->ATTR = DMA_TCD_ATTR_SSIZE(1) | DMA_TCD_ATTR_DSIZE(1);
    _dma.TCD->NBYTES_MLNO = 2;
    _dma.TCD->SLAST = 0;
    _dma.TCD->DADDR = dmaSampleBuffer;
    _dma.TCD->DOFF = 2;
    _dma.TCD->CITER_ELINKNO = sizeof(dmaSampleBuffer) / 2;
    _dma.TCD->DLASTSGA = -sizeof(dmaSampleBuffer);
    _dma.TCD->BITER_ELINKNO = sizeof(dmaSampleBuffer) / 2;
    _dma.TCD->CSR = DMA_TCD_CSR_INTHALF | DMA_TCD_CSR_INTMAJOR;

    _dma.triggerAtHardwareEvent(DMAMUX_SOURCE_ADC0);
    _dma.attachInterrupt(BatAudio::adc0_isr);
    _dma.enable();

    PDB0_SC = PDB_CONFIG | PDB_SC_SWTRIG;
}

void BatAudio::stop()
{
    _dma.disable();
    _isEnabled = false;
}

int16_t BatAudio::readRawBatteryVoltage()
{
    if (!_isEnabled)
    {
        return readRawBatteryVoltageInternal();
    }
    return -1;
}

int16_t BatAudio::readRawBatteryVoltageInternal()
{
    _adc.adc1->disableInterrupts();
    int16_t rawVoltage = _adc.adc1->analogRead(TB_PIN_BATTERY);
    _adc.adc1->enableInterrupts(adc1_isr, 150);
    return rawVoltage;
}

int16_t BatAudio::readTempInternal()
{
    _adc.adc1->disableInterrupts();
    int16_t adcVal = _adc.adc1->analogRead(ADC_INTERNAL_SOURCE::TEMP_SENSOR);
    _adc.adc1->enableInterrupts(adc1_isr, 150);
    if (adcVal < 0)
    {
        return -9999;
    }
    return (-3331*adcVal + 3149817)>>10;
}

int16_t BatAudio::readTempC()
{
    if (!_isEnabled)
    {
        return readTempInternal();
    }
    return -999;
}

void BatAudio::adc0_isr()
{
    uint32_t daddr = (uint32_t)(_dma.TCD->DADDR);
    _dma.clearInterrupt();

    _adc.adc1->startSingleRead(TB_PIN_ENVELOPE);

    uint16_t *dst;
    const uint16_t *src, *end;

    if (daddr < (uint32_t)dmaSampleBuffer + sizeof(dmaSampleBuffer) / 2)
    {
        // DMA is receiving to the first half of the buffer
        // need to remove data from the second half
        src = (uint16_t *)&dmaSampleBuffer[TB_AUDIO_SAMPLE_BUFFER_SIZE / 2];
        dst = (uint16_t *)&(_instance->_currentSamplingBuffer[TB_AUDIO_SAMPLE_BUFFER_SIZE / 2]);
        end = (uint16_t *)&dmaSampleBuffer[TB_AUDIO_SAMPLE_BUFFER_SIZE];

        do
        {
            *dst++ = *src++;
        } while (src < end);

        if (!_instance->_isProcessingSample)
        {
            _instance->_envelopeValueAfterSampleRead = _instance->_lastEnvelopeValue;
            _instance->rotateSamplingBuffer();
            NVIC_SET_PENDING(IRQ_SOFTWARE);
        }
        else
        {
            _instance->_errProcessOverlap++;
            DEBUG_LN("Process overlap!")
        }
    }
    else
    {
        // DMA is receiving to the second half of the buffer
        // need to remove data from the first half
        src = (uint16_t *)dmaSampleBuffer;
        dst = (uint16_t *)_instance->_currentSamplingBuffer;
        end = (uint16_t *)&dmaSampleBuffer[TB_AUDIO_SAMPLE_BUFFER_SIZE / 2];

        do
        {
            *dst++ = *src++;
        } while (src < end);
    }
}

void BatAudio::rotateSamplingBuffer()
{
    uint16_t *tmp = _instance->_lastSamplingBuffer;
    _instance->_lastSamplingBuffer = _instance->_completedSamplingBuffer;
    _instance->_completedSamplingBuffer = _instance->_currentSamplingBuffer;
    _instance->_currentSamplingBuffer = tmp;
}

void adc1_isr(void)
{
    BatAudio::_instance->_lastEnvelopeValue = BatAudio::_adc.adc1->readSingle();
}

void software_isr(void)
{
    BatAudio::_instance->_isProcessingSample = true;
    BatAudio::_instance->sample_complete_isr();
    BatAudio::_instance->_isProcessingSample = false;
}

void BatAudio::sample_complete_isr()
{
    if (_callBufferEntries >= TB_CALL_BUFFER_COUNT - 2)
    {
        MESSAGE("Call Buffer Full!\n")
        CALL_BUFFER_FULL()
        _errCallBufferFull++;
    } else {
        CALL_BUFFER_NOT_FULL()
    }

    uint16_t envelopeValue = _envelopeValueAfterSampleRead;

    // prepare fftBuffer
    uint16_t *s = (uint16_t *)_lastSamplingBuffer;
    uint32_t *d = (uint32_t *)this->_fftBuffer;
    const uint16_t *window = AudioWindowHanning256;
    for (uint8_t i = 0; i < TB_AUDIO_SAMPLE_BUFFER_SIZE; i++)
    {
        uint32_t v = *s * *window++;
        *d = v >> 15;
        s++;
        d++;
    }
    s = (uint16_t *)_completedSamplingBuffer;
    for (uint8_t i = 0; i < TB_AUDIO_SAMPLE_BUFFER_SIZE; i++)
    {
        uint32_t v = *s * *window++;
        *d = v >> 15;
        s++;
        d++;
    }

    _sampleCounter++;

    // For a new Call to start we need at least 3 free entries in the Buffer.
    if (!_isCallInProgress && envelopeValue > TB_CALL_START_THRESHOLD && _callBufferEntries < TB_CALL_BUFFER_COUNT - 2)
    {
        uint8_t nextCallPointerIndex = (_callPointerIndexHead + 1) % TB_CALL_POINTER_COUNT;
        if (nextCallPointerIndex == _callPointerIndexTail)
        {
            MESSAGE("Call Pointer Buffer Full!\n");
            _errCallPointerBufferFull++;
            return;
        }
        _isCallInProgress = true;
        CALL_INPROGRESS()
        _currentCall = &_callPointers[nextCallPointerIndex];
        _currentCall->startOfData = _callBufferNextByte;
        _currentCall->startTime = millis();
        _currentCall->length = 0;
        _currentCall->maxLevel = 0;
        _currentCall->highFreqSampleCount = 0;
        _currentCall->highPowerSampleCount = 0;

        _afterCallSampleCount = 0;

        // new call started, get calls from pre call buffer
        // preCallBufferCount will be equal to PRE_CALL_BUFFER_COUNT in most cases
        // except if the buffer has not yet been filled after a call ended.
        // We need to get the oldest item in the buffer first. If the Buffer is full, this is
        // the current item, if not, this is the preCallBufferIndex - preCallBufferCount.
        // Add PRE_CALL_BUFFER_COUNT to ensure that modulus is positive
        uint8_t p = _preCallBufferIndex + TB_PRE_CALL_BUFFER_COUNT - _preCallBufferCount;

        for (int i = 0; i < _preCallBufferCount; i++)
        {
            uint8_t * q = _preCallBuffer + ((p + i) % TB_PRE_CALL_BUFFER_COUNT) * TB_CALL_DATA_SIZE;
            copyToCallBuffer(q);
            _currentCall->length++;
        }
        _preCallBufferCount = 0;

        // add the current call
        computeFFT(_callBufferNextByte, _currentCall, envelopeValue);
        increaseCallBuffer();
        _currentCall->length++;
    }
    else if (_isCallInProgress                                     // Continue recording if there is a call already in progress
             && (envelopeValue > TB_CALL_STOP_THRESHOLD       // and sound level high enough
                 || _afterCallSampleCount < TB_AFTER_CALL_SAMPLES) // or not enough "after" samples collected yet
             && _callBufferEntries < TB_CALL_BUFFER_COUNT          // and enough memory available
             && _currentCall->length < 200)                        // Max Call length not yet reached
    {
        if (envelopeValue <= TB_CALL_STOP_THRESHOLD)
        {
            _afterCallSampleCount++;
        }
        else if (envelopeValue > TB_CALL_START_THRESHOLD)
        {
            _afterCallSampleCount = 0;
        }
        // call in progress, record FFT
        computeFFT(_callBufferNextByte, _currentCall, envelopeValue);
        increaseCallBuffer();
        _currentCall->length++;
    }
    else if (_isCallInProgress)
    {
        // Either no more space in buffer or call has ended
        // add the last call to the buffer if there is enough space
        if (_callBufferEntries < TB_CALL_BUFFER_COUNT)
        {
            computeFFT(_callBufferNextByte, _currentCall, envelopeValue);
            increaseCallBuffer();
            _currentCall->length++;
        } else {
            _errCallBufferFull++;
        }

        _callPointerIndexHead = (_callPointerIndexHead + 1) % TB_CALL_POINTER_COUNT;
        _isCallInProgress = false;
        CALL_NOT_INPROGRESS()
        _currentCall = nullptr;
    }
    else
    {
        // No call yet -> buffer in case we need it later
        computeFFT(_preCallBuffer + _preCallBufferIndex * TB_CALL_DATA_SIZE, nullptr, envelopeValue);
        _preCallBufferIndex++;
        if (_preCallBufferIndex >= TB_PRE_CALL_BUFFER_COUNT)
        {
            _preCallBufferIndex = 0;
        }
        if (_preCallBufferCount < TB_PRE_CALL_BUFFER_COUNT)
        {
            _preCallBufferCount++;
        }

        if (_msSinceBatteryRead >= TB_MS_BETWEEN_BATTERY_READS)
        {
            int16_t volt = (readRawBatteryVoltageInternal() * _voltageFactor) / 1000;
            AddAdditionalData(AdditionalDataType::Battery, volt);
            DEBUG_F("Read Voltage: %hu.\n", volt);
            _msSinceBatteryRead = 0;
        }

        if (_msSinceTempRead >= TB_MS_BETWEEN_TEMP_READS)
        {
            int16_t temp = readTempInternal();
            AddAdditionalData(AdditionalDataType::Temp, temp);
            DEBUG_F("Read Temp: %hi.\n", temp);
            _msSinceTempRead = 0;
        }
    }
}

void BatAudio::computeFFT(uint8_t *dest, CallPointer * callPointer, uint16_t envelopeValue)
{
    *dest = (byte)(envelopeValue);
    dest++;
    *dest = (byte)(envelopeValue >> 8);
    dest++;
    *dest = (byte)(_sampleCounter);
    dest++;
    *dest = (byte)(_sampleCounter >> 8);
    dest++;

    uint8_t maxLevel = 0;
    uint8_t maxLevelCount = 0;

    arm_cfft_q15(&arm_cfft_sR_q15_len256, (q15_t *)_fftBuffer, 0, 1);
    for (int i = 0; i < TB_FFT_RESULT_SIZE; i++)
    {
        uint32_t tmp = *((uint32_t *)_fftBuffer + i); // real & imag
        uint32_t magsq = multiply_16tx16t_add_16bx16b(tmp, tmp);
        *dest = (uint8_t)(sqrt_uint32_approx(magsq) / 2);
        if (i > TB_CALL_MIN_FREQ_BIN && *dest > maxLevel) {
            maxLevel = *dest;
            if (*dest > TB_CALL_MIN_POWER) {
                maxLevelCount++;
            }
        }
        dest++;
    }
    if (callPointer != nullptr)
    {
        if (maxLevel > _currentCall->maxLevel)
        {
            _currentCall->maxLevel = maxLevel;
            if (_currentCall->highFreqSampleCount < 255 - maxLevelCount)
            {
                _currentCall->highFreqSampleCount += maxLevelCount;
            }
        }
        if (envelopeValue > TB_CALL_POWERCOUNTER_THRESHOLD)
        {
            _currentCall->highPowerSampleCount++;
        }
    }
}

void BatAudio::copyToCallBuffer(uint8_t *src)
{
    memcpy(_callBufferNextByte, src, TB_CALL_DATA_SIZE);
    increaseCallBuffer();
}

void BatAudio::increaseCallBuffer()
{
    _callBufferNextByte += TB_CALL_DATA_SIZE;
    if (_callBufferNextByte >= _callBufferEnd)
    {
        _callBufferNextByte = &_callBuffer[0];
    }
    _callBufferEntries++;
}

void BatAudio::AddAdditionalData(AdditionalDataType type, uint16_t data)
{
    uint8_t nextIndex = (_additionalDataHead + 1) % TB_ADDITIONAL_DATA_COUNT;
    if (nextIndex == _additionalDataTail)
    {
        // Buffer full.
        MESSAGE("Additional Data Buffer is full. Cannot add data.\n")
        _errDataBufferFull++;
        return;
    }
    _additionalData[nextIndex].timeMs = millis();
    _additionalData[nextIndex].type = type;
    WriteUInt16(_additionalData[nextIndex].data, data);
    _additionalData[nextIndex].data[2] = 0;

    _additionalDataHead = nextIndex;
}

void BatAudio::debug()
{
    if (_callPointerIndexHead == _callPointerIndexTail)
    {
        return;
    }

    uint8_t callPointer = (_callPointerIndexTail + 1) % TB_CALL_POINTER_COUNT;
    CallPointer *c = &_callPointers[callPointer];

    //Serial.println(F("A: "));
    //Serial.print(c->length);
    //Serial.print(F(" / "));
    //Serial.println(_callBufferEntries);
    uint8_t *data = c->startOfData;
    for (uint16_t b = 0; b < c->length; b++)
    {
        // Serial.print(F(": "));
        // Serial.print(x);
        // Serial.print(F(", "));
        // x = ((uint16_t)*(data+3)) << 8 | *(data+2);
        // Serial.println(x);
        // Serial.println();
        
        data += TB_CALL_DATA_SIZE;
        cli();
        _callBufferEntries--;
        if (data >= _callBufferEnd)
        {
            data = &_callBuffer[0];
        }
        _callBufferFirstByte = data;
        sei();
    }
    // Serial.print(F("B: "));
    // Serial.println(_callBufferEntries);
}

void BatAudio::sendOverUsb()
{
    if (_callPointerIndexHead == _callPointerIndexTail)
    {
        return;
    }

    cli();
    _callPointerIndexTail = (_callPointerIndexTail + 1) % TB_CALL_POINTER_COUNT;
    sei();
    CallPointer *c = &_callPointers[_callPointerIndexTail];

    uint8_t usbBuf[64];

    //Serial.println(c->length);

    uint8_t *data = c->startOfData;
    usbBuf[0] = 2;
    usbBuf[1] = 1;
    usbBuf[2] = (byte)(c->length >> 8);
    usbBuf[3] = (byte)(c->length);
    RawHID.send(usbBuf, 10);

    for (uint16_t b = 0; b < c->length; b++)
    {
        usbBuf[0] = 2;
        usbBuf[1] = 2;
        usbBuf[2] = (byte)(b >> 8);
        usbBuf[3] = (byte)(b);
        usbBuf[4] = data[0];
        usbBuf[5] = data[1];
        //usbBuf from 7 to 31 is reserved

        data += 4;
        uint32_t *s = (uint32_t *)(data);
        uint32_t *d = (uint32_t *)(usbBuf + 32);
        for (uint8_t i = 0; i < 32 / 4; i++)
        {
            *d = *s;
            s++;
            d++;
        }
        RawHID.send(usbBuf, 10);

        usbBuf[0] = 2;
        usbBuf[1] = 3;
        usbBuf[2] = (byte)(b >> 8);
        usbBuf[3] = (byte)(b);
        d = (uint32_t *)(usbBuf + 32);
        for (uint8_t i = 0; i < 32 / 4; i++)
        {
            *d = *s;
            s++;
            d++;
        }
        RawHID.send(usbBuf, 10);
        data += TB_CALL_DATA_SIZE - 4;

        cli();
        _callBufferEntries--;
        if (data >= _callBufferEnd)
        {
            data = &_callBuffer[0];
        }
        _callBufferFirstByte = data;
        sei();
    }
}

bool BatAudio::hasDataAvailable()
{
    return _callPointerIndexHead != _callPointerIndexTail;
}

bool BatAudio::writeToCard(uint16_t *blocksWritten, SdFat *sd, uint16_t blockCount, uint8_t *sdBuffer)
{
    /*
       Record (512 bytes):
        |  0-31  |  32-191   |  192-351  |  352-511  |
        | Header | FFT Block | FFT Block | FFT Block |

        Call Header:
        ||  0  |  1  |  2  |  3  ||  4  |  5  |  6  |  7  || 8-31 ||
        || xFF | xFE | # Blocks  ||      StartTimeMS      ||   0  ||

        Additional Header:
        ||  0  |  1  |  2  |  3  ||  4  |  5  |  6  |  7  || 8-31 ||
        || xFF | xFC |  0  |  0  ||  0  |  0  |  0  |  0  ||   0  ||

        FFT Block:
        ||  0  |  1  |  2  |  3  ||  4  |  5  |  6  |  7  || 8-31 | 32-159 ||
        || xFF | xFD |   Index   || Loudness  | SampleNr. ||   0  |   FFT  ||
    */

    Serial.println(_callBufferEntries);

    if (_callPointerIndexHead == _callPointerIndexTail)
    {
        return true;
    }

    uint8_t callPointer = (_callPointerIndexTail + 1) % TB_CALL_POINTER_COUNT;
    CallPointer *c = &_callPointers[callPointer];

    // if (c->highFreqSampleCount < 2 && c->highPowerSampleCount < 5)
    // {
    //     // Skip Call
    //     uint8_t *skip = c->startOfData;
    //     skip += c->length * TB_CALL_DATA_SIZE;
    //     if (skip >= _callBufferEnd) 
    //     {
    //         skip = &_callBuffer[0] + (skip - _callBufferEnd);
    //     }

    //     cli();
    //     _callBufferEntries -= c->length;
    //     _callBufferFirstByte = skip;
    //     _callPointerIndexTail = callPointer;
    //     sei();
    //     DEBUG_F("Skipped Call: MX: %hhu HC: %hhu HP: %hhu\n", c->maxLevel, c->highFreqSampleCount, c->highPowerSampleCount);
    //     DEBUG_F("X: %hu\n", _callBufferEntries);
    //     _errSkippedCalls++;
    //     return true;
    // }

    uint16_t blocksNeeded = c->length/3 + (c->length % 3 != 0);
    if (*blocksWritten + blocksNeeded > TB_FILE_BLOCK_COUNT){
        return false;
    }

    memset(sdBuffer, 0, TB_SD_BUFFER_SIZE);

    uint8_t *data = c->startOfData;
    sdBuffer[0] = 0xFF;
    sdBuffer[1] = 0xFE;
    WriteUInt16(sdBuffer + 2, c->length);

    uint32_t *d = (uint32_t *)(sdBuffer + 4);
    *d = c->startTime;

    sdBuffer[8] = _errCallBufferFull;
    _errCallBufferFull = 0;
    sdBuffer[9] = _errCallPointerBufferFull;
    _errCallPointerBufferFull = 0;
    sdBuffer[10] = _errDataBufferFull;
    _errDataBufferFull = 0;
    sdBuffer[11] = _errProcessOverlap;
    _errProcessOverlap = 0;

    sdBuffer[12] = c->highFreqSampleCount;
    sdBuffer[13] = c->highPowerSampleCount;
    sdBuffer[14] = c->maxLevel;
    // 15 is reserved

    WriteAdditionalDataToBuffer(sdBuffer + 16);
    WriteAdditionalDataToBuffer(sdBuffer + 24);

    // skip to end of header
    d = (uint32_t *)(sdBuffer + 32);
    for (uint16_t b = 0; b < c->length; b++)
    {
        if (b > 0 && b % 3 == 0)
        {
            sd->card()->writeData(sdBuffer);
            (*blocksWritten)++;
            memset(sdBuffer, 0, TB_SD_BUFFER_SIZE);
            sdBuffer[0] = 0xFF;
            sdBuffer[1] = 0xFC;

            WriteAdditionalDataToBuffer(sdBuffer + 8);
            WriteAdditionalDataToBuffer(sdBuffer + 16);
            WriteAdditionalDataToBuffer(sdBuffer + 24);

            d = (uint32_t *)(sdBuffer + 32);
        }

        // Marker (xFFFD) and Index
        // Beware of little endian!
        *d = 0x0000FDFF | ((uint32_t)b) << 16;
        d++;

        WriteAdditionalDataToBuffer((byte *)d);
        d += 2;
        WriteAdditionalDataToBuffer((byte *)d);
        d += 2;
        WriteAdditionalDataToBuffer((byte *)d);
        d += 2;

        uint32_t *s = (uint32_t *)(data);
        for (uint8_t i = 0; i < TB_CALL_DATA_SIZE / 4; i++)
        {
            *d = *s;
            s++;
            d++;
        }
        data += TB_CALL_DATA_SIZE;

        cli();
        _callBufferEntries--;
        if (data >= _callBufferEnd)
        {
            data = &_callBuffer[0];
        }
        _callBufferFirstByte = data;
        sei();
    }

    SD_ACTIVE_ON()
    sd->card()->writeData(sdBuffer);
    SD_ACTIVE_OFF()
    (*blocksWritten)++;
    cli();
    _callPointerIndexTail = callPointer;
    sei();
    return true;
}

void BatAudio::WriteAdditionalDataToBuffer(byte * buffer)
{
    if (_additionalDataTail == _additionalDataHead)
    {
        // Buffer empty
        return;
    }
    uint8_t index = (_additionalDataTail + 1) % TB_ADDITIONAL_DATA_COUNT;
    AdditionalData d = _additionalData[index];
    buffer[0] = d.type;
    WriteUInt32(buffer + 1, d.timeMs);
    buffer[5] = d.data[0];
    buffer[6] = d.data[1];
    buffer[7] = d.data[2];
    cli();
    _additionalDataTail = index;
    sei();
}
