#include "DMAChannel.h"
#include "arm_math.h"
#include "arm_const_structs.h"
#include <ADC.h>
#include "SdFat.h"

static const uint8_t TB_PIN_LED_GREEN = 6;
static const uint8_t TB_PIN_AUDIO = A9;
static const uint8_t TB_PIN_LED_YELLOW = 5;
static const uint8_t TB_PIN_SDCS = 10;
static const uint8_t TB_PIN_LED_RED = 4;

#define PDB_CONFIG (PDB_SC_TRGSEL(15) | PDB_SC_PDBEN | PDB_SC_CONT | PDB_SC_PDBIE | PDB_SC_DMAEN)
#define SAMPLE_BUF_SIZE 128
#define TB_FFT_SIZE 128
DMAMEM static uint16_t sampleBuf[SAMPLE_BUF_SIZE];
static uint16_t buf[TB_FFT_SIZE*2];

int buffIndex = 0;

static DMAChannel dma;

volatile int finished = 0;

SdFat _sd;
SdFile _file;

int16_t _complexBuffer[TB_FFT_SIZE*2] __attribute__((aligned(4)));
int16_t _outBuf[TB_FFT_SIZE];

int16_t maxValue = 0;
uint32_t maxIndex = 0;

uint8_t dataBlock1[512];
uint8_t dataBlock2[512];
uint8_t * currentBlock = dataBlock1;
uint8_t idx = 0;
uint32_t bgnBlock, endBlock;
#define BLOCK_COUNT 2048


int x = HIGH;
int y = 0;
int blockCount = 0;


//arm_cfft_radix4_instance_q15 _fft_inst;
const static arm_cfft_instance_q15 *S;

ADC *adc = new ADC(); // adc object


static void copy_to_fft_buffer(void *destination, const void *source)
{
  const uint16_t *src = (const uint16_t *)source;
  uint32_t *dst = (uint32_t *)destination;

  for (int i=0; i < TB_FFT_SIZE; i++) {
    *dst++ = *src++;  // real sample plus a zero for imaginary
  }
}


void isr(void)
{
  uint32_t daddr = (uint32_t)(dma.TCD->DADDR);
  dma.clearInterrupt();

  uint16_t *dst;
  const uint16_t *src, *end;
  
  if (daddr < (uint32_t)sampleBuf + sizeof(sampleBuf) / 2) {
    // DMA is receiving to the first half of the buffer
    // need to remove data from the second half
    src = (uint16_t *)&sampleBuf[SAMPLE_BUF_SIZE/2];
    dst = (uint16_t *)&buf[SAMPLE_BUF_SIZE/2];
    end = (uint16_t *)&sampleBuf[SAMPLE_BUF_SIZE];
    if (x == HIGH){
      x = LOW;
    } else {
      x = HIGH;
    }
    NVIC_SET_PENDING(IRQ_SOFTWARE);
    digitalWriteFast(TB_PIN_LED_YELLOW, x);
  } else {
    // DMA is receiving to the second half of the buffer
    // need to remove data from the first half
    src = (uint16_t *)&sampleBuf[0];
    dst = (uint16_t *)&buf[0];
    end = (uint16_t *)&sampleBuf[SAMPLE_BUF_SIZE/2];
    //digitalWriteFast(TB_PIN_LED_YELLOW, HIGH);
  }
  do {
    *dst++ = *src++;
  } while (src < end);
}

void writeBlock(){
    digitalWriteFast(TB_PIN_LED_RED, HIGH);

    if (blockCount == 0){
      if (!_sd.card()->writeStart(bgnBlock, BLOCK_COUNT)) {
        Serial.println("writeStart failed");
        return;
      }
    }
  
    if (!_sd.card()->writeData(currentBlock)) {
      Serial.println("write data failed");
      return;
    }
    blockCount++;

    if (blockCount >= BLOCK_COUNT){
      if (!_sd.card()->writeStop()) {
        Serial.println("writeStop failed");
        return;
      }
      _sd.remove("test.bin");
      _file.close();
      if (!_file.createContiguous("test.bin", 512 * BLOCK_COUNT)) {
        Serial.println("createContiguous failed");
        return;
      }
      if (!_file.contiguousRange(&bgnBlock, &endBlock)) {
        Serial.println("contiguousRange failed");
        return;
      }
      blockCount=0;
    }
    
    digitalWriteFast(TB_PIN_LED_RED, LOW);
}

void software_isr(void) 
{
    digitalWriteFast(TB_PIN_LED_GREEN, HIGH);
    copy_to_fft_buffer(_complexBuffer, buf);
    arm_cfft_q15(S, _complexBuffer, 0, 1);

    arm_cmplx_mag_q15(_complexBuffer, _outBuf, TB_FFT_SIZE);

    idx++;
    if (idx > 3){
      if (finished == 1){
        digitalWriteFast(A8, HIGH);
      } else {
        digitalWriteFast(A8, LOW);
      }
      idx = 0;
      if (currentBlock == dataBlock1){
        currentBlock = dataBlock2;
      } else {
        currentBlock = dataBlock2;
      }
      finished = 1;
    }
        
    digitalWriteFast(TB_PIN_LED_GREEN, LOW);
}

void setup() {
  pinMode(TB_PIN_AUDIO, INPUT);
  pinMode(TB_PIN_LED_GREEN, OUTPUT);
  pinMode(TB_PIN_LED_YELLOW, OUTPUT);
  pinMode(TB_PIN_LED_RED, OUTPUT);
  pinMode(A8, OUTPUT);
  pinMode(10, OUTPUT);
  Serial.begin(115200);
  while (!Serial) {
    
  }
  

  adc->setAveraging(4, ADC_0);
  adc->setResolution(12, ADC_0);
  adc->setConversionSpeed(ADC_CONVERSION_SPEED::VERY_HIGH_SPEED, ADC_0);
  adc->setSamplingSpeed(ADC_SAMPLING_SPEED::VERY_HIGH_SPEED, ADC_0);
  adc->adc0->startSingleRead(TB_PIN_AUDIO);
  
  S = &arm_cfft_sR_q15_len128;
  
  // set the programmable delay block to trigger the ADC at (120 CPU Clock, 60 Mhz Bus)
  // 60Mhz / 600 = 200kHz
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
  dma.begin(true);

  dma.TCD->SADDR = &ADC0_RA;
  dma.TCD->SOFF = 0;
  dma.TCD->ATTR = DMA_TCD_ATTR_SSIZE(1) | DMA_TCD_ATTR_DSIZE(1);
  dma.TCD->NBYTES_MLNO = 2;
  dma.TCD->SLAST = 0;
  dma.TCD->DADDR = sampleBuf;
  dma.TCD->DOFF = 2;
  dma.TCD->CITER_ELINKNO = sizeof(sampleBuf) / 2;
  dma.TCD->DLASTSGA = -sizeof(sampleBuf);
  dma.TCD->BITER_ELINKNO = sizeof(sampleBuf) / 2;
  dma.TCD->CSR = DMA_TCD_CSR_INTHALF | DMA_TCD_CSR_INTMAJOR;

  dma.triggerAtHardwareEvent(DMAMUX_SOURCE_ADC0);
  dma.enable();
  dma.attachInterrupt(isr);

  NVIC_SET_PRIORITY(IRQ_SOFTWARE, 208); // 255 = lowest priority
  NVIC_ENABLE_IRQ(IRQ_SOFTWARE);

  if (!_sd.begin(TB_PIN_SDCS, SPI_FULL_SPEED)) {
    _sd.initErrorPrint();
  }
  _sd.remove("test.bin");
  if (!_file.createContiguous("test.bin", 512UL * BLOCK_COUNT)) {
    Serial.println("createContiguous failed");
  }
  if (!_file.contiguousRange(&bgnBlock, &endBlock)) {
    Serial.println("contiguousRange failed");
  }
  Serial.println("Ok, file opens.");
  
  memset(dataBlock1, 'X', sizeof(dataBlock1));
  memset(dataBlock2, 'X', sizeof(dataBlock2));

}


void loop() {
  if (finished == 1){
    writeBlock();
    finished = 0;
  }
}




