#include "SerialApiClass.h"

SerialApiClass SerialApi;

SerialApiClass::SerialApiClass() {
    _initialized = false;
}

void SerialApiClass::begin() {

    // --- Sottoscrizione callback SerialCommunication ---
    SerialCommunication.AddOnReceivedCommandListener(
        [this](const String& cmd, const std::vector<String>& params) {
            this->executeCommand(cmd, params);
        }
    );

    // --- Sottoscrizione callback MotionController ---
    MotionController.addOnHomingCompletedListener(
        [this]() {
            this->homingCompleted();
        }
    );

    MotionController.addOnTargetReachedListener(
        [this](float pos, TargetType type) {
            this->targetReached(pos, type);
        }
    );

    MotionController.addOnScanStartedListener(
        [this]() {
            this->scanStarted();
        }
    );

    MotionController.addOnScanCompletedListener(
        [this]() {
            this->scanCompleted();
        }
    );

    MotionController.addOnScanPausedListener(
        [this](float pos) {
            this->scanPaused(pos);
        }
    );

    MotionController.addOnCalibrationStartedListener(
        [this]() {
            this->calibrationStarted();
        }
    );

    MotionController.addOnCalibrationCompletedListener(
        [this]() {
            this->calibrationCompleted();
        }
    );

    MotionController.addOnCalibrationPausedListener(
        [this](float pos) {
            this->calibrationPaused(pos);
        }
    );

    MotionController.addOnMeasurementStartedListener(
        [this]() {
            this->measurementStarted();
        }
    );

    MotionController.addOnMeasurementCompletedListener(
        [this]() {
            this->measurementCompleted();
        }
    );

    MotionController.addOnPositionChangedListener(
        [this](float pos) {
            this->positionChanged(pos);
        }
    );

    // --- NUOVO: sottoscrizione stato macchina ---
    MotionController.addOnStateChangedListener(
        [this](MachineState st) {
            this->stateChanged(st);
        }
    );

    _initialized = true;
}

void SerialApiClass::update() {
    if (!_initialized) return;

    SerialCommunication.update();
}

// -----------------------------------------------------------------------------
// CALLBACK PRIVATI
// -----------------------------------------------------------------------------

void SerialApiClass::executeCommand(const String& command, const std::vector<String>& params) {   
    if (command == "SET_STEPSPERUNIT") Configuration.setStepsPerUnit(std::stof(params[0].c_str()));
    if (command == "SET_STEPLRSIZE") Configuration.setStepLRSize(std::stof(params[0].c_str()));
    if (command == "SET_STEPHRSIZE") Configuration.setStepHRSize(std::stof(params[0].c_str()));
    if (command == "SET_STARTPOSITION") Configuration.setStartPosition(std::stof(params[0].c_str()));
    if (command == "SET_ENDPOSITION") Configuration.setEndPosition(std::stof(params[0].c_str()));
    if (command == "SET_HOMINGSPEED") Configuration.setHomingSpeed(std::stoi(params[0].c_str()));
    if (command == "SET_NORMALSPEED") Configuration.setNormalSpeed(std::stoi(params[0].c_str()));
    if (command == "SET_DELAYBEFOREREAD") Configuration.setDelayBeforeRead(std::stoi(params[0].c_str()));
    if (command == "SET_DELAYAFTERREAD") Configuration.setDelayAfterRead(std::stoi(params[0].c_str()));
    if (command == "SET_HALLVREF") Configuration.setHallVref(std::stoi(params[0].c_str()), std::stof(params[1].c_str()));
    if (command == "SET_HALLSENSITIVITY") Configuration.setHallSensitivity(std::stoi(params[0].c_str()), std::stof(params[1].c_str()));

    if (command == "SET_HRRANGES") {
        Configuration.clearHRRanges();
        for (const auto& p : params) {
            int commaIndex = p.indexOf(',');
            if (commaIndex < 0) continue;

            float start = p.substring(0, commaIndex).toFloat();
            float end   = p.substring(commaIndex + 1).toFloat();   

            Configuration.addHRRange(start, end);
        }
    }

    if (command == "GET_STEPSPERUNIT") SerialCommunication.sendEvent("STEPSPERUNIT",{String(Configuration.getStepsPerUnit())});
    if (command == "GET_STEPLRSIZE") SerialCommunication.sendEvent("STEPLRSIZE",{String(Configuration.getStepLRSize())});
    if (command == "GET_STEPHRSIZE") SerialCommunication.sendEvent("STEPHRSIZE",{String(Configuration.getStepHRSize())});
    if (command == "GET_STARTPOSITION") SerialCommunication.sendEvent("STARTPOSITION",{String(Configuration.getStartPosition())});
    if (command == "GET_ENDPOSITION") SerialCommunication.sendEvent("ENDPOSITION",{String(Configuration.getEndPosition())});
    if (command == "GET_HOMINGSPEED") SerialCommunication.sendEvent("HOMINGSPEED", { String(Configuration.getHomingSpeed()) });
    if (command == "GET_NORMALSPEED") SerialCommunication.sendEvent("NORMALSPEED", { String(Configuration.getNormalSpeed()) });
    if (command == "GET_DELAYBEFOREREAD") SerialCommunication.sendEvent("DELAYBEFOREREAD", { String(Configuration.getDelayBeforeRead()) });
    if (command == "GET_DELAYAFTERREAD") SerialCommunication.sendEvent("DELAYAFTERREAD", { String(Configuration.getDelayAfterRead()) });
    if (command == "GET_HALLVREF") {
        std::vector<String> out;        
        for (int i=0; i<5; i++)
            out.push_back(String(Configuration.getHallVref(i),3));            
        SerialCommunication.sendEvent("HALLVREF", out);
    }
    if (command == "GET_HALLSENSITIVITY") {
        std::vector<String> out;        
        for (int i=0; i<5; i++)
            out.push_back(String(Configuration.getHallSensitivity(i),3));            
        SerialCommunication.sendEvent("HALLSENSITIVITY", out);
    }

    if (command == "GET_HRRANGES") {
        std::vector<String> out;
        int count = Configuration.getHRRangeCount();
        for (int i = 0; i < count; i++) {
            HRRange r = Configuration.getHRRange(i);
            out.push_back(String(r.start) + "," + String(r.end));
        }
        SerialCommunication.sendEvent("HRRANGES", out);
    }

    if (command == "SAVE_CONFIG") Configuration.save();

    if (command == "GOTO_HOME") MotionController.goToMachineHome();
    if (command == "GOTO_START") MotionController.goToStart();
    if (command == "GOTO_END") MotionController.goToEnd();
    if (command == "GOTO_CENTER") MotionController.goToCenter();
    if (command == "SCAN") MotionController.startScan();
    if (command == "CALIBRATE") MotionController.startCalibration();
    if (command == "MEASURE") MotionController.startMeasurement();
    if (command == "ABORT") MotionController.abort();
    if (command == "RESET") NVIC_SystemReset();
}

void SerialApiClass::homingCompleted() {
    SerialCommunication.sendEvent("HOMING_COMPLETED", {});
}

void SerialApiClass::targetReached(float pos, TargetType type) {
    SerialCommunication.sendEvent(
        "TARGET_REACHED",
        { String(pos), targetTypeToString(type) }
    );
}

void SerialApiClass::scanStarted() {
    SerialCommunication.sendEvent("SCAN_STARTED", {});
}

void SerialApiClass::scanCompleted() {
    SerialCommunication.sendEvent("SCAN_COMPLETED", {});
}

void SerialApiClass::scanPaused(float pos) {
    MagneticFluxReaderClass::Sample s = MagneticFluxReader.read(Configuration.getDelayAfterRead());
    SerialCommunication.sendEvent("SCAN_VALUES_READ", {
        String(pos), 
        String(s.left), 
        String(s.right), 
        String(s.up), 
        String(s.down), 
        String(s.center)
    });
}

void SerialApiClass::calibrationStarted() {    
    SerialCommunication.sendEvent("CALIBRATION_STARTED", {});
}

void SerialApiClass::calibrationCompleted() {
    SerialCommunication.sendEvent("CALIBRATION_COMPLETED", {});
}

void SerialApiClass::calibrationPaused(float pos) {
    MagneticFluxReaderClass::Sample s = MagneticFluxReader.readVref(Configuration.getDelayAfterRead());
    SerialCommunication.sendEvent("CALIBRATION_VALUES_READ", {
        String(pos), 
        String(s.left,3), 
        String(s.right,3), 
        String(s.up,3), 
        String(s.down,3), 
        String(s.center,3)
    });
}

void SerialApiClass::measurementStarted() {    
    SerialCommunication.sendEvent("MEASUREMENT_STARTED", {});
}

void SerialApiClass::measurementCompleted() {
    SerialCommunication.sendEvent("MEASUREMENT_COMPLETED", {});
}

void SerialApiClass::positionChanged(float pos) {
    SerialCommunication.sendEvent("P", { String(pos) });
}

// -----------------------------------------------------------------------------
// NUOVO: callback stato macchina
// -----------------------------------------------------------------------------

void SerialApiClass::stateChanged(MachineState st) {
    String s;

    switch (st) {
        case MachineState::IDLE:         s = "IDLE"; break;
        case MachineState::HOMING:       s = "HOMING"; break;
        case MachineState::MOVING:       s = "MOVING"; break;
        case MachineState::SCANNING:     s = "SCANNING"; break;
        case MachineState::CALIBRATING : s = "CALIBRATING"; break;
        case MachineState::MEASURING :   s = "MEASURING"; break;
        case MachineState::ALARM :       s = "ALARM"; break;
        default:                         s = "UNKNOWN"; break;
    }

    SerialCommunication.sendEvent("STATE_CHANGED", { s });
}

// -----------------------------------------------------------------------------
// Utility
// -----------------------------------------------------------------------------

String SerialApiClass::targetTypeToString(TargetType type) {
    switch (type) {
        case TargetType::START:  return "START";
        case TargetType::CENTER: return "CENTER";
        case TargetType::END:    return "END";
        case TargetType::NEXT:   return "NEXT";
    }
    return "UNKNOWN";
}
