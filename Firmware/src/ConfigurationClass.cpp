#include "ConfigurationClass.h"
#include "SerialCommunicationClass.h"
#include <EEPROM.h>

ConfigurationClass Configuration;

// Indirizzi EEPROM (float = 4 byte, int = 4 byte)
static const int ADDR_START_POS      = 0;
static const int ADDR_END_POS        = 4;
static const int ADDR_STEPLR_SIZE    = 8;
static const int ADDR_STEPHR_SIZE    = 12;
static const int ADDR_STEPS_UNIT     = 16;

// NUOVI PARAMETRI
static const int ADDR_HOMING_SPEED     = 20;
static const int ADDR_NORMAL_SPEED     = 24;
static const int ADDR_DELAY_BEFORE     = 28;
static const int ADDR_DELAY_AFTER      = 32;

// HR ranges
static const int ADDR_HR_RANGES        = 36;

// Hall Vref (5 float)
static const int ADDR_HALL_VREF        = ADDR_HR_RANGES + 4 + (sizeof(HRRange) * 10);
// = 36 + 4 + 80 = 120

static const int ADDR_HALL_SENS        = ADDR_HALL_VREF + (sizeof(float) * 5);

ConfigurationClass::ConfigurationClass() {
    applyDefaults();
}

// -------------------- DEFAULTS --------------------

void ConfigurationClass::applyDefaults() {
    _values["startPosition"] = 0.0f;
    _values["endPosition"]   = 100.0f;
    _values["stepLRSize"]    = 10.0f;
    _values["stepHRSize"]    = 10.0f;
    _values["stepsPerUnit"]  = 10.0f;

    // NUOVI DEFAULT (sempre positivi)
    _values["homingSpeed"]     = 4000;
    _values["normalSpeed"]     = 4000;
    _values["delayBeforeRead"] = 100;
    _values["delayAfterRead"]  = 100;

    // HR ranges
    _hrRangeCount = 0;

    // Hall Vref default
    for (int i = 0; i < HALL_VREF_COUNT; i++)
        _hallVref[i] = 2.50f;   // default 2.5V

    // Hall Sensitivity default
    for (int i = 0; i < HALL_SENS_COUNT; i++)
        _hallSens[i] = 3.3f;
}

void ConfigurationClass::resetToDefaults() {
    applyDefaults();
    save();
}

// -------------------- SETTER --------------------

void ConfigurationClass::setStartPosition(float value) {
    _values["startPosition"] = value;
}

void ConfigurationClass::setEndPosition(float value) {
    _values["endPosition"] = value;
}

void ConfigurationClass::setStepLRSize(float value) {
    _values["stepLRSize"] = value;
}

void ConfigurationClass::setStepHRSize(float value) {
    _values["stepHRSize"] = value;
}

void ConfigurationClass::setStepsPerUnit(float value) {
    _values["stepsPerUnit"] = value;
}

// NUOVI SETTER (forzano valore positivo)

void ConfigurationClass::setHomingSpeed(int value) {
    _values["homingSpeed"] = abs(value);
}

void ConfigurationClass::setNormalSpeed(int value) {
    _values["normalSpeed"] = abs(value);
}

void ConfigurationClass::setDelayBeforeRead(int value) {
    _values["delayBeforeRead"] = abs(value);
}

void ConfigurationClass::setDelayAfterRead(int value) {
    _values["delayAfterRead"] = abs(value);
}

void ConfigurationClass::setHallVref(int index, float value) {
    if (index < 0 || index >= HALL_VREF_COUNT) return;
    _hallVref[index] = value;
}

void ConfigurationClass::setHallSensitivity(int index, float value) {
    if (index < 0 || index >= HALL_SENS_COUNT) return;
    _hallSens[index] = value;
}

// -------------------- GETTER --------------------

float ConfigurationClass::getStartPosition() const {
    return _values.at("startPosition");
}

float ConfigurationClass::getEndPosition() const {
    return _values.at("endPosition");
}

float ConfigurationClass::getStepLRSize() const {
    return _values.at("stepLRSize");
}

float ConfigurationClass::getStepHRSize() const {
    return _values.at("stepHRSize");
}

float ConfigurationClass::getStepsPerUnit() const {
    return _values.at("stepsPerUnit");
}

// NUOVI GETTER

int ConfigurationClass::getHomingSpeed() const {
    return _values.at("homingSpeed");
}

int ConfigurationClass::getNormalSpeed() const {
    return _values.at("normalSpeed");
}

int ConfigurationClass::getDelayBeforeRead() const {
    return _values.at("delayBeforeRead");
}

int ConfigurationClass::getDelayAfterRead() const {
    return _values.at("delayAfterRead");
}

float ConfigurationClass::getHallVref(int index) const {
    if (index < 0 || index >= HALL_VREF_COUNT) return 0.0f;
    return _hallVref[index];
}

float ConfigurationClass::getHallSensitivity(int index) const {
    if (index < 0 || index >= HALL_SENS_COUNT) return 0.0f;
    return _hallSens[index];
}

// -------------------- GENERIC KEY-VALUE --------------------

void ConfigurationClass::setValue(const String& key, float value) {
    _values[key] = value;
}

float ConfigurationClass::getValue(const String& key, float defaultValue) const {
    auto it = _values.find(key);
    if (it != _values.end())
        return it->second;
    return defaultValue;
}

// -------------------- HR RANGES --------------------

void ConfigurationClass::addHRRange(float start, float end) {
    if (_hrRangeCount >= MAX_HR_RANGES) return;
    _hrRanges[_hrRangeCount++] = { start, end };
}

void ConfigurationClass::clearHRRanges() {
    _hrRangeCount = 0;
}

int ConfigurationClass::getHRRangeCount() const {
    return _hrRangeCount;
}

HRRange ConfigurationClass::getHRRange(int index) const {
    return _hrRanges[index];
}

// -------------------- PERSISTENZA EEPROM --------------------

void ConfigurationClass::save() {
    EEPROM.put(ADDR_START_POS,    _values["startPosition"]);
    EEPROM.put(ADDR_END_POS,      _values["endPosition"]);
    EEPROM.put(ADDR_STEPLR_SIZE,  _values["stepLRSize"]);
    EEPROM.put(ADDR_STEPHR_SIZE,  _values["stepHRSize"]);
    EEPROM.put(ADDR_STEPS_UNIT,   _values["stepsPerUnit"]);

    // NUOVI PARAMETRI
    EEPROM.put(ADDR_HOMING_SPEED,     (int)_values["homingSpeed"]);
    EEPROM.put(ADDR_NORMAL_SPEED,     (int)_values["normalSpeed"]);
    EEPROM.put(ADDR_DELAY_BEFORE,     (int)_values["delayBeforeRead"]);
    EEPROM.put(ADDR_DELAY_AFTER,      (int)_values["delayAfterRead"]);

    // HR ranges
    EEPROM.put(ADDR_HR_RANGES, _hrRangeCount);

    int addr = ADDR_HR_RANGES + sizeof(int);
    for (int i = 0; i < _hrRangeCount; i++) {
        EEPROM.put(addr, _hrRanges[i]);
        addr += sizeof(HRRange);
    }

    // Hall Vref
    addr = ADDR_HALL_VREF;
    for (int i = 0; i < HALL_VREF_COUNT; i++) {        
        EEPROM.put(addr, _hallVref[i]);
        addr += sizeof(float);
    }

    // Hall Sensitivity
    addr = ADDR_HALL_SENS;
    for (int i = 0; i < HALL_SENS_COUNT; i++) {
        EEPROM.put(addr, _hallSens[i]);
        addr += sizeof(float);
    }
}

void ConfigurationClass::load() {
    float tmp;

    EEPROM.get(ADDR_START_POS, tmp);
    _values["startPosition"] = tmp;

    EEPROM.get(ADDR_END_POS, tmp);
    _values["endPosition"] = tmp;

    EEPROM.get(ADDR_STEPLR_SIZE, tmp);
    _values["stepLRSize"] = tmp;

    EEPROM.get(ADDR_STEPHR_SIZE, tmp);
    _values["stepHRSize"] = tmp;

    EEPROM.get(ADDR_STEPS_UNIT, tmp);
    _values["stepsPerUnit"] = tmp;

    // NUOVI PARAMETRI
    int itmp;

    EEPROM.get(ADDR_HOMING_SPEED, itmp);
    _values["homingSpeed"] = abs(itmp);

    EEPROM.get(ADDR_NORMAL_SPEED, itmp);
    _values["normalSpeed"] = abs(itmp);

    EEPROM.get(ADDR_DELAY_BEFORE, itmp);
    _values["delayBeforeRead"] = abs(itmp);

    EEPROM.get(ADDR_DELAY_AFTER, itmp);
    _values["delayAfterRead"] = abs(itmp);

    // HR ranges
    EEPROM.get(ADDR_HR_RANGES, _hrRangeCount);
    if (_hrRangeCount < 0 || _hrRangeCount > MAX_HR_RANGES)
        _hrRangeCount = 0;

    int addr = ADDR_HR_RANGES + sizeof(int);
    for (int i = 0; i < _hrRangeCount; i++) {
        EEPROM.get(addr, _hrRanges[i]);
        addr += sizeof(HRRange);
    }

    // Hall Vref
    addr = ADDR_HALL_VREF;
    for (int i = 0; i < HALL_VREF_COUNT; i++) {
        EEPROM.get(addr, _hallVref[i]);        
        addr += sizeof(float);
    }

    // Hall Sensitivity
    addr = ADDR_HALL_SENS;
    for (int i = 0; i < HALL_SENS_COUNT; i++) {
        EEPROM.get(addr, _hallSens[i]);
        addr += sizeof(float);
    }
}
