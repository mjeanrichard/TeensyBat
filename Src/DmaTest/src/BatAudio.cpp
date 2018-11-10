#include "BatAudio.h"

DMAChannel BatAudio::_dma(false);
DMAMEM static uint16_t dmaSampleBuffer[AUDIO_SAMPLE_BUFFER_SIZE];

// int16_t BatAudio::_fftBuffer[AUDIO_SAMPLE_BUFFER_SIZE*2] __attribute__((aligned(4)));
// int16_t BatAudio::_outBuf[AUDIO_SAMPLE_BUFFER_SIZE];

// const arm_cfft_instance_q15 * BatAudio::_cfftData;

volatile bool busy = false;

BatAudio * BatAudio::_self;


uint8_t preCallBuffer[CALL_DATA_SIZE * PRE_CALL_BUFFER_COUNT];
uint8_t preCallBufferIndex = 0;
uint8_t preCallBufferCount = 0;
uint8_t afterCallSampleCount = 0;
uint16_t sampleCounter = 0;

// Definition for the Call Ring Buffer
static uint8_t callBuffer[CALL_DATA_SIZE * CALL_BUFFER_COUNT];
static const uint8_t * callBufferEnd = &callBuffer[CALL_DATA_SIZE * CALL_BUFFER_COUNT - 1];

static uint8_t * callBufferNextByte = &callBuffer[0];
static uint8_t * callBufferFirstByte = &callBuffer[0];
static volatile uint8_t isCallInProgress = false;
static volatile uint16_t callBufferEntries = 0;

static CallPointer * currentCall;

static CallPointer callPointers[CALL_POINTER_COUNT];
static volatile uint8_t callPointerIndexHead = 0;
static volatile uint8_t callPointerIndexTail = 0;

#define PDB_CONFIG (PDB_SC_TRGSEL(15) | PDB_SC_PDBEN | PDB_SC_CONT | PDB_SC_PDBIE | PDB_SC_DMAEN)

void BatAudio::init()
{
    _cfftData = &arm_cfft_sR_q15_len128;

    _adc->setAveraging(4, ADC_0);
    _adc->setResolution(12, ADC_0);
    _adc->setConversionSpeed(ADC_CONVERSION_SPEED::VERY_HIGH_SPEED, ADC_0);
    _adc->setSamplingSpeed(ADC_SAMPLING_SPEED::VERY_HIGH_SPEED, ADC_0);
    _adc->setReference(ADC_REFERENCE::REF_3V3, ADC_0);
    _adc->adc0->startSingleRead(TB_PIN_AUDIO);

    _adc->setAveraging(4, ADC_1);
    _adc->setResolution(12, ADC_1);
    _adc->setConversionSpeed(ADC_CONVERSION_SPEED::MED_SPEED, ADC_1);
    _adc->setSamplingSpeed(ADC_SAMPLING_SPEED::MED_SPEED, ADC_1);
    _adc->setReference(ADC_REFERENCE::REF_3V3, ADC_1);
    _adc->adc1->startSingleRead(TB_PIN_ENVELOPE);
	_adc->enableInterrupts(ADC_1);

    // set the programmable delay block to trigger the ADC at (120 CPU Clock, 60 Mhz Bus)
    // 60Mhz / 600 = 100kHz
    // 60Mhz / 234 = 256.4kHz
    SIM_SCGC6 |= SIM_SCGC6_PDB;
    PDB0_IDLY = 1;
    PDB0_MOD = 234-1;
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
    _dma.enable();
    _dma.attachInterrupt(BatAudio::adc0_isr);

    NVIC_SET_PRIORITY(IRQ_SOFTWARE, 208); // 255 = lowest priority
    NVIC_ENABLE_IRQ(IRQ_SOFTWARE);
}

void BatAudio::adc0_isr()
{
    uint32_t daddr = (uint32_t)(_dma.TCD->DADDR);
    _dma.clearInterrupt();

    _self->_adc->adc1->startSingleRead(TB_PIN_ENVELOPE);

    uint32_t *dst;
    const uint16_t *src, *end;

    if (daddr < (uint32_t)dmaSampleBuffer + sizeof(dmaSampleBuffer) / 2) {
        // DMA is receiving to the first half of the buffer
        // need to remove data from the second half
        src = (uint16_t *)&dmaSampleBuffer[FFT_RESULT_SIZE];
        dst = (uint32_t *)&(BatAudio::_self->_fftBuffer[AUDIO_SAMPLE_BUFFER_SIZE]);
        end = (uint16_t *)&dmaSampleBuffer[AUDIO_SAMPLE_BUFFER_SIZE];

        if (!busy){
            NVIC_SET_PENDING(IRQ_SOFTWARE);
            digitalWriteFast(TB_PIN_LED_RED, LOW);
        } else {
            digitalWriteFast(TB_PIN_LED_RED, HIGH);
        }

        //digitalWriteFast(TB_PIN_LED_YELLOW, LOW);
    } else {
        // DMA is receiving to the second half of the buffer
        // need to remove data from the first half
        src = (uint16_t *)&dmaSampleBuffer[0];
        dst = (uint32_t *)&(BatAudio::_self->_fftBuffer[0]);
        end = (uint16_t *)&dmaSampleBuffer[AUDIO_SAMPLE_BUFFER_SIZE/2];
        //digitalWriteFast(TB_PIN_LED_YELLOW, HIGH);
    }

    do {
        *dst++ = *src++;
    } while (src < end);
}

void adc1_isr(void)
{
    BatAudio::_self->_lastEnvelopeValue = BatAudio::_self->_adc->adc1->readSingle();
}

void software_isr(void)
{
    busy = true;
    BatAudio::_self->sample_complete_isr();
    busy = false;
}

void BatAudio::sample_complete_isr()
{
    digitalWriteFast(TB_PIN_LED_RED, callBufferEntries >= CALL_BUFFER_COUNT-2);
    digitalWriteFast(TB_PIN_LED_YELLOW, HIGH);

    sampleCounter++;

    // For a new Call to start we need at least 3 free entries in the Buffer.
    if (!isCallInProgress && _lastEnvelopeValue > CALL_START_THRESHOLD && callBufferEntries < CALL_BUFFER_COUNT - 2)
    {

        uint8_t nextCallPointerIndex = (callPointerIndexHead + 1) % CALL_POINTER_COUNT;
        if (nextCallPointerIndex == callPointerIndexTail)
        {
            //Serial.println("Call Pointer Buffer Full!");
            //digitalWriteFast(TB_PIN_LED_YELLOW, HIGH);
            digitalWriteFast(TB_PIN_LED_YELLOW, LOW);
            return;
        }
        //digitalWriteFast(TB_PIN_LED_YELLOW, LOW);
        isCallInProgress = true;
        currentCall = &callPointers[nextCallPointerIndex];
        currentCall->startOfData = callBufferNextByte;
        currentCall->startTime = millis();
        currentCall->length = 0;
        
        afterCallSampleCount = 0;

        // new call started, get calls from pre call buffer
        // preCallBufferCount will be equal to PRE_CALL_BUFFER_COUNT in most cases
        // except if the buffer has not yet been filled after a call ended.
        // We need to get the oldest item in the buffer first. If the Buffer is full, this is
        // the current item, if not, this is the preCallBufferIndex - preCallBufferCount.
        // Add PRE_CALL_BUFFER_COUNT to ensure that modulus is positive
        uint8_t p = preCallBufferIndex + PRE_CALL_BUFFER_COUNT - preCallBufferCount;
        for(int i=0; i < preCallBufferCount;i++){
            copyToCallBuffer(&preCallBuffer[((p + i) % 4) * CALL_DATA_SIZE]);
            currentCall->length++;
        }

        // add the current call
        computeFFT(callBufferNextByte);
        increaseCallBuffer();
        currentCall->length++;
    }
    else if (isCallInProgress && (_lastEnvelopeValue > CALL_STOP_THRESHOLD || afterCallSampleCount < AFTER_CALL_SAMPLES) && callBufferEntries < CALL_BUFFER_COUNT)
    {
        if (_lastEnvelopeValue <= CALL_STOP_THRESHOLD)
        {
            afterCallSampleCount++;
        }
        else if (_lastEnvelopeValue > CALL_START_THRESHOLD) 
        {
            afterCallSampleCount = 0;
        }
        // call in progress, record FFT
        computeFFT(callBufferNextByte);
        increaseCallBuffer();
        currentCall->length++;
    }
    else if (isCallInProgress)
    {
        // Either no more space in buffer or call has ended
        // add the last call to the buffer if there is enough space
        if (callBufferEntries < CALL_BUFFER_COUNT)
        {
            computeFFT(callBufferNextByte);
            increaseCallBuffer();
            currentCall->length++;
        }

        callPointerIndexHead = (callPointerIndexHead + 1) % CALL_POINTER_COUNT;
        isCallInProgress = false;
        currentCall = nullptr;
    } else {
        // No call yet -> buffer in case we need it later
        computeFFT(&preCallBuffer[preCallBufferIndex * CALL_DATA_SIZE]);
        preCallBufferIndex++;
        if (preCallBufferIndex >= PRE_CALL_BUFFER_COUNT){
            preCallBufferIndex = 0;
        }
        if (preCallBufferCount < PRE_CALL_BUFFER_COUNT){
            preCallBufferCount++;
        }
    }
    digitalWriteFast(TB_PIN_LED_YELLOW, LOW);
}

void BatAudio::computeFFT(uint8_t * dest)
{
	*dest = (byte)(_lastEnvelopeValue);
    dest++;
	*dest = (byte)(_lastEnvelopeValue>>8);
    dest++;
    *dest = (byte)(sampleCounter);
    dest++;
    *dest = (byte)(sampleCounter>>8);
    dest++;
   
    arm_cfft_q15(&arm_cfft_sR_q15_len128, (q15_t *)_fftBuffer, 0, 1);
    for (int i=0; i < FFT_RESULT_SIZE; i++) {
        uint32_t tmp = *((uint32_t *)_fftBuffer + i); // real & imag
        uint32_t magsq = multiply_16tx16t_add_16bx16b(tmp, tmp);
        *dest = (uint8_t)(sqrt_uint32_approx(magsq) / 2);
        dest++;
    }
}

void BatAudio::copyToCallBuffer(uint8_t * src){
    uint32_t * s = (uint32_t*)src;
    uint32_t * d = (uint32_t*)callBufferNextByte;
    for(uint8_t i=0;i<CALL_DATA_SIZE/4;i++){
      *d = *s;
      s++;
      d++;
    }
    increaseCallBuffer();
}

void BatAudio::increaseCallBuffer()
{
    callBufferNextByte += CALL_DATA_SIZE;
    if (callBufferNextByte >= callBufferEnd)
    {
        callBufferNextByte = &callBuffer[0];
    }
    callBufferEntries++;
}

void BatAudio::debug()
{
    if (callPointerIndexHead == callPointerIndexTail){
        return;
    }

    cli();
    callPointerIndexTail = (callPointerIndexTail + 1) % CALL_POINTER_COUNT;
    sei();
    CallPointer * c = &callPointers[callPointerIndexTail];

    Serial.print("A: ");
    Serial.print(c->length);
    Serial.print(" / ");
    Serial.println(callBufferEntries);
    uint8_t * data = c->startOfData;
    for (uint16_t b = 0; b < c->length; b++)
    {
        Serial.print(*(uint16_t *)(data));
        Serial.print(",");
        data += 4;
        for(uint8_t i = 0; i < CALL_DATA_SIZE-4; i++)
        {
            Serial.print(*data);
            Serial.print(",");
            data++;
        }
        Serial.println();
        cli();
        callBufferEntries--;
        if (data >= callBufferEnd){
            data = &callBuffer[0];
        }
        callBufferFirstByte = data;
        sei();
    }
    Serial.print("B: ");
    Serial.println(callBufferEntries);
}


void BatAudio::sendOverUsb()
{
    if (callPointerIndexHead == callPointerIndexTail){
        return;
    }

    cli();
    callPointerIndexTail = (callPointerIndexTail + 1) % CALL_POINTER_COUNT;
    sei();
    CallPointer * c = &callPointers[callPointerIndexTail];

    uint8_t usbBuf[64];

    Serial.println(c->length);
    
    uint8_t * data = c->startOfData;
    usbBuf[0] = 2;
    usbBuf[1] = 1;
	usbBuf[2] = (byte)(c->length>>8);
	usbBuf[3] = (byte)(c->length);
    RawHID.send(usbBuf, 10);

    for (uint16_t b = 0; b < c->length; b++)
    {
        usbBuf[0] = 2;
        usbBuf[1] = 2;
        usbBuf[2] = (byte)(b>>8);
        usbBuf[3] = (byte)(b);
        usbBuf[4] = data[0];
        usbBuf[5] = data[1];
        //usbBuf from 7 to 31 is reserved

        data += 4;
        uint32_t * s = (uint32_t*)(data);
        uint32_t * d = (uint32_t*)(usbBuf+32);
        for(uint8_t i=0;i<32/4;i++){
            *d = *s;
            s++;
            d++;
        }
        RawHID.send(usbBuf, 10);

        usbBuf[0] = 2;
        usbBuf[1] = 3;
        usbBuf[2] = (byte)(b>>8);
        usbBuf[3] = (byte)(b);
        d = (uint32_t*)(usbBuf+32);
        for(uint8_t i=0;i<32/4;i++){
            *d = *s;
            s++;
            d++;
        }
        RawHID.send(usbBuf, 10);
        data += CALL_DATA_SIZE-4;

        cli();
        callBufferEntries--;
        if (data >= callBufferEnd){
            data = &callBuffer[0];
        }
        callBufferFirstByte = data;
        sei();
    }
}

bool BatAudio::hasDataAvailable()
{
    return callPointerIndexHead != callPointerIndexTail;
}

void BatAudio::writeToCard(uint16_t * blocksWritten, SdFat * sd, uint16_t blockCount)
{
    /*
       Record (512 bytes):
        |  0-7   |    8-79   |   80-151  |  152-223  |  224-295  |  296-367  |  368-439  |  440-511  |
        | Header | FFT Block | FFT Block | FFT Block | FFT Block | FFT Block | FFT Block | FFT Block |

        Call Header:
        ||  0  |  1  |  2  |  3  ||  4  |  5  |  6  |  7  ||
        ||  0  |  1  |  2  |  3  ||  4  |  5  |  6  |  7  ||
        || xFF | xFE | # Blocks  ||      StartTimeMS      ||

        Additional Header:
        ||  0  |  1  |  2  |  3  ||  4  |  5  |  6  |  7  ||
        || xFF | xFC |  0  |  0  ||  0  |  0  |  0  |  0  ||

        FFT Block:
        ||  0  |  1  |  2  |  3  ||  4  |  5  |  6  |  7  || 8-71 ||
        || xFF | xFD |   Index   || Loudness  | SampleNr. || FFT  ||
    */

    if (callPointerIndexHead == callPointerIndexTail){
        return;
    }
digitalWriteFast(TB_PIN_LED_GREEN, HIGH);

    cli();
    callPointerIndexTail = (callPointerIndexTail + 1) % CALL_POINTER_COUNT;
    sei();
    CallPointer * c = &callPointers[callPointerIndexTail];

    uint8_t sdBuf[512];
    memset(sdBuf, 0, sizeof(sdBuf));

    Serial.println(c->length);
    
    // 36 Bytes Header and then 7 * 68 byte call data

    uint8_t * data = c->startOfData;
    sdBuf[0] = 0xFF;
    sdBuf[1] = 0xFE;
	sdBuf[2] = (byte)(c->length);
	sdBuf[3] = (byte)(c->length>>8);

    uint32_t * d = (uint32_t*)(sdBuf + 4);
    *d = c->startTime;
    d++;

    for (uint16_t b = 0; b < c->length; b++)
    {
        if (b > 0 && b % 7 == 0)
        {
            sd->card()->writeData(sdBuf);
            (*blocksWritten)++;
            memset(sdBuf, 0, sizeof(sdBuf));
            sdBuf[0] = 0xFF;
            sdBuf[1] = 0xFC;
            d = (uint32_t*)(sdBuf + 8);
        }

        // Beware of little endian!
        *d = 0x0000FDFF | ((uint32_t)b)<<16;
        d++;

        uint32_t * s = (uint32_t*)(data);
        for(uint8_t i=0;i<CALL_DATA_SIZE/4;i++){
            *d = *s;
            s++;
            d++;
        }
        data += CALL_DATA_SIZE;

        cli();
        callBufferEntries--;
        if (data >= callBufferEnd){
            data = &callBuffer[0];
        }
        callBufferFirstByte = data;
        sei();
    }
    sd->card()->writeData(sdBuf);
    (*blocksWritten)++;
    digitalWriteFast(TB_PIN_LED_GREEN, LOW);

}
