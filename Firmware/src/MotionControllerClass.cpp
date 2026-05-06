#include "MotionControllerClass.h"
#include "SerialCommunicationClass.h"

MotionControllerClass MotionController;

MotionControllerClass::MotionControllerClass()
: _stepper(AccelStepper::DRIVER, 0, 0)
{
    _initialized = false;
    _pinLimitSwitch = 255;
    _pinAlarm = 255;

    _homingDone = false;
    _isHoming = false;        

    _currentPosition = 0.0f;
    _stepsPerUnit = 1.0f;

    _targetSteps = 0;
    _targetActive = false;
    _currentTargetType = TargetType::START;

    _commandActive = false;
    _waitStart = 0;

    _lastPositionReportTime = 0;
    _lastReportedPosition = NAN;

    _state = MachineState::IDLE;
}

void MotionControllerClass::begin(uint8_t pinLimitSwitch, uint8_t pinStep, uint8_t pinDir, uint8_t pinAlarm) {
    _pinLimitSwitch = pinLimitSwitch;
    pinMode(_pinLimitSwitch, INPUT_PULLUP);

    _pinAlarm = pinAlarm;
    pinMode(_pinAlarm, INPUT);

    _stepper = AccelStepper(AccelStepper::DRIVER, pinStep, pinDir);    
    _stepper.setMaxSpeed(8000);

    _stepsPerUnit = Configuration.getStepsPerUnit();

    _initialized = true;
}

bool MotionControllerClass::isLimitSwitchActive() const {
    if (!_initialized) return false;
    return digitalRead(_pinLimitSwitch) == LOW;
}

bool MotionControllerClass::isAlarmActive() const {
    if (!_initialized) return false;
    return digitalRead(_pinAlarm) == LOW;
}

void MotionControllerClass::update() {
    if (!_initialized) return;

    if (isAlarmActive()) {
        if (_state != MachineState::ALARM) {
            stateChange(MachineState::ALARM);  
            _lastAlarmTime = millis();      
        }
        if ( millis()-_lastAlarmTime > 1000) {
            stateChange(MachineState::IDLE);
        }        
        return;
    } else {
        if (_state == MachineState::ALARM) {
            stateChange(MachineState::IDLE);
            return;
        }
    }

    // --- HOMING ---
    if (_isHoming) {        
        _stepper.runSpeed();

        if (isLimitSwitchActive()) {            
            _stepper.setSpeed(0);
            _stepper.setCurrentPosition(0);
            _stepper.moveTo(0);
            _currentPosition = 0.0f;

            _isHoming = false;
            _stepper.setSpeed(Configuration.getNormalSpeed());

            _targetActive = false;
            _homingDone = true;

            notifyHomingCompleted();
            stateChange(MachineState::IDLE);
        }

        return;
    }

    // --- SE NON C'È UN COMANDO ATTIVO, PRENDINE UNO ---
    if (!_commandActive && !_queue.empty()) {
        startNextCommand();
    }

    // --- SE NON C'È NULLA DA FARE ---
    if (!_commandActive) {
        if (_queue.empty())
            stateChange(MachineState::IDLE);
        return;
    }

    MotionCommand &cmd = const_cast<MotionCommand&>(_queue.front());

    if (cmd.type == MotionCommandType::MOVE) {

        _stepper.setSpeed(Configuration.getNormalSpeed());
        _stepper.runSpeedToPosition();
        _currentPosition = _stepper.currentPosition() / _stepsPerUnit;

        unsigned long now = millis();

        if (isnan(_lastReportedPosition) || 
            abs(_currentPosition - _lastReportedPosition) >= 1 || 
            _stepper.distanceToGo()==0) {

                notifyPositionChanged(_currentPosition);
                _lastReportedPosition = _currentPosition;
                _lastPositionReportTime = now;
        }

        if (_stepper.distanceToGo() == 0) {
            notifyTargetReached();
            _queue.pop();
            _commandActive = false;
            if (cmd.targetType!= TargetType::NEXT || cmd.targetPosition == Configuration.getEndPosition())
                stateChange(MachineState::IDLE);
        }
    }

    else if (cmd.type == MotionCommandType::SCAN_STARTED) {
        notifyScanStarted();
        _queue.pop();
        _commandActive = false;
        stateChange(MachineState::SCANNING);
    }

    else if (cmd.type == MotionCommandType::SCAN_COMPLETED) {
        notifyScanCompleted();
        _queue.pop();
        _commandActive = false;
        stateChange(MachineState::IDLE);
    }

    else if (cmd.type == MotionCommandType::SCAN_PAUSED) {
        notifyScanPaused(cmd.targetPosition);
        _queue.pop();
        _commandActive = false;
    }

    else if (cmd.type == MotionCommandType::CALIBRATION_STARTED) {        
        notifyCalibrationStarted();
        _queue.pop();
        _commandActive = false;
        stateChange(MachineState::CALIBRATING);
    }

    else if (cmd.type == MotionCommandType::CALIBRATION_COMPLETED) {
        notifyCalibrationCompleted();
        _queue.pop();
        _commandActive = false;
        stateChange(MachineState::IDLE);
    }    

    else if (cmd.type == MotionCommandType::CALIBRATION_PAUSED) {
        notifyCalibrationPaused(cmd.targetPosition);
        _queue.pop();
        _commandActive = false;
    }

    else if (cmd.type == MotionCommandType::READ_STARTED) {        
        notifyMeasurementStarted();
        _queue.pop();
        _commandActive = false;
        stateChange(MachineState::MEASURING);
    }

    else if (cmd.type == MotionCommandType::READ_COMPLETED) {
        notifyMeasurementCompleted();
        _queue.pop();
        _commandActive = false;
        stateChange(MachineState::IDLE);
    }

    else if (cmd.type == MotionCommandType::WAIT) {
        if (millis() - _waitStart >= cmd.waitMs) {
            _queue.pop();
            _commandActive = false;
        }
    }
}

void MotionControllerClass::startNextCommand() {
    if (_queue.empty()) return;

    MotionCommand &cmd = const_cast<MotionCommand&>(_queue.front());
    _commandActive = true;

    if (cmd.type == MotionCommandType::MOVE) {
        _currentTargetType = cmd.targetType;
        _targetSteps = cmd.targetPosition * _stepsPerUnit;
        _stepper.moveTo(_targetSteps);
        _targetActive = true;
        if (cmd.targetType == TargetType::NEXT)
            stateChange(MachineState::SCANNING);
        else
            stateChange(MachineState::MOVING);
    }
    else if (cmd.type == MotionCommandType::WAIT) {
        _waitStart = millis();
    }
}

void MotionControllerClass::notifyHomingCompleted() {
    for (auto &cb : _homingCompletedListeners) cb();
}

void MotionControllerClass::notifyTargetReached() {
    for (auto &cb : _targetReachedListeners) cb(_currentPosition, _currentTargetType);
}

void MotionControllerClass::notifyScanStarted() {
    for (auto &cb : _scanStartedListeners) cb();
}

void MotionControllerClass::notifyScanCompleted() {
    for (auto &cb : _scanCompletedListeners) cb();
}

void MotionControllerClass::notifyScanPaused(float pos) {
    for (auto &cb : _scanPausedListeners) cb(pos);
}

void MotionControllerClass::notifyCalibrationStarted() {
    for (auto &cb : _calibrationStartedListeners) cb();
}

void MotionControllerClass::notifyCalibrationCompleted() {
    for (auto &cb : _calibrationCompletedListeners) cb();
}

void MotionControllerClass::notifyCalibrationPaused(float pos) {
    for (auto &cb : _calibrationPausedListeners) cb(pos);
}

void MotionControllerClass::notifyMeasurementStarted() {
    for (auto &cb : _measurementStartedListeners) cb();
}

void MotionControllerClass::notifyMeasurementCompleted() {
    for (auto &cb : _measurementCompletedListeners) cb();
}

void MotionControllerClass::notifyPositionChanged(float pos) {
    for (auto &cb : _positionChangedListeners) cb(pos);
}

void MotionControllerClass::enqueueMove(float pos, TargetType type) {
    if (!_homingDone)
        goToMachineHome();

    MotionCommand cmd;
    cmd.type = MotionCommandType::MOVE;
    cmd.targetPosition = pos;
    cmd.targetType = type;
    _queue.push(cmd);
}

void MotionControllerClass::enqueueWait(unsigned long ms) {
    MotionCommand cmd;
    cmd.type = MotionCommandType::WAIT;
    cmd.waitMs = ms;
    cmd.targetType = TargetType::START;
    _queue.push(cmd);
}

void MotionControllerClass::enqueueScanPaused(float position) {
    MotionCommand cmd;
    cmd.type = MotionCommandType::SCAN_PAUSED;
    cmd.targetPosition = position;
    cmd.waitMs = 0;
    cmd.targetType = TargetType::START;
    _queue.push(cmd);
}

void MotionControllerClass::enqueueCalibrationPaused(float pos) {
    MotionCommand cmd;
    cmd.type = MotionCommandType::CALIBRATION_PAUSED;
    cmd.targetPosition = pos;
    cmd.waitMs = 0;
    cmd.targetType = TargetType::START;
    _queue.push(cmd);
}

void MotionControllerClass::abort() {
    _stepper.stop();
    _stepper.setSpeed(0);
    _stepper.moveTo(_stepper.currentPosition());

    while (!_queue.empty()) _queue.pop();

    _commandActive = false;
    _targetActive = false;

    _lastReportedPosition = NAN;
    _isHoming = false;

    stateChange(MachineState::IDLE);
}

void MotionControllerClass::goToMachineHome() {
    if (_isHoming) return;

    _isHoming = true;
    _homingDone = false;
    _lastReportedPosition = NAN;

    _stepper.setSpeed(-Configuration.getHomingSpeed());

    stateChange(MachineState::HOMING);
}

void MotionControllerClass::startScan() {
    stateChange(MachineState::SCANNING);

    MotionCommand ev;
    ev.type = MotionCommandType::SCAN_STARTED;
    _queue.push(ev);

    std::vector<HRRange> hrRanges;
    int hrCount = Configuration.getHRRangeCount();
    hrRanges.reserve(hrCount);
    for (int i = 0; i < hrCount; i++) {
        hrRanges.push_back(Configuration.getHRRange(i));
    }

    float start = Configuration.getStartPosition();
    float end   = Configuration.getEndPosition();
    float stepLR = Configuration.getStepLRSize();
    float stepHR = Configuration.getStepHRSize();

    int delayBeforeMs = Configuration.getDelayBeforeRead();
    int delayAfterMs = Configuration.getDelayAfterRead();

    enqueueMove(start, TargetType::START);
    enqueueWait(delayBeforeMs);
    enqueueScanPaused(start);
    //enqueueWait(delayAfterMs);

    float pos = start;

    float step = isInHRRange(pos, hrRanges) ? stepHR : stepLR;
    if (step <= 0.0f) step = stepLR;
    pos += step;

    while (pos <= end) {
        float step = isInHRRange(pos, hrRanges) ? stepHR : stepLR;
        if (step <= 0.0f) step = stepLR;

        enqueueMove(pos, TargetType::NEXT);
        enqueueWait(delayBeforeMs);
        enqueueScanPaused(pos);
        //enqueueWait(delayAfterMs);

        pos += step;
    }

    MotionCommand done;
    done.type = MotionCommandType::SCAN_COMPLETED;
    _queue.push(done);

    enqueueMove(start, TargetType::START);
}

bool MotionControllerClass::isInHRRange(float pos, const std::vector<HRRange>& ranges) {
    for (const auto& r : ranges) {
        if (pos >= r.start && pos < r.end)
            return true;
    }
    return false;
}

void MotionControllerClass::startCalibration() {
    stateChange(MachineState::CALIBRATING);

    MotionCommand ev;
    ev.type = MotionCommandType::CALIBRATION_STARTED;
    _queue.push(ev);

    float start = Configuration.getStartPosition();
    float end   = Configuration.getEndPosition();
    float center = (start + end) / 2.0f;
    int delayBeforeMs = Configuration.getDelayBeforeRead();
    int delayAfterMs = Configuration.getDelayAfterRead();

    enqueueMove(start, TargetType::START);
    enqueueWait(delayBeforeMs);
    enqueueCalibrationPaused(start);
    //enqueueWait(delayAfterMs);

    enqueueMove(center, TargetType::CENTER);
    enqueueWait(delayBeforeMs);
    enqueueCalibrationPaused(center);
    //enqueueWait(delayAfterMs);

    enqueueMove(end, TargetType::END);
    enqueueWait(delayBeforeMs);
    enqueueCalibrationPaused(end);
    //enqueueWait(delayAfterMs);

    MotionCommand done;
    done.type = MotionCommandType::CALIBRATION_COMPLETED;
    _queue.push(done);
}

void MotionControllerClass::startMeasurement() {
    stateChange(MachineState::MEASURING);

    float start = Configuration.getStartPosition();
    float end   = Configuration.getEndPosition();
    float center = (start+end)/2;
    
    int delayBeforeMs = Configuration.getDelayBeforeRead();
    int delayAfterMs = Configuration.getDelayAfterRead();
    
    MotionCommand ev;
    ev.type = MotionCommandType::READ_STARTED;
    _queue.push(ev);

    enqueueMove(center, TargetType::CENTER);
    enqueueWait(delayBeforeMs);
    enqueueCalibrationPaused(center);
    //enqueueScanPaused(center);

    MotionCommand done;
    done.type = MotionCommandType::READ_COMPLETED;
    _queue.push(done);    
}


void MotionControllerClass::goToStart() {
    enqueueMove(Configuration.getStartPosition(), TargetType::START);
}

void MotionControllerClass::goToEnd() {
    enqueueMove(Configuration.getEndPosition(), TargetType::END);
}

void MotionControllerClass::goToCenter() {
    float center = (Configuration.getStartPosition() + Configuration.getEndPosition()) / 2.0f;
    enqueueMove(center, TargetType::CENTER);
}

float MotionControllerClass::getCurrentPosition() const {
    return _currentPosition;
}

void MotionControllerClass::addOnHomingCompletedListener(std::function<void()> callback) {
    _homingCompletedListeners.push_back(callback);
}

void MotionControllerClass::addOnTargetReachedListener(std::function<void(float, TargetType)> callback) {
    _targetReachedListeners.push_back(callback);
}

void MotionControllerClass::addOnPositionChangedListener(std::function<void(float)> callback) {
    _positionChangedListeners.push_back(callback);
}

void MotionControllerClass::addOnScanStartedListener(std::function<void()> callback) {
    _scanStartedListeners.push_back(callback);
}

void MotionControllerClass::addOnScanCompletedListener(std::function<void()> callback) {
    _scanCompletedListeners.push_back(callback);
}

void MotionControllerClass::addOnScanPausedListener(std::function<void(float)> callback) {
    _scanPausedListeners.push_back(callback);
}

void MotionControllerClass::addOnCalibrationStartedListener(std::function<void()> cb) {
    _calibrationStartedListeners.push_back(cb);
}

void MotionControllerClass::addOnCalibrationCompletedListener(std::function<void()> cb) {
    _calibrationCompletedListeners.push_back(cb);
}

void MotionControllerClass::addOnCalibrationPausedListener(std::function<void(float)> cb) {
    _calibrationPausedListeners.push_back(cb);
}

void MotionControllerClass::addOnMeasurementStartedListener(std::function<void()> cb) {
    _measurementStartedListeners.push_back(cb);
}

void MotionControllerClass::addOnMeasurementCompletedListener(std::function<void()> cb) {
    _measurementCompletedListeners.push_back(cb);
}

void MotionControllerClass::addOnStateChangedListener(std::function<void(MachineState)> callback) {
    _stateChangedListeners.push_back(callback);
}

void MotionControllerClass::stateChange(MachineState newState) {
    if (_state == newState) return;

    _state = newState;

    for (auto &cb : _stateChangedListeners)
        cb(newState);
}
