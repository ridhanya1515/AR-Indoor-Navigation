# Review workflow coverage

Reference: INDOOR_NAVIGATION PROJECT(6).pptx, workflow slide 13, inspected 13 October 2026.

| Requirement | Review implementation |
|---|---|
| Login first | Fixed local account student / Campus123!; optional laptop database login retained |
| Map after login | Bundled illustrative map; optional protected server map |
| Destination selection | Searchable room list; tapping a room opens camera/alignment |
| Current position | Explicit checkpoint and facing-direction alignment, followed by phone AR pose |
| Wi-Fi/Bluetooth automatic localization | Not implemented; one sensor does not supply building-wide positioning |
| A* | Graph route with blocked edges excluded |
| Live arrows and turns | World-space blue arrows; camera-relative turn text and remaining route distance |
| Sensor | One fixed HC-SR04, one ESP32 hotspot, authenticated HTTP polling |
| Blockage | Below 60 cm blocks; above 80 cm clears, with filtering |
| Reroute | Automatic on graph changes while navigation remains active, same destination |
| Wrong turn | Sustained route deviation triggers recalculation |
| No path | No-route message and Help; no invented shortcut |
| Arrival | Same-floor proximity, arrival message, arrows hidden |

Local review credentials are demonstration authentication, not a production account system. Read VALIDATION.md before claiming a successful physical demonstration.
