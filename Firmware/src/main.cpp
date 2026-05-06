#include <Arduino.h>
#include "SerialCommunicationClass.h"
#include "SerialApiClass.h"
#include "ConfigurationClass.h"


void setup() {
  analogReadResolution(12);
  Configuration.load();
  SerialCommunication.begin(1000000);
  SerialApi.begin();
  MotionController.begin(10,12,11,9);
  MagneticFluxReader.begin(A0,A1,A2,A3,A4);
}

void loop() {
  SerialCommunication.update();
  MotionController.update();
}