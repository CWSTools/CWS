# ICCCE Runtime

Place the slim ICCCE runtime here for bundled CWS releases.

Required entry:

- `InkCanvasForClass.exe`

CWS copies this folder to the application output as `ICCCE\` and calls:

- `InkCanvasForClass.exe --show`
- `InkCanvasForClass.exe --board`
- `InkCanvasForClass.exe icc://...`

Keep only the runtime files required by ICCCE to start and handle protocol commands. Do not place ICCCE source code in this folder.
