#ifndef CONFIGURATION_CLASS_H
#define CONFIGURATION_CLASS_H

#include <Arduino.h>
#include <map>

struct HRRange {
    float start;
    float end;
};

class ConfigurationClass {
public:
    ConfigurationClass();

    // Setter (con salvataggio automatico)
    void setStartPosition(float value);
    void setEndPosition(float value);
    void setStepLRSize(float value);
    void setStepHRSize(float value);
    void setStepsPerUnit(float value);

    // NUOVI PARAMETRI (sempre positivi)
    void setHomingSpeed(int value);
    void setNormalSpeed(int value);
    void setDelayBeforeRead(int value);
    void setDelayAfterRead(int value);

    // Getter
    float getStartPosition() const;
    float getEndPosition() const;
    float getStepLRSize() const;
    float getStepHRSize() const;
    float getStepsPerUnit() const;

    // NUOVI GETTER
    int getHomingSpeed() const;
    int getNormalSpeed() const;
    int getDelayBeforeRead() const;
    int getDelayAfterRead() const;

    // Hall Vref (5 valori float)
    void setHallVref(int index, float value);
    float getHallVref(int index) const;

    // Hall Sensitivity (5 valori float)
    void setHallSensitivity(int index, float value);
    float getHallSensitivity(int index) const;

    // Key-value generico
    void setValue(const String& key, float value);
    float getValue(const String& key, float defaultValue = 0.0f) const;

    // HR Ranges
    void addHRRange(float start, float end);
    void clearHRRanges();
    int getHRRangeCount() const;
    HRRange getHRRange(int index) const;

    // Persistenza
    void save();
    void load();

    // Reset ai valori di default
    void resetToDefaults();

private:
    std::map<String, float> _values;

    // HR Ranges
    static const int MAX_HR_RANGES = 10;
    HRRange _hrRanges[MAX_HR_RANGES];
    int _hrRangeCount;

    // Hall Vref
    static const int HALL_VREF_COUNT = 5;
    float _hallVref[HALL_VREF_COUNT];

    // Hall Sensitivity
    static const int HALL_SENS_COUNT = 5;
    float _hallSens[HALL_SENS_COUNT];

    void applyDefaults();
};

extern ConfigurationClass Configuration;

#endif
