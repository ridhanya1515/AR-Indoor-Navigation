# College merge record

Source: your uploaded IndoorNavigation.zip. Active map: IndoorNavigation/Generated/MainBlock_Map.json.

All 116 nodes, 118 connections, 45 destination flags and original coordinates from that map are retained. Edge IDs were added for the new sensor client. Original blocked flags and stair flags are preserved. See COLLEGE_DESTINATIONS.csv for the complete active destination list.

The latest map contains Main Block and other campus blocks together. The earlier CSE layout occupies the same footprint and is NOT overlaid on the latest Main Block. Earlier-only destinations: Library, Reading Room, CS5, CSE Lab. Those remain in the original backup and map files, not the active destination list. Temple and other decorative landmarks are shown where the source places them, but a landmark without a source navigation node is not assigned an invented route.

The original upload is preserved byte-for-byte as OriginalAssets_Backup.zip. College/temple OBJ models, materials, prefabs and the latest scene's geometry are included. Old application, camera and sensor behaviours were removed from the geometry scene to prevent two navigation systems running. The authored floor-visibility tags remain. Materials use the included compatible unlit shader; appearance needs checking in Unity.

The editor setup creates a CollegeGeometry prefab from the cleaned scene and assigns it to the app. PC simulation shows the college geometry. Phone AR shows the real camera and route arrows; the college geometry does not cover the camera. The 2D map uses source building footprints, graph links, landmark labels and floor buttons. All 45 destinations remain searchable regardless of the displayed map floor.

One fixed sensor uses corridor-01, edge Junction_MainNorth, matching the source's first sensor binding. Flash the firmware in THIS package: the earlier demonstration firmware used a different edge ID. The second example sensor mapping is not enabled because you have one board. Stale/disconnected hardware excludes the configured edge. For software-only PC testing disable ESP32 in Settings.

Starting position is selected manually at a checkpoint. All checkpoint headings are +Z (map north), which must be marked/verified on site. Wi-Fi transports obstacle status; it does not determine the user's indoor position. Existing source dimensions are approximate and other-block rooms were described as proposed. No new surveying is claimed.
