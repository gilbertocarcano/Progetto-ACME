#ifndef SERIAL_COMMUNICATION_CLASS_H
#define SERIAL_COMMUNICATION_CLASS_H

#include <Arduino.h>
#include <vector>
#include <functional>

class SerialCommunicationClass {
public:
    using OnReceivedCommandCallback = std::function<void(const String& command, const std::vector<String>& params)>;

    SerialCommunicationClass();

    void begin(unsigned long baudRate);
    void update();
    void sendEvent(const String& event, const std::vector<String>& params);
    void log(const String& event);

    void AddOnReceivedCommandListener(OnReceivedCommandCallback callback);

private:
    bool _initialized;
    String _rxBuffer;

    std::vector<OnReceivedCommandCallback> _listeners;

    void processLine(const String& line);
};

extern SerialCommunicationClass SerialCommunication;

#endif
