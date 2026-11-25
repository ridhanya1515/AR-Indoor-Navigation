#pragma once
#include <stdint.h>
// Hysteresis: three consecutive filtered samples. Timeouts are invalid, never 'clear'.
struct SensorLogic {
  bool valid=false, blocked=false;
  uint8_t nearCount=0, farCount=0;
  float distanceCm=-1;
  float blockCm=60, clearCm=80;
  void sample(float cm) {
    valid=cm>=2 && cm<=400;
    distanceCm=valid?cm:-1;
    if(!valid){nearCount=farCount=0;return;}
    if(cm<blockCm){if(nearCount<3)nearCount++;farCount=0;if(nearCount>=3)blocked=true;}
    else if(cm>clearCm){if(farCount<3)farCount++;nearCount=0;if(farCount>=3)blocked=false;}
    else {nearCount=farCount=0;}
  }
};
