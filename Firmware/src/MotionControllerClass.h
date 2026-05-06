#ifndef MOTION_CONTROLLER_CLASS_H
#define MOTION_CONTROLLER_CLASS_H

#include <Arduino.h>
#include <AccelStepper.h>
#include <vector>
#include <queue>
#include <functional>
#include "ConfigurationClass.h"

enum class TargetType {
    START,
    CENTER,
    END,
    NEXT
};

enum class MotionCommandType {
    MOVE,
    WAIT,
    SCAN_PAUSED,
    SCAN_STARTED,
    SCAN_COMPLETED,
    READ_STARTED,
    CALIBRATION_STARTED,
    CALIBRATION_PAUSED,
    CALIBRATION_COMPLETED,
    READ_COMPLETED
};

enum class MachineState {
    IDLE,
    HOMING,
    MOVING,
    SCANNING,
    CALIBRATING,
    MEASURING,
    ALARM
};

struct MotionCommand {
    MotionCommandType type;
    float targetPosition;      // valido per MOVE    
    unsigned long waitMs;      // valido per WAIT
    TargetType targetType;     // START, END, CENTER, NEXT
};

class MotionControllerClass {
public:
    MotionControllerClass();

    void begin(uint8_t pinLimitSwitch, uint8_t pinStep, uint8_t pinDir, uint8_t pinAlarm);

    void update();

    // --- API DI ALTO LIVELLO (accodano comandi) ---
    void goToMachineHome();    
    void startScan();
    void startCalibration();
    void startMeasurement();
    void goToStart();
    void goToEnd();
    void goToCenter();
    void abort();

    float getCurrentPosition() const;

    // --- CALLBACK MULTICAST ---
    void addOnHomingCompletedListener(std::function<void()> callback);
    void addOnTargetReachedListener(std::function<void(float, TargetType)> callback);
    void addOnPositionChangedListener(std::function<void(float)> callback);
    void addOnScanStartedListener(std::function<void()> callback);
    void addOnScanCompletedListener(std::function<void()> callback);
    void addOnScanPausedListener(std::function<void(float)> callback);
    void addOnCalibrationStartedListener(std::function<void()> callback);
    void addOnCalibrationCompletedListener(std::function<void()> callback);
    void addOnCalibrationPausedListener(std::function<void(float)> callback);
    void addOnMeasurementStartedListener(std::function<void()> callback);
    void addOnMeasurementCompletedListener(std::function<void()> callback);

    void addOnStateChangedListener(std::function<void(MachineState)> callback);

private:
    uint8_t _pinLimitSwitch;
    uint8_t _pinAlarm;
    bool _initialized;
    long _lastAlarmTime;

    bool _homingDone;
    bool _isHoming;        

    float _currentPosition;
    float _stepsPerUnit;

    long _targetSteps;
    bool _targetActive;
    TargetType _currentTargetType;

    unsigned long _lastPositionReportTime;
    float _lastReportedPosition;

    AccelStepper _stepper;

    std::vector<std::function<void()>> _homingCompletedListeners;
    std::vector<std::function<void(float, TargetType)>> _targetReachedListeners;
    std::vector<std::function<void(float)>> _positionChangedListeners;
    std::vector<std::function<void()>> _scanStartedListeners;
    std::vector<std::function<void()>> _scanCompletedListeners;
    std::vector<std::function<void(float)>> _scanPausedListeners;
    std::vector<std::function<void()>> _calibrationStartedListeners;
    std::vector<std::function<void()>> _calibrationCompletedListeners;
    std::vector<std::function<void(float)>> _calibrationPausedListeners;
    std::vector<std::function<void()>> _measurementStartedListeners;
    std::vector<std::function<void()>> _measurementCompletedListeners;


    std::vector<std::function<void(MachineState)>> _stateChangedListeners;

    MachineState _state;

    // --- CODA FIFO ---
    std::queue<MotionCommand> _queue;
    bool _commandActive;
    unsigned long _waitStart;

    void enqueueMove(float pos, TargetType type);
    void enqueueWait(unsigned long ms);
    void enqueueScanPaused(float pos);
    void enqueueCalibrationPaused(float pos);

    bool isLimitSwitchActive() const;
    bool isAlarmActive() const;

    void notifyHomingCompleted();
    void notifyTargetReached();
    void notifyPositionChanged(float pos);
    void notifyScanStarted();
    void notifyScanCompleted();
    void notifyScanPaused(float pos);
    void notifyCalibrationStarted();
    void notifyCalibrationCompleted();
    void notifyCalibrationPaused(float pos);
    void notifyMeasurementStarted();
    void notifyMeasurementCompleted();

    void startNextCommand();
    bool isInHRRange(float pos, const std::vector<HRRange>& ranges);

    void stateChange(MachineState newState);
};

extern MotionControllerClass MotionController;

#endif
