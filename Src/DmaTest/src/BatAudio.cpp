#include "BatAudio.h"

DMAChannel BatAudio::_dma(false);
DMAMEM static uint16_t dmaSampleBuffer[TB_AUDIO_SAMPLE_BUFFER_SIZE];

ADC BatAudio::_adc;
BatAudio *BatAudio::_instance;

#define PDB_CONFIG (PDB_SC_TRGSEL(15) | PDB_SC_PDBEN | PDB_SC_CONT | PDB_SC_PDBIE | PDB_SC_DMAEN)

void BatAudio::init()
{
    _adc.setAveraging(4, ADC_0);
    _adc.setResolution(12, ADC_0);
    _adc.setConversionSpeed(ADC_CONVERSION_SPEED::VERY_HIGH_SPEED, ADC_0);
    _adc.setSamplingSpeed(ADC_SAMPLING_SPEED::VERY_HIGH_SPEED, ADC_0);
    _adc.setReference(ADC_REFERENCE::REF_3V3, ADC_0);
    _adc.adc0->startSingleRead(TB_PIN_AUDIO);

    _adc.setAveraging(4, ADC_1);
    _adc.setResolution(12, ADC_1);
    _adc.setConversionSpeed(ADC_CONVERSION_SPEED::MED_SPEED, ADC_1);
    _adc.setSamplingSpeed(ADC_SAMPLING_SPEED::MED_SPEED, ADC_1);
    _adc.setReference(ADC_REFERENCE::REF_3V3, ADC_1);
    _adc.adc1->startSingleRead(TB_PIN_ENVELOPE);
    _adc.enableInterrupts(ADC_1);

    // set the programmable delay block to trigger the ADC at (120 CPU Clock, 60 Mhz Bus)
    // 60Mhz / 600 = 100kHz
    // 60Mhz / 234 = 256.4kHz
    SIM_SCGC6 |= SIM_SCGC6_PDB;
    PDB0_IDLY = 1;
    PDB0_MOD = 234 - 1;
    PDB0_SC = PDB_CONFIG | PDB_SC_LDOK;
    PDB0_SC = PDB_CONFIG | PDB_SC_SWTRIG;
    PDB0_CH0C1 = 0x0101;

    // enable the ADC for hardware trigger and DMA
    ADC0_SC2 |= ADC_SC2_ADTRG | ADC_SC2_DMAEN;

    // set up a DMA channel to store the ADC data
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

    NVIC_SET_PRIORITY(IRQ_SOFTWARE, 208); // 255 = lowest priority
    NVIC_ENABLE_IRQ(IRQ_SOFTWARE);
}

void BatAudio::start()
{
    _isEnabled = true;
    _dma.enable();
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
        _adc.disableInterrupts(ADC_1);
        int16_t value = _adc.adc1->analogRead(TB_PIN_BATTERY);
        _adc.enableInterrupts(ADC_1);
        return value;
    }
    else
    {
        return -1;
    }
}

int16_t BatAudio::readTempC()
{
    if (!_isEnabled)
    {
        _adc.disableInterrupts(ADC_1);
        int adcVal = _adc.adc1->analogRead(ADC_INTERNAL_SOURCE::TEMP_SENSOR);
        if (adcVal < 0)
        {
            return adcVal;
        }
        uint16_t tempC = (1819615 - 1924 * adcVal) >> 12;
        _adc.enableInterrupts(ADC_1);
        return tempC;
    }
    else
    {
        return -2000;
    }
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
            _instance->rotateSamplingBuffer();
            NVIC_SET_PENDING(IRQ_SOFTWARE);
            digitalWriteFast(TB_PIN_LED_RED, LOW);
        }
        else
        {
            digitalWriteFast(TB_PIN_LED_RED, HIGH);
        }
        digitalWriteFast(TB_PIN_LED_YELLOW, LOW);
    }
    else
    {
        // DMA is receiving to the second half of the buffer
        // need to remove data from the first half
        src = (uint16_t *)dmaSampleBuffer;
        dst = (uint16_t *)_instance->_currentSamplingBuffer;
        end = (uint16_t *)&dmaSampleBuffer[TB_AUDIO_SAMPLE_BUFFER_SIZE / 2];
        digitalWriteFast(TB_PIN_LED_YELLOW, HIGH);

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
    digitalWriteFast(TB_PIN_LED_RED, _callBufferEntries >= TB_CALL_BUFFER_COUNT - 2);
    digitalWriteFast(TB_PIN_LED_GREEN, HIGH);

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
    if (!_isCallInProgress && _lastEnvelopeValue > TB_CALL_START_THRESHOLD && _callBufferEntries < TB_CALL_BUFFER_COUNT - 2)
    {
        uint8_t nextCallPointerIndex = (_callPointerIndexHead + 1) % TB_CALL_POINTER_COUNT;
        if (nextCallPointerIndex == _callPointerIndexTail)
        {
            Serial.println(F("Call Pointer Buffer Full!"));
            digitalWriteFast(TB_PIN_LED_GREEN, LOW);
            return;
        }
        _isCallInProgress = true;
        _currentCall = &_callPointers[nextCallPointerIndex];
        _currentCall->startOfData = _callBufferNextByte;
        _currentCall->startTime = millis();
        _currentCall->length = 0;

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
            copyToCallBuffer(&_preCallBuffer[((p + i) % 4) * TB_CALL_DATA_SIZE]);
            _currentCall->length++;
        }
        _preCallBufferCount = 0;

        // add the current call
        computeFFT(_callBufferNextByte);
        increaseCallBuffer();
        _currentCall->length++;
    }
    else if (_isCallInProgress                                     // Continue recording if there is a call already in progress
             && (_lastEnvelopeValue > TB_CALL_STOP_THRESHOLD       // and sound level high enough
                 || _afterCallSampleCount < TB_AFTER_CALL_SAMPLES) // or not enough "after" samples collected yet
             && _callBufferEntries < TB_CALL_BUFFER_COUNT          // and enough memory available
             && _currentCall->length < 200)                        // Max Call length not yet reached
    {
        if (_lastEnvelopeValue <= TB_CALL_STOP_THRESHOLD)
        {
            _afterCallSampleCount++;
        }
        else if (_lastEnvelopeValue > TB_CALL_START_THRESHOLD)
        {
            _afterCallSampleCount = 0;
        }
        // call in progress, record FFT
        computeFFT(_callBufferNextByte);
        increaseCallBuffer();
        _currentCall->length++;
    }
    else if (_isCallInProgress)
    {
        // Either no more space in buffer or call has ended
        // add the last call to the buffer if there is enough space
        if (_callBufferEntries < TB_CALL_BUFFER_COUNT)
        {
            computeFFT(_callBufferNextByte);
            increaseCallBuffer();
            _currentCall->length++;
        }

        _callPointerIndexHead = (_callPointerIndexHead + 1) % TB_CALL_POINTER_COUNT;
        _isCallInProgress = false;
        _currentCall = nullptr;
    }
    else
    {
        // No call yet -> buffer in case we need it later
        computeFFT(&_preCallBuffer[_preCallBufferIndex * TB_CALL_DATA_SIZE]);
        _preCallBufferIndex++;
        if (_preCallBufferIndex >= TB_PRE_CALL_BUFFER_COUNT)
        {
            _preCallBufferIndex = 0;
        }
        if (_preCallBufferCount < TB_PRE_CALL_BUFFER_COUNT)
        {
            _preCallBufferCount++;
        }
    }
    digitalWriteFast(TB_PIN_LED_GREEN, LOW);
}

void BatAudio::computeFFT(uint8_t *dest)
{
    *dest = (byte)(_lastEnvelopeValue);
    dest++;
    *dest = (byte)(_lastEnvelopeValue >> 8);
    dest++;
    *dest = (byte)(_sampleCounter);
    dest++;
    *dest = (byte)(_sampleCounter >> 8);
    dest++;

    arm_cfft_q15(&arm_cfft_sR_q15_len256, (q15_t *)_fftBuffer, 0, 1);
    for (int i = 0; i < TB_FFT_RESULT_SIZE; i++)
    {
        uint32_t tmp = *((uint32_t *)_fftBuffer + i); // real & imag
        uint32_t magsq = multiply_16tx16t_add_16bx16b(tmp, tmp);
        *dest = (uint8_t)(sqrt_uint32_approx(magsq) / 2);
        dest++;
    }
}

void BatAudio::copyToCallBuffer(uint8_t *src)
{
    memcpy(src, _callBufferNextByte, TB_CALL_DATA_SIZE);
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

void BatAudio::debug()
{
    if (_callPointerIndexHead == _callPointerIndexTail)
    {
        return;
    }

    cli();
    _callPointerIndexTail = (_callPointerIndexTail + 1) % TB_CALL_POINTER_COUNT;
    sei();
    CallPointer *c = &_callPointers[_callPointerIndexTail];

    //Serial.print(F("A: "));
    //Serial.print(c->length);
    //Serial.print(F(" / "));
    //Serial.println(_callBufferEntries);
    uint8_t *data = c->startOfData;
    for (uint16_t b = 0; b < c->length; b++)
    {
        //Serial.print(*(uint16_t *)(data));
        //Serial.print(F(","));
        data += 4;
        for (uint8_t i = 0; i < TB_CALL_DATA_SIZE - 4; i++)
        {
            //Serial.print(*data);
            //Serial.print(F(","));
            data++;
        }
        //Serial.println();
        cli();
        _callBufferEntries--;
        if (data >= _callBufferEnd)
        {
            data = &_callBuffer[0];
        }
        _callBufferFirstByte = data;
        sei();
    }
    //Serial.print(F("B: "));
    //Serial.println(_callBufferEntries);
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

    Serial.println(c->length);

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

void BatAudio::writeToCard(uint16_t *blocksWritten, SdFat *sd, uint16_t blockCount, uint8_t *sdBuffer)
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

    if (_callPointerIndexHead == _callPointerIndexTail)
    {
        return;
    }
    //digitalWriteFast(TB_PIN_LED_GREEN, HIGH);

    cli();
    _callPointerIndexTail = (_callPointerIndexTail + 1) % TB_CALL_POINTER_COUNT;
    sei();
    CallPointer *c = &_callPointers[_callPointerIndexTail];

    memset(sdBuffer, 0, TB_SD_BUFFER_SIZE);

    uint8_t *data = c->startOfData;
    sdBuffer[0] = 0xFF;
    sdBuffer[1] = 0xFE;
    sdBuffer[2] = (byte)(c->length);
    sdBuffer[3] = (byte)(c->length >> 8);

    uint32_t *d = (uint32_t *)(sdBuffer + 4);
    *d = c->startTime;

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
            d = (uint32_t *)(sdBuffer + 32);
        }

        // Marker (xFFFD) and Index
        // Beware of little endian!
        *d = 0x0000FDFF | ((uint32_t)b) << 16;
        d++;

        // Skip 24 reserved bytes
        d += 6;

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
    sd->card()->writeData(sdBuffer);
    (*blocksWritten)++;
    Serial.println(*blocksWritten);
    //digitalWriteFast(TB_PIN_LED_GREEN, LOW);
}
