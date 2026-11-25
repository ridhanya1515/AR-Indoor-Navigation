# Surveying and authoring the map

## Coordinate convention

Use metres: X is right/east on the plan, Z is the chosen forward/north direction, and Y is height. Ground floor is floor index 0. Do not use phone GPS latitude/longitude as these coordinates.

1. Pick a permanent, identifiable entrance point as (0, 0, 0).
2. Mark a clear +Z facing direction there. It need not be geographic north, but it must be consistent.
3. Measure corridor centreline points at doors, corners, intersections and stair landings.
4. Put a node at every turn. Straight edges must stay wholly inside a walkable corridor; do not connect across walls.
5. Place destination nodes at reachable door entrances, not room centres behind a wall.
6. Enter actual floor heights and staircase endpoint coordinates. The sample's 3.5 m storey height is only a placeholder.
7. Mark repeated alignment checkpoints at known positions. Record the facing yaw in degrees; 0 faces +Z and 90 faces +X.
8. Check every route on site and validate the placement of both stairs.

Example node:

```json
{"id":"Office","label":"Office","x":-2,"y":0,"z":8,"floor":0,"destination":true,"checkpoint":false,"yaw":0}
```

Example corridor edge:

```json
{"id":"F0_BC","a":"F0_B","b":"F0_C","multiplier":1,"stairs":false,"closed":false}
```

Edges are bidirectional. `multiplier` is at least 1, preserving the A* Euclidean heuristic. Use `closed: true` for a manually closed path. Sensor closures are additional temporary exclusions. Do not use a reduced weight below 1. A path can cross floors only with `stairs: true`.

The sample graph represents two vertical stair connections. A real stairwell with turns should have its measured intermediate geometry represented appropriately; verify arrow placement at landings. Floor changes are explicitly confirmed by the user near the target landing. This build does not infer floors from a barometer or beacon.

## Corridor attachment and rerouting

The user must remain within 1.25 m of the nearest mapped corridor segment. This is a localization tolerance, not a measured building width. Tune it only after measuring your corridors and testing AR drift.

The planner attaches to the geometrically nearest corridor before choosing a path. It does not choose a farther corridor merely because the route would be shorter. A blocked edge cannot be used to attach to a distant endpoint. This avoids falsely routing through a blocked corridor, but may require the user to return to a known checkpoint when already inside that corridor.

If two physically separate corridors are closer than the tolerance, insert additional checkpoints and use more precise localization before deploying. This project has no mesh-based wall collision or automatic semantic understanding of the camera scene.

## Sensor mapping

```json
{"id":"corridor-01","edgeId":"F0_BC","url":"http://192.168.1.45/status","enabled":true,"failClosed":true}
```

Firmware `SENSOR_ID` and `EDGE_ID` must match. The runtime Settings screen edits the first sensor's URL/enabled state for convenience. For multiple ESP32 devices, configure multiple sensor objects in JSON with distinct IDs and URLs on the same router. Use the same sensor key configured in the app for this demonstration implementation.

Each physical sensor has one fixed monitored edge. If multiple sensors monitor the same edge, any blocked or fail-closed unknown sensor excludes it. A built-in `closed` edge cannot be reopened by a sensor clear reading.

Do not carry a fixed-edge sensor around while keeping its old edge ID: that would report obstacles at the wrong place. A mobile obstacle sensor needs an independently verified position-to-edge association and is outside this implementation.

## Optional floor-plan assets

The mini map is drawn directly from the graph. The AR camera shows the real building, so photorealistic building meshes are not required. If importing the old college mesh for visualization, use a separate view and verify its scale and orientation; do not assume its surfaces establish live AR localization.

The included `Demo_Floorplan.svg` is an exact visualization of the sample JSON, not a surveyed architectural plan.
