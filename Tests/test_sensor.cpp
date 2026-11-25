#include "../Firmware/CampusSensor/SensorLogic.h"
#include <cassert>
#include <iostream>
int main(){
 SensorLogic s;
 assert(!s.valid);
 s.sample(40);s.sample(40);assert(!s.blocked);s.sample(40);assert(s.blocked&&s.valid);
 s.sample(70);assert(s.blocked); // hysteresis band cannot clear
 s.sample(100);s.sample(100);assert(s.blocked);s.sample(100);assert(!s.blocked);
 s.sample(-1);assert(!s.valid&&s.distanceCm==-1); // no-echo must not be labelled clear
 s.sample(40);s.sample(-1);s.sample(40);s.sample(40);assert(!s.blocked);
 s.sample(40);assert(s.blocked);
 s.sample(401);assert(!s.valid&&s.blocked);
 std::cout<<"SensorLogic: 10 assertions passed\n";
}
