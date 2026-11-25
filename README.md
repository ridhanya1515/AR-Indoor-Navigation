# AR-Based Indoor Navigation System with Dynamic Rerouting

An academic project that combines augmented reality navigation with ESP32-based obstacle detection to guide users through a mapped college building.

Built using **Unity, C#, AR Foundation, ARCore, Python and an ESP32 with an ultrasonic sensor**.

## Project Overview

The application lets users log in, select a destination and align their starting position at a known checkpoint. It calculates a route through the building and displays directional arrows over the Android phone’s live camera view.

An ultrasonic sensor monitors a mapped corridor. When the sensor detects a blockage, the application receives its status from the ESP32 over Wi-Fi. If the blockage affects the active route, the application recalculates a route using A*.

## Features

- College building map with checkpoints and destinations.
- Local demonstration login and optional Python server login.
- Graph-based A* route planning.
- AR directional arrows and navigation instructions.
- Remaining distance and position updates.
- Live corridor status from an ESP32.
- Dynamic rerouting around blocked corridors when an alternative exists.
- Desktop simulation for demonstrating navigation.
- Ground, first and second floor map data.

## Workflow

1. Log in to the application.
2. Select a destination.
3. Select a known starting checkpoint.
4. Confirm your position and facing direction.
5. Start navigation.
6. Follow the AR arrows and instructions.
7. Receive live corridor status over Wi-Fi.
8. Recalculate the route when a relevant corridor becomes blocked.
9. Continue towards the destination.

## Technology Stack

| Component | Technology |
|---|---|
| Application development | Unity and C# |
| Mobile augmented reality | AR Foundation and ARCore |
| Pathfinding | A* on a navigation graph |
| Sensor firmware | Arduino framework for ESP32 |
| Obstacle detection | HC-SR04 ultrasonic sensor |
| Sensor communication | HTTP over Wi-Fi |
| Optional authentication server | Python and SQLite |

## Repository Structure

| Folder | Contents |
|---|---|
| UnityProject | Unity assets, packages and project settings |
| Firmware | ESP32 firmware and sensor logic |
| Server | Optional Python login and map server |
| Docs | Project documentation |
| OriginalMaps | Source map files |
| Tests | Supporting tests |

## Hardware Requirements

- ESP32 development board.
- HC-SR04 ultrasonic sensor.
- Breadboard and jumper wires.
- 1 kΩ and 2 kΩ resistors for the Echo voltage divider.
- USB cable and power source.
- ARCore-compatible Android phone.
- Computer with Unity for development and desktop simulation.

### Sensor Connections

| HC-SR04 connection | ESP32 connection |
|---|---|
| VCC | Board’s suitable 5 V supply |
| GND | GND |
| TRIG | GPIO 5 |
| ECHO | GPIO 18 through the voltage divider |

Voltage divider connections:

- HC-SR04 ECHO connects through a 1 kΩ resistor to a junction.
- The junction connects to ESP32 GPIO 18.
- A 2 kΩ resistor connects from the junction to GND.
- The sensor and ESP32 share a common ground.

**Do not connect the sensor’s 5 V Echo output directly to an ESP32 GPIO. Disconnect power before changing wiring.**

## Open the Unity Project

1. Clone or download this repository.
2. Check `UnityProject/ProjectSettings/ProjectVersion.txt` for the Unity version.
3. Install that version using Unity Hub.
4. Add the `UnityProject` folder to Unity Hub.
5. Open the project and allow package imports to finish.
6. Open `Assets/CampusNavigation/Scenes/CampusNavigation.unity`.

For Android builds, install Android Build Support, including the SDK, NDK and OpenJDK, for the selected Unity editor.

## Desktop Demonstration

1. Open the CampusNavigation scene.
2. Select the Campus Navigation object.
3. Enable `Simulate In Editor`.
4. Enter Play mode.
5. Log in using the local demonstration account.
6. Select a destination and confirm a starting checkpoint and direction.
7. Click inside the Game view and use the simulation controls.

The local demonstration account is:

- Username: `student`
- Password: `Campus123!`

This is an intentionally public assessment account. It is not production authentication.

## ESP32 Setup

1. Open `Firmware/CampusSensor/CampusSensor.ino` in Arduino IDE.
2. Install the ESP32 board support package.
3. Select your ESP32 board and its COM port.
4. Replace `YOUR_AP_PASSWORD` with a local hotspot password of at least eight characters.
5. Replace `YOUR_SENSOR_KEY` with your own sensor key.
6. Upload the sketch to the ESP32.
7. Connect the demonstration phone or computer to `CampusAR-Sensor`.
8. Keep the ESP32 powered during sensor testing.

For direct hotspot mode, leave the router SSID and password empty.

The default sensor endpoint is:

`http://192.168.4.1/status`

The endpoint requires an `X-Sensor-Key` header.

## Connect the Application to the Sensor

1. Set the application’s Sensor URL to the ESP32 endpoint.
2. Set its Sensor Key to the same value used in the firmware.
3. Check the Campus Navigation component in the scene: saved Inspector values override script defaults.
4. Keep the sensor ID and edge ID consistent with the map.

The demonstration sensor ID is `corridor-01`.

`YOUR_SENSOR_KEY` and `YOUR_AP_PASSWORD` are configuration placeholders. Configure your local copies before hardware testing and keep personal credentials out of public commits.

## Optional Python Server

The Server folder provides login and map endpoints.

1. Install Python 3.10 or later.
2. Open the Server folder.
3. Run `CREATE_USER.bat` to create a local account.
4. Run `START_SERVER.bat` to start the server.
5. Connect the server computer and application device to a network where they can reach each other.
6. Configure the application’s Server URL and enable server login.

The direct ESP32 hotspot demonstration can use the local review account without the Python server.

Local databases and private configuration should remain excluded from Git.

## Android Demonstration

1. Configure the sensor key and endpoint before building.
2. Build the Android application from Unity.
3. Install the APK on a compatible Android phone.
4. Install or update Google Play Services for AR if prompted.
5. Allow camera access.
6. Connect to the sensor’s Wi-Fi network for hardware testing.
7. Log in and select a destination.
8. Align at the matching physical checkpoint and facing direction.
9. Follow the camera-view arrows and navigation instructions.

## How Dynamic Rerouting Works

The building is represented as a graph:

- Nodes represent mapped locations and junctions.
- Edges represent permitted connections between locations.
- A sensor is associated with a particular corridor edge.
- A blocked corridor becomes unavailable for route planning.
- A* searches for an alternative route to the selected destination.

A* evaluates nodes using:

`f(n) = g(n) + h(n)`

Where:

- `g(n)` is the accumulated cost from the starting node.
- `h(n)` estimates the remaining cost to the destination.
- `f(n)` combines both values to prioritise the search.

An alternative route is possible only if the map contains another available connection.

## Demonstrated Behaviour

During project demonstrations:

- Desktop navigation and destination arrival were observed.
- Android camera-view arrows were displayed.
- Position values and remaining distance changed during movement.
- Sensor status changed between CLEAR and BLOCKED.
- Route updates were observed during blockage testing.

These are demonstration observations, not a formal performance benchmark.

## Scope and Limitations

- Initial location alignment uses a known checkpoint and facing direction.
- Wi-Fi carries sensor data; it does not automatically determine the user’s indoor position.
- The current hardware demonstration monitors one corridor with one sensor.
- Ultrasonic readings indicate objects within the sensor’s measurement area; they do not identify every possible corridor hazard.
- AR tracking depends on lighting, visible features and device compatibility.
- Floor maps, dimensions and checkpoint positions require physical site validation.
- Multi-floor map data does not establish that staircase navigation has been fully tested.
- The project is an academic prototype, not a certified emergency navigation system.

## Future Improvements

- Additional sensors for more corridors.
- Improved checkpoint alignment and tracking recovery.
- Further physical testing across floors.
- Accessible-route preferences.
- Stronger authentication and encrypted communication for deployment.
- Measured evaluation of tracking accuracy and rerouting latency.