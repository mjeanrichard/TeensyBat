#ifndef HardFault_h
#define HardFault_h

#include <unistd.h>
#include "usb_serial.h"
#include "core_pins.h"
#include "elapsedMillis.h"

#include "Config.h"

#define SCB_SHCSR_USGFAULTENA (uint32_t)1<<18
#define SCB_SHCSR_BUSFAULTENA (uint32_t)1<<17
#define SCB_SHCSR_MEMFAULTENA (uint32_t)1<<16

#define SCB_SHPR1_USGFAULTPRI *(volatile uint8_t *)0xE000ED20
#define SCB_SHPR1_BUSFAULTPRI *(volatile uint8_t *)0xE000ED19
#define SCB_SHPR1_MEMFAULTPRI *(volatile uint8_t *)0xE000ED18

#define SCnSCB_ACTLR  (*(volatile uint32_t *)0xE000E008)


extern "C" {
void __attribute__((naked)) hard_fault_isr(void){
    uint32_t* sp=0;
    // this is from "Definitive Guide to the Cortex M3" pg 423
    asm volatile ( "TST LR, #0x4\n\t"   // Test EXC_RETURN number in LR bit 2
                   "ITE EQ\n\t"         // if zero (equal) then
                   "MRSEQ %0, MSP\n\t"  //   Main Stack was used, put MSP in sp
                   "MRSNE %0, PSP\n\t"  // else Process stack was used, put PSP in sp
           : "=r" (sp) : : "cc");

    uint32_t hfsr = SCB_HFSR;
    uint32_t cfsr = SCB_CFSR;
    uint32_t dfsr = SCB_DFSR;
    uint32_t bfar = SCB_BFAR;

    // allow USB interrupts to preempt us:
    SCB_SHPR1_BUSFAULTPRI = (uint8_t)255;
    SCB_SHPR1_USGFAULTPRI = (uint8_t)255;
    SCB_SHPR1_MEMFAULTPRI = (uint8_t)255;

    Serial.println("!!!! Hard Fault !!!!");
    Serial.print("pc=0x");
    Serial.print(sp[6], 16);
    Serial.print(", lr=0x");
    Serial.print(sp[5], 16);
    Serial.println();

    Serial.print("HFSR: 0x");
    Serial.print(hfsr, 16);
    Serial.println();

    Serial.print("CFSR: 0x");
    Serial.print(cfsr, 16);
    Serial.println();

    Serial.print("DFSR: 0x");
    Serial.print(dfsr, 16);
    Serial.println();

    Serial.print("BFAR: 0x");
    Serial.print(bfar, 16);
    Serial.println();

    Serial.flush();

    uint32_t m = micros();
    uint16_t cnt = 0;
	while (1) {
        if (micros() - m > 500){
            if (cnt > 250) {
                cnt = 0;
            } else {
                cnt++;
            }
            m = micros();
        }

        if (cnt > 125)
        {
            digitalWrite(TB_PIN_LED_RED, LOW);
        } 
        else 
        {
            digitalWrite(TB_PIN_LED_RED, HIGH);
        }

		// keep polling some communication while in fault
		// mode, so we don't completely die.
		if (SIM_SCGC4 & SIM_SCGC4_USBOTG) usb_isr();
		if (SIM_SCGC4 & SIM_SCGC4_UART0) uart0_status_isr();
		if (SIM_SCGC4 & SIM_SCGC4_UART1) uart1_status_isr();
		if (SIM_SCGC4 & SIM_SCGC4_UART2) uart2_status_isr();
	}
}
}

#endif