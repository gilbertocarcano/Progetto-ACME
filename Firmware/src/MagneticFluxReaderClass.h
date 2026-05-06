#ifndef MAGNETIC_FLUX_READER_CLASS_H
#define MAGNETIC_FLUX_READER_CLASS_H

#include <Arduino.h>
#include <vector>

class MagneticFluxReaderClass {
public:

    struct Sample {       
        float left;    // valore B convertito
        float right;
        float up;
        float down;
        float center;
    };

    MagneticFluxReaderClass();

    void begin(uint8_t pinLeft, uint8_t pinRight, uint8_t pinUp, uint8_t pinDown, uint8_t pinCenter);

    // Legge i 5 sensori, converte in B, accoda nel buffer
    Sample read(int readingTimeMs);

    // Legge le tensioni fornite dai 5 sensori
    Sample readVref(int readingTimeMs);

    const std::vector<Sample>& getBuffer() const;
    void clearBuffer();

private:
    bool _initialized;

    uint8_t _pinLeft;
    uint8_t _pinRight;
    uint8_t _pinUp;
    uint8_t _pinDown;
    uint8_t _pinCenter;

    std::vector<Sample> _buffer;

    // --- FUNZIONE PRIVATA DI CONVERSIONE ---
    float readSensor(uint8_t pin, float Vzero, float sensitivity, int readingTimeMs);      // analogRead + conversione ADC → B
    float readSensorVref(uint8_t pin, int readingTimeMs);  // analogRead + conversione ADC → Vref
    int readAnalogTrimmed(uint8_t pin, int nSamples, int cutPercent, int delayMicros);
};

extern MagneticFluxReaderClass MagneticFluxReader;

#endif
