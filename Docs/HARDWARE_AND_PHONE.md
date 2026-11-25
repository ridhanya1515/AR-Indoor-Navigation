# Hardware wiring reference

IMPORTANT: The guide below was written for the earlier sample map. Use START_HERE.html for the current college setup, room choices and sensor edge Junction_MainNorth. Ignore sample-map route examples and F0_BC references below. Wiring, Wi-Fi credentials and build instructions still apply.

# Campus AR: one-sensor review project

This updated package follows slide 13 of INDOOR_NAVIGATION PROJECT(6).pptx within your available hardware: one ESP32, one ultrasonic sensor, and a Redmi Note 10.

**Source package, not a compiled APK.** Unity compilation, Android camera tracking and physical ESP32 operation must be checked on your devices. The map is a demonstration layout, not the surveyed college. Keep your old project separate.

## 1. Open this new project

1. Extract the ZIP to a NEW folder such as D:\CampusAR_Review. Do not overwrite your old Unity project.
2. Open Unity Hub, select Projects, then Add / Add project from disk.
3. Choose the **UnityProject** folder inside the extracted package. Select the folder containing Assets, Packages and ProjectSettings.
4. Use Unity **6000.3.25f1** (6.3 LTS). Allow package downloads to finish. Internet is needed for setup, not for the local review demonstration.
5. Choose **Campus Navigation > 1 - Create or open project scene** from Unity's top menu.
6. Close and reopen this project after setup so the Input System setting takes effect.
7. Run **Campus Navigation > 3 - Validate bundled map**, then **4 - Run routing tests**. These also check the review login.
8. If there is a red Console error, stop and copy the FIRST error. Do not delete scripts or copy scripts from your old project into this one.

## 2. Review account and app flow

Username: **student**

Password: **Campus123!** (case-sensitive)

Leave **Use optional laptop login server** unticked. The local account validates these credentials and loads the bundled map without internet or a laptop server. This is a fixed demonstration account, not production user management. There is no unauthenticated bypass button.

After login:

1. The app displays the map and a searchable list of rooms.
2. Tap **Office (F0)**. Selecting the room opens the camera automatically on Android.
3. Allow camera permission if prompted. Move the phone slowly until the message says tracking is ready.
4. Stand at your marked entrance, face the marked forward direction, and select **Main entrance (demo)**.
5. Set the height of your phone camera above the floor, then tap **Confirm position and direction**.
6. Navigation starts immediately. Blue arrows appear on the floor in the camera image; distance, heading instructions and X/Z update as you walk.
7. The app monitors the ESP32. A blocked corridor triggers replanning to the same destination.
8. Arrival displays **Destination reached!**. If no route remains, use Help.

Wi-Fi carries sensor messages. It does not determine your position. The confirmed checkpoint and facing direction align the map, then phone AR tracking measures movement. There is no automatic Wi-Fi/Bluetooth localization or automatic image-marker scanning in this review version.

## 3. First test on the PC

1. Press Play in the generated scene. Use a portrait Game view if possible.
2. Login with the review account.
3. Before choosing a room, open Settings and tap **Disable ESP32** if hardware is not connected. Otherwise the monitored corridor is intentionally excluded until valid readings arrive.
4. Return to the map, choose Office, choose Main entrance and confirm alignment.
5. Click within the Game view. **W/A/S/D** move the simulated person; **Q/E** turn.
6. To demonstrate software rerouting, choose Destinations, Settings, then **Toggle simulated blockage: F0_BC**. Return to the map and start navigation again. Simulated readings are explicitly labelled.
7. Clear the simulated blockage before testing real hardware.

The PC uses a virtual corridor display. Live phone-camera AR is tested in the Android app. The included upper floors are optional; use only the ground-floor loop for your first review. Floor changes require explicit landing confirmation.

## 4. Wire the sensor

The firmware assumes a **classic ESP32 DevKit and four-pin HC-SR04**. Confirm those names on your hardware before wiring. Different boards/sensors can require different pins or voltages.

Required: your board and sensor, a USB data cable and power source, jumper wires, and a **1 kΩ and 2 kΩ resistor** (or a suitable level shifter). A breadboard makes the divider easier to connect. No extra Bluetooth beacon is required.

Disconnect power while wiring.

| Sensor pin | Connection |
|---|---|
| VCC | ESP32 board's documented 5 V/VIN supply |
| GND | ESP32 GND |
| TRIG | GPIO 5 |
| ECHO | 1 kΩ resistor to a junction; junction to GPIO 18; 2 kΩ from junction to GND |

**Do not connect a 5 V ECHO directly to GPIO 18 or feed 5 V into the 3V3 pin.** The divider reduces ECHO to approximately 3.3 V. Power the DevKit through USB.

## 5. Program ESP32 and connect the phone

1. Install Arduino IDE and the **esp32 by Espressif Systems** board package using Boards Manager.
2. Open **Firmware/CampusSensor/CampusSensor.ino**. Keep SensorLogic.h in that same folder.
3. Select the actual board model and COM port. For a classic DevKit this is commonly ESP32 Dev Module.
4. Leave ROUTER_SSID and ROUTER_PASSWORD blank for this direct-hotspot demonstration.
5. Upload, then open Serial Monitor at **115200** baud. Note the hotspot IP address.
6. On the phone, open Wi-Fi settings and connect to **CampusAR-Sensor**, password **CampusAR123**.
7. Stay connected when Android says **No internet**. If the phone automatically switches to mobile data, disable that switching or temporarily turn mobile data off.
8. Open the app and log in. Hardware monitoring is enabled automatically. Default endpoint: **http://192.168.4.1/status**. Default sensor key: **change-this-sensor-key**.
9. If you change SENSOR_KEY in firmware, enter the same key in app Settings and tap Enable ESP32. These supplied credentials are for the private review demonstration only.
10. Point the sensor at a fixed target 100–150 cm away. Wait for **CLEAR** in the app.
11. Place an object 30–40 cm away. Wait for **BLOCKED**. Remove it to restore CLEAR.

The phone can open **http://192.168.4.1/** in its browser to check connectivity. The /status endpoint requires a header supplied by the app; an unauthorized response in a normal browser is expected.

Block threshold: below **60 cm**. Clear threshold: above **80 cm**. The firmware uses a median filter and repeated readings to reduce chatter. A missing echo is **unknown**, not clear. Keep a background target within sensor range. Offline/stale readings exclude the monitored corridor; they are labelled separately from physical BLOCKED readings.

## 6. Build the Android app

1. Unity Hub > Installs > this editor's menu > Add modules: ensure Android Build Support, SDK/NDK Tools and OpenJDK are installed.
2. Stop Play in Unity. Select **Campus Navigation > 2 - Build Android APK**.
3. If Unity switches to Android and recompiles, wait and select the build menu again.
4. After the successful APK message, find **UnityProject/Builds/CampusAR_Workflow.apk**.
5. Copy it to your phone's Download folder by USB, open it in File Manager and install.
6. Open **Campus AR Workflow**, not your old app. If prompted, install/update Google Play Services for AR.
7. Login first. Choosing a destination starts the camera and permission request. If permission was denied, allow Camera in Android's app settings and reopen the app.

Every source change requires a new APK build and installation. This package uses the built-in renderer and its own app ID. Do not import your old URP project over it.

## 7. Demonstrate the real hardware reroute

Use **Docs/Demo_Floorplan.svg** to mark a matching course in a clear area. The loop is 6 m wide and 8 m long, with Entrance 2 m before its southwest junction. Office is 2 m west of the northwest junction. Do not follow this sample map through real walls or furniture. Change and measure the JSON coordinates if your demonstration area differs.

1. Fix the sensor to monitor the corridor represented by **F0_BC** (west-middle to west-north junction). It is stationary, not carried by the user.
2. Connect phone to ESP32 Wi-Fi, log in and wait for CLEAR.
3. Select Office, align at Entrance facing the loop, and start walking.
4. **Before entering the monitored corridor**, have a helper place the obstacle in front of the sensor.
5. Verify BLOCKED, an increased reroute count, and a different blue route while Office remains the destination. View the small map if useful.
6. Follow the alternate route and verify the arrival message.
7. Remove the obstacle and repeat. Also test disconnecting ESP32: stale/unknown must be visible and its corridor must be excluded.

One sensor only monitors its assigned passage. It does not identify obstacles everywhere. If the user is already inside the blocked passage, the planner may stop and request assistance instead of drawing an unsafe connector through it.

## 8. Optional server, limitations and troubleshooting

For real database-backed login on your laptop, follow **Docs/OPTIONAL_SERVER.md**. This is optional for the local review account.

- If X/Z stay unchanged while you WALK, stop: check Camera permission, AR services, lighting and tracking status. Turning in place changes heading, not necessarily position.
- If tracking is lost or the app goes into the background, return to a marked checkpoint and realign.
- Sensor STALE: verify Wi-Fi, power, endpoint, key, then the browser connectivity page.
- CLEAR never appears: check the ECHO divider and background target. No echo is intentionally not clear.
- No route: check sensor status and starting position; do not disable real sensor protection merely to force a path.
- A physical floor plan and measured checkpoint headings are required for college deployment. The old college meshes are not included in this separate project.

Review statement: “This prototype demonstrates login, map selection, camera-based AR guidance and A* rerouting using one fixed ultrasonic sensor over Wi-Fi. Starting position is manually aligned. Building-wide automatic positioning is future work.”

See Docs/VALIDATION.md for the exact tests run and outstanding phone checks.
