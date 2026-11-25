# Validation record

## Executed in the delivery workspace

- Python 3 server and map test suite: **15 tests passed**.
- Tests cover successful login, rejected credentials, protected map fetch, expired/revoked sessions, password hashing, malformed login requests, rate limiting, invalid map rejection, every destination's connectivity, alternate route around the sensor edge, no-route behavior, both staircase alternatives, sensor IDs, and bundled/server map consistency.
- `SensorLogic.h` compiled with g++ using `-std=c++11 -Wall -Wextra -pedantic`; its **10 assertions passed**. This checks the actual firmware's shared threshold/hysteresis state code, including invalid echoes. It does not compile the complete Arduino sketch or emulate its electrical timing.

## Supplied for execution in Unity

Menu **Campus Navigation → 4 - Run routing tests** runs the actual C# NavGraph checks, four local-login checks, and bundled map validation. Standalone source is also in `Tests/NavGraphTests.cs`.

These C# tests were not executed here: this workspace has no Unity or C# compiler. A Unity editor build and a physical phone run are still required. No APK is included and no successful Unity compilation is claimed.

## Required phone and hardware acceptance tests

| Test | Expected observation | Result |
|---|---|---|
| Unity compilation/build | No red compile errors; APK build succeeds | Not run |
| Local review login | Correct account opens map; wrong password rejected | Not run |
| Destination flow | Room selection opens camera; alignment starts route | Not run |
| Camera permission and ARCore | Live camera; SessionTracking obtained | Not run |
| Translation | X/Z change when physically stepping | Not run |
| Rotation | Heading-dependent instruction changes while turning | Not run |
| Alignment | Arrows line up with measured corridor/floor | Not run |
| Distance | Remaining route distance decreases toward target | Not run |
| Wrong turn | Same destination, updated route after sustained deviation | Not run |
| Physical sensor clear/blocked | Valid status changes from measured objects | Not run |
| Sensor-driven detour | Blocked edge excluded, alternative arrows shown | Not run |
| Sensor disconnected | Stale/unknown displayed; configured edge excluded | Not run |
| No route | Assistance text, no unsafe arrow path | Not run |
| Tracking loss/pause | Alignment required; route arrows hidden | Not run |
| Both staircases/all floors | Correct landing confirmation and floor route | Not run |
| Arrival | Reached message only near correct destination/floor | Not run |
| Real site survey | Rooms, corridors, stairs and sensor placement verified | Not run |

Record device, app version, date, location, observed error, latency and pass/fail during your tests. Do not copy the desktop test results as proof of physical AR navigation accuracy.

## Re-run the portable tests

From the package root:

```text
python -m unittest discover -s Tests -p "test_*.py" -v
```

If g++ is installed:

```text
g++ -std=c++11 -Wall -Wextra -pedantic Tests/test_sensor.cpp -o sensor-test
./sensor-test
```

For a standalone C# compiler, compile `NavGraph.cs` with `Tests/NavGraphTests.cs`; no Unity assemblies are required for these two files.

## Review update scope

This source package implements the revised local login → map → destination → camera → checkpoint alignment → navigation flow. One fixed ESP32 sensor controls one mapped edge. It uses manual starting-point alignment and AR tracking, not automatic Wi-Fi localization. The sample map is illustrative, not a surveyed college floor plan. The local review account is not production authentication.

## College merge validation

15 Python map/server tests passed on the merged college data. Every current destination is preserved. Alternative route around Junction_MainNorth and each staircase alternative checked independently. Original C# menu tests passed on the user PC BEFORE this merge only. Modified C# and geometry prefab creation need to be run in Unity. See COLLEGE_MERGE.md for scope.
