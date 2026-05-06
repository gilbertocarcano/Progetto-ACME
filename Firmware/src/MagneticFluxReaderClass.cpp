#include "MagneticFluxReaderClass.h"
#include "ConfigurationClass.h"
#include <algorithm>  

MagneticFluxReaderClass MagneticFluxReader;

MagneticFluxReaderClass::MagneticFluxReaderClass()
{
    _initialized = false;

    _pinLeft   = 255;
    _pinRight  = 255;
    _pinUp     = 255;
    _pinDown   = 255;
    _pinCenter = 255;
}

void MagneticFluxReaderClass::begin(uint8_t pinLeft, uint8_t pinRight, uint8_t pinUp, uint8_t pinDown, uint8_t pinCenter) {
    _pinLeft   = pinLeft;
    _pinRight  = pinRight;
    _pinUp     = pinUp;
    _pinDown   = pinDown;
    _pinCenter = pinCenter;

    pinMode(_pinLeft,   INPUT);
    pinMode(_pinRight,  INPUT);
    pinMode(_pinUp,     INPUT);
    pinMode(_pinDown,   INPUT);
    pinMode(_pinCenter, INPUT);

    _initialized = true;
}

float MagneticFluxReaderClass::readSensor(uint8_t pin, float Vzero, float sensitivityMv, int readingTimeMs) {
    if (!_initialized) return 0.0f;

    const float Vref = 5.0f;                           // tensione alimentazione sensori
    //const float Vzero = 2.5f;                        // offset a 0 gauss (da calibrare per maggiore precisione)
    const float sensitivity = sensitivityMv / 1000.0f; // V/gauss (3.125 mV/G da datasheet)
    int nSamples = readingTimeMs*10;
    int raw = readAnalogTrimmed(pin, nSamples, 10, 100); // 10% cut, 100 µs delay
    float vout = (raw / 4095.0f) * Vref;
    float deltaV = vout - Vzero;
    float B_gauss = deltaV / sensitivity;    

    return B_gauss;
}

MagneticFluxReaderClass::Sample MagneticFluxReaderClass::read(int readingTimeMs) {
    if (!_initialized) {
        return { 0, 0, 0, 0, 0 };
    }

    Sample s;
    s.left   = readSensor(_pinLeft, Configuration.getHallVref(0), Configuration.getHallSensitivity(0), readingTimeMs);
    s.right  = readSensor(_pinRight, Configuration.getHallVref(1), Configuration.getHallSensitivity(1), readingTimeMs);
    s.up     = readSensor(_pinUp, Configuration.getHallVref(2), Configuration.getHallSensitivity(2), readingTimeMs);
    s.down   = readSensor(_pinDown, Configuration.getHallVref(3), Configuration.getHallSensitivity(3), readingTimeMs);
    s.center = readSensor(_pinCenter, Configuration.getHallVref(4), Configuration.getHallSensitivity(4), readingTimeMs);

    _buffer.push_back(s);
    return s;
}

float MagneticFluxReaderClass::readSensorVref(uint8_t pin, int readingTimeMs) {
    if (!_initialized) return 0.0f;    

    const float Vref = 5.0;             // tensione alimentazione sensori  
    int nSamples = readingTimeMs*10;
    int raw = readAnalogTrimmed(pin, nSamples, 10, 100); // 10% cut, 100 µs delay 
    float vout = (raw / 4095.0) * Vref;
    
    return vout;
}

MagneticFluxReaderClass::Sample MagneticFluxReaderClass::readVref(int readingTimeMs) {
    if (!_initialized) {
        return { 2.5f, 2.5f, 2.5f, 2.5f, 2.5f };
    }

    Sample s;
    s.left   = readSensorVref(_pinLeft, readingTimeMs);
    s.right  = readSensorVref(_pinRight, readingTimeMs);
    s.up     = readSensorVref(_pinUp, readingTimeMs);
    s.down   = readSensorVref(_pinDown, readingTimeMs);
    s.center = readSensorVref(_pinCenter, readingTimeMs);
    
    return s;
}

int MagneticFluxReaderClass::readAnalogTrimmed(uint8_t pin, int nSamples, int cutPercent, int delayMicros) {
    static uint16_t samples[5000];
    if (nSamples > 5000) nSamples = 5000;

    // Acquisizione
    for (int i = 0; i < nSamples; i++) {
        samples[i] = analogRead(pin);
        delayMicroseconds(delayMicros);
    }

    // Ordinamento veloce
    std::sort(samples, samples + nSamples);

    // Calcolo media troncata
    int cut = (nSamples * cutPercent) / 100;
    if (cut * 2 >= nSamples) cut = nSamples / 4; // sicurezza

    long sum = 0;
    int count = 0;

    for (int i = cut; i < nSamples - cut; i++) {
        sum += samples[i];
        count++;
    }

    return (int)(sum / count);
}


const std::vector<MagneticFluxReaderClass::Sample>& MagneticFluxReaderClass::getBuffer() const {
    return _buffer;
}

void MagneticFluxReaderClass::clearBuffer() {
    _buffer.clear();
}
