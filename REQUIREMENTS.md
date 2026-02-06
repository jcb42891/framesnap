# FrameSnap Requirements

## 1. Product Definition
FrameSnap is a lightweight Windows desktop utility for fixed-aspect screenshot capture.

The app behavior is:
- User starts capture from a global hotkey or tray menu.
- A dimmed overlay appears on the active monitor.
- A fixed-ratio capture rectangle follows the cursor.
- User clicks once (or presses Enter) to capture.
- Result is copied to clipboard immediately.

## 2. Target Users
- Designers, marketers, and developers who repeatedly capture consistent aspect-ratio images.
- Users replacing repetitive manual drag-snipping workflows.

## 3. Platform and Runtime
- OS: Windows 11 (primary), Windows 10 22H2+ (best effort).
- App type: Desktop tray utility.
- Runtime: .NET 8.
- UI framework: WPF.
- Packaging: unpackaged during development; MSIX optional after MVP stabilization.

## 4. Non-Goals (v1)
- Video capture or recording.
- OCR/text extraction.
- In-app annotation editor.
- Cloud sync, sharing backend, or user accounts.

## 5. Functional Requirements (MVP)
### 5.1 Capture Entry
The app must support:
- Global hotkey `Ctrl+Shift+S` by default.
- Tray menu action `Capture`.

### 5.2 Overlay and Interaction
When capture starts, the app must:
- Show a topmost translucent overlay.
- Track cursor movement at interactive frame rate.
- Draw a fixed-aspect rectangle that stays within monitor bounds.
- Support input:
  - `Left Click`: capture.
  - `Enter`: capture.
  - `Esc`: cancel.

### 5.3 Aspect Ratio Management
The app must provide these preset ratios:
- `1:1`, `16:9`, `4:3`, `3:2`, `9:16`, `21:9`.

The app must support:
- Custom ratio input (`W:H`, positive integers).
- Persisting the last selected ratio.

### 5.4 Rectangle Behavior
MVP anchor mode:
- `Center` anchor only.

MVP size policy:
- `Max-fit default size` based on current monitor.
- Rectangle position is clamped so it never crosses monitor bounds.

### 5.5 Output Behavior
On successful capture, the app must:
- Copy image to clipboard.
- Encode as PNG-compatible clipboard content.

MVP optional but expected in first stable release:
- Save PNG to disk when output mode is `ClipboardAndSave`.
- Default save path: `%USERPROFILE%\Pictures\AspectSnips\YYYY-MM`.
- Filename format: `snip_YYYYMMDD_HHMMSS.png`.

### 5.6 Settings
The app must persist:
- Selected aspect ratio.
- Output mode (`ClipboardOnly` or `ClipboardAndSave`).

The app should persist:
- Last used capture size.

## 6. Technical Requirements
### 6.1 Architecture
Single process with these components:
- `TrayShell`: tray icon, menu, lifecycle.
- `HotkeyManager`: register/unregister global hotkey.
- `OverlayWindow`: capture UI and input handling.
- `CaptureEngine`: screen-frame acquisition and crop.
- `OutputService`: clipboard and file save operations.
- `SettingsStore`: read/write user settings.

### 6.2 Capture Backend
- Primary backend: Windows Graphics Capture.
- Capture one frame from the monitor containing the target rectangle.
- Crop in physical pixels.

### 6.3 Coordinate and DPI Correctness
The app must:
- Run as Per-Monitor DPI Aware v2.
- Correctly support mixed DPI multi-monitor setups.
- Correctly support negative virtual-screen coordinates.
- Store and process final capture rectangles in physical pixels.

### 6.4 Reliability
The app must:
- Prevent concurrent capture sessions.
- Always dismiss overlay on completion, cancel, or exception.
- Recover hotkey behavior after sleep/resume where possible.

## 7. Performance Requirements
- Overlay interaction should feel real time (target 60 FPS rendering path).
- Capture-to-clipboard completion target: under 300 ms on typical hardware for standard regions.
- No per-frame heavy bitmap allocations in overlay rendering.

## 8. Security and Privacy
- No network calls in MVP.
- All image processing remains local.
- No telemetry in MVP.

## 9. Validation and Acceptance Criteria
MVP is accepted when all are true:
- Capture can be triggered from hotkey and tray.
- Preset ratios and custom ratio are usable.
- Output dimensions match selected ratio within +/-1 px rounding tolerance.
- Clipboard paste works in at least Paint and one browser-based app.
- Multi-monitor capture works with at least one mixed-DPI configuration.
- Cancel path (`Esc`) always exits cleanly.

## 10. Delivery Milestones
- `M0`: tray shell, hotkey, overlay skeleton.
- `M1`: fixed-ratio rectangle math and successful clipboard capture.
- `M2`: settings persistence, save-to-disk mode, reliability hardening.
- `M3` (optional): wheel-resize, fixed pixel presets, annotation handoff.

## 11. Open Decisions (Track Before M2)
- Keep WPF long-term or migrate UI shell to WinUI 3.
- Add GDI fallback for unsupported capture environments.
- Add toast notifications in-app vs Windows notification API.
