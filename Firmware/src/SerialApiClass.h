#ifndef SERIAL_API_CLASS_H
#define SERIAL_API_CLASS_H

#include <Arduino.h>
#include <vector>
#include <functional>

#include "SerialCommunicationClass.h"
#include "MotionControllerClass.h"
#include "MagneticFluxReaderClass.h"

class SerialApiClass {
public:
    SerialApiClass();

    void begin();     // NON inizializza la seriale
    void update();    // chiama solo SerialCommunication.update()

private:
    bool _initialized;

    // --- CALLBACK PRIVATI COLLEGATI A SerialCommunication ---
    void executeCommand(const String& command, const std::vector<String>& params);

    // --- CALLBACK PRIVATI COLLEGATI A MotionController ---
    void homingCompleted();
    void targetReached(float pos, TargetType type);
    void scanStarted();
    void scanCompleted();
    void scanPaused(float pos);
    void calibrationStarted();
    void calibrationCompleted();
    void calibrationPaused(float pos);
    void measurementStarted();
    void measurementCompleted();
    void positionChanged(float pos);
    void stateChanged(MachineState st);

    // --- Utility ---
    String targetTypeToString(TargetType type);
};

extern SerialApiClass SerialApi;

#endif
