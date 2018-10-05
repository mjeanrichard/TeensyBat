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
    digitalWriteFast(TB_PIN_LED_GREEN, HIGH);

    digitalWriteFast(TB_PIN_LED_RED, callBufferEntries >= CALL_BUFFER_COUNT-2);

    // For a new Call to start we need at least 3 free entries in the Buffer.
    if (!isCallInProgress && _lastEnvelopeValue > 500 && callBufferEntries < CALL_BUFFER_COUNT - 2)
    {

        uint8_t nextCallPointerIndex = (callPointerIndexHead + 1) % CALL_POINTER_COUNT;
        if (nextCallPointerIndex == callPointerIndexTail)
        {
            //Serial.println("Call Pointer Buffer Full!");
            digitalWriteFast(TB_PIN_LED_YELLOW, HIGH);
            digitalWriteFast(TB_PIN_LED_GREEN, LOW);
            return;
        }
        digitalWriteFast(TB_PIN_LED_YELLOW, LOW);
        isCallInProgress = true;
        currentCall = &callPointers[nextCallPointerIndex];
        currentCall->startOfData = callBufferNextByte;
        currentCall->startTime = millis();
        currentCall->length = 0;
        

        // new call started, get last two calls from pre call buffer
        uint8_t pi = (preCallBufferIndex + PRE_CALL_BUFFER_COUNT - 2) % PRE_CALL_BUFFER_COUNT;
        copyToCallBuffer(&preCallBuffer[pi * CALL_DATA_SIZE]);
        currentCall->length++;

        pi = (pi + 1) % PRE_CALL_BUFFER_COUNT;
        copyToCallBuffer(&preCallBuffer[pi * CALL_DATA_SIZE]);
        currentCall->length++;

        // add the current call
        computeFFT(callBufferNextByte);
        increaseCallBuffer();
        currentCall->length++;
    }
    else if (isCallInProgress && _lastEnvelopeValue > 100 && callBufferEntries < CALL_BUFFER_COUNT)
    {
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
    }

    digitalWriteFast(TB_PIN_LED_GREEN, LOW);
}

void BatAudio::computeFFT(uint8_t * dest)
{
    *(uint16_t *)(dest) = _lastEnvelopeValue;
    dest += 4;
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

