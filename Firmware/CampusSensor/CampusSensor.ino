#include <WiFi.h>
#include <WebServer.h>
#include <esp_system.h>
#include "SensorLogic.h"

// Classic ESP32 DevKit + HC-SR04. Adapt pins for another board.
const int TRIG_PIN=5;
const int ECHO_PIN=18; // Use a 1 kOhm / 2 kOhm divider: HC-SR04 Echo is 5 V.
const char* SENSOR_ID="corridor-01";
const char* EDGE_ID="Junction_MainNorth"; // Must match the mapped corridor in Unity/server JSON.

// Set your own key before use. Use the same key in Unity/server settings.
const char* SENSOR_KEY="YOUR_SENSOR_KEY";
const char* AP_SSID="CampusAR-Sensor";

// Set your own hotspot password before use (at least 8 characters).
const char* AP_PASSWORD="YOUR_AP_PASSWORD";

// Leave blank for direct ESP32 hotspot demo.
// For full login+sensor LAN use your router SSID/password.
const char* ROUTER_SSID="";
const char* ROUTER_PASSWORD="";

WebServer server(80);
SensorLogic sensor;
String bootId;
uint32_t sequence=0,lastSample=0;
float samples[5];int sampleCount=0,sampleAt=0;
const char* headers[]={"X-Sensor-Key"};

float readDistance() {
  digitalWrite(TRIG_PIN,LOW);delayMicroseconds(2);
  digitalWrite(TRIG_PIN,HIGH);delayMicroseconds(10);digitalWrite(TRIG_PIN,LOW);
  unsigned long duration=pulseIn(ECHO_PIN,HIGH,26000);
  if(duration==0)return -1;
  return duration*.0343f/2;
}

void takeSample() {
  float cm=readDistance();
  if(cm<2||cm>400){sampleCount=0;sampleAt=0;sensor.sample(-1);}
  else {
    samples[sampleAt]=cm;sampleAt=(sampleAt+1)%5;if(sampleCount<5)sampleCount++;
    float sorted[5];for(int i=0;i<sampleCount;i++)sorted[i]=samples[i];
    for(int i=0;i<sampleCount;i++)for(int j=i+1;j<sampleCount;j++)if(sorted[j]<sorted[i]){float x=sorted[i];sorted[i]=sorted[j];sorted[j]=x;}
    sensor.sample(sorted[sampleCount/2]);
  }
  lastSample=millis();sequence++;
}

void status() {
  if(server.header("X-Sensor-Key")!=SENSOR_KEY){server.send(401,"application/json","{\"error\":\"unauthorized\"}");return;}
  String json="{\"sensorId\":\""+String(SENSOR_ID)+"\",\"edgeId\":\""+String(EDGE_ID)+"\",\"bootId\":\""+bootId+"\",\"sequence\":"+String(sequence);
  json+=",\"valid\":"+String(sensor.valid?"true":"false")+",\"blocked\":"+String(sensor.blocked?"true":"false");
  json+=",\"distanceCm\":"+String(sensor.distanceCm,1)+",\"ageMs\":"+String((uint32_t)(millis()-lastSample))+"}";
  server.sendHeader("Cache-Control","no-store");server.send(200,"application/json",json);
}

void setup() {
  Serial.begin(115200);pinMode(TRIG_PIN,OUTPUT);pinMode(ECHO_PIN,INPUT);
  bootId=String(esp_random(),HEX)+String(esp_random(),HEX);
  if(strlen(ROUTER_SSID)>0) {
    WiFi.mode(WIFI_STA);WiFi.begin(ROUTER_SSID,ROUTER_PASSWORD);
    uint32_t start=millis();while(WiFi.status()!=WL_CONNECTED && millis()-start<15000){delay(100);}
  }
  if(WiFi.status()!=WL_CONNECTED){WiFi.mode(WIFI_AP);WiFi.softAP(AP_SSID,AP_PASSWORD);Serial.print("Hotspot address: ");Serial.println(WiFi.softAPIP());}
  else {Serial.print("Router address: ");Serial.println(WiFi.localIP());WiFi.setAutoReconnect(true);}
  server.collectHeaders(headers,1);server.on("/status",HTTP_GET,status);
  server.on("/",HTTP_GET,[]{server.send(200,"text/plain","Campus AR sensor is online. The app uses /status with X-Sensor-Key.");});
  server.begin();takeSample();
}

void loop() {
  server.handleClient();
  if((uint32_t)(millis()-lastSample)>=100)takeSample();
  delay(1);
}