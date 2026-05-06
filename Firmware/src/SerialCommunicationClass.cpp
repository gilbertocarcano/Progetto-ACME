#include "SerialCommunicationClass.h"

SerialCommunicationClass SerialCommunication;

SerialCommunicationClass::SerialCommunicationClass() {
    _initialized = false;
    _rxBuffer.reserve(128);
}

void SerialCommunicationClass::begin(unsigned long baudRate) {
    Serial.begin(baudRate);
    _initialized = true;
}

void SerialCommunicationClass::AddOnReceivedCommandListener(OnReceivedCommandCallback callback) {
    _listeners.push_back(callback);
}

void SerialCommunicationClass::update() {
    if (!_initialized) return;

    while (Serial.available()) {
        char c = Serial.read();

        if (c == '\n') {
            String line = _rxBuffer;
            _rxBuffer = "";
            processLine(line);
        } else if (c != '\r') {
            _rxBuffer += c;
        }
    }
}

void SerialCommunicationClass::processLine(const String& line) {
    if (!line.startsWith("CMD|")) return;

    std::vector<String> tokens;
    int start = 0;
    int idx = 0;

    while ((idx = line.indexOf('|', start)) != -1) {
        tokens.push_back(line.substring(start, idx));
        start = idx + 1;
    }

    tokens.push_back(line.substring(start));

    if (tokens.size() < 2) return;

    String command = tokens[1];

    std::vector<String> params;
    for (size_t i = 2; i < tokens.size(); i++) {
        params.push_back(tokens[i]);
    }

    for (auto& cb : _listeners) {
        cb(command, params);
    }
}

void SerialCommunicationClass::sendEvent(const String& event, const std::vector<String>& params) {
    if (!_initialized) return;

    String line = "EVT|" + event;

    for (const auto& p : params) {
        line += "|" + p;
    }

    Serial.println(line);
}

void SerialCommunicationClass::log(const String& msg) {
    if (!_initialized) return;

    sendEvent("LOG",{msg});
}
