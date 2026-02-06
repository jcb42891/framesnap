Tech Spec: Aspect-Ratio Snip (Windows)
1) Goal

Build a lightweight Windows screenshot app similar to Snipping Tool, but optimized for consistent framing:

User selects an aspect ratio (e.g., 1:1, 16:9, 4:3, 9:16, custom W:H).

Instead of click-dragging a rectangle, the app shows a fixed-ratio capture box that follows the cursor.

User clicks once to capture the region under the box.

2) Non-goals (v1)

Video capture / screen recording

OCR, text extraction

Annotation editor (can be v1.1)

Cloud sync / sharing / account system

3) UX / User Flow
Primary flow (one-click snip)

User triggers capture via:

Hotkey (default: Ctrl+Shift+S) OR tray menu “Capture…”

Screen dims; an overlay appears.

A capture rectangle of the chosen aspect ratio appears and follows the mouse.

User moves to position, then:

Left click = capture and copy to clipboard

Enter = capture

Esc = cancel

After capture:

Optional toast “Copied to clipboard” + buttons: Save / Open / Copy path (optional)

Secondary flows

Change aspect ratio from tray menu / small toolbar

Toggle “Remember last ratio”

“Fixed size mode” (optional): aspect ratio + fixed pixel size (e.g., 1080×1080)

4) Feature Scope (MVP)
4.1 Capture modes

Region capture (fixed ratio) (required)

Optional: fullscreen capture (nice-to-have)

Optional: active window capture (nice-to-have)

4.2 Aspect ratio options

Presets: 1:1, 16:9, 4:3, 3:2, 9:16, 21:9

Custom ratio input: W:H (integers)

Optional: “Lock to nearest monitor bounds” behavior

4.3 Capture rectangle behavior

Rectangle anchored on cursor:

Cursor is the center (default) or top-left anchor (configurable)

Rectangle is sized to the largest that fits on screen up to a max dimension OR uses last used size.

Must never extend beyond the current monitor bounds; clamp position.

4.4 Output

Copy captured image to clipboard (required)

Save to file (optional in MVP, but recommended)

Default location: Pictures\AspectSnips\YYYY-MM\…

Naming: snip_YYYYMMDD_HHMMSS.png

Optional: open in default image viewer

4.5 Settings

Remember last ratio

Include cursor? (off by default)

Default output behavior: clipboard only vs clipboard+save

Hotkey customization (optional)

5) Recommended Tech Stack
Option A (recommended): .NET 8 + WinUI 3 (Windows App SDK)

Good for modern UI + fast iteration, with native Windows APIs available.

UI: WinUI 3

Overlay: borderless, topmost window

Global hotkey: Win32 RegisterHotKey

Capture: Windows Graphics Capture (Windows.Graphics.Capture) via WinRT interop

Image encoding: Windows.Graphics.Imaging or System.Drawing.Common alternatives (prefer WinRT imaging to avoid GDI pitfalls)

Option B: WPF (.NET 8) + Win32 + Windows Graphics Capture

Simpler overlay work; very stable for desktop utilities.

Why Windows Graphics Capture?

Modern, fast, DPI-aware, supports multi-monitor better than older GDI-only approaches.

(If you want the absolute simplest first cut, you can do GDI BitBlt, but DPI/multi-monitor edge cases show up quickly.)

6) Architecture Overview
Processes / components

Single-process tray app.

Modules

TrayApp / Shell

Starts on login (optional)

Tray icon + context menu (ratio select, capture, settings, exit)

HotkeyManager

Registers/unregisters global hotkey(s)

Raises CaptureRequested

OverlayWindow

Full-screen per monitor (or one spanning virtual screen)

Renders dim background + capture rectangle

Handles mouse move, click, key events

CaptureEngine

Given a rectangle in screen coordinates (physical pixels), captures image

Returns bitmap buffer

OutputService

Clipboard writer

File saver

Notification/toast

Data model
CaptureSettings
- AspectRatio: (W, H)
- AnchorMode: Center | TopLeft
- PreferredSizePx: (optional width/height) OR MaxSizePolicy
- OutputMode: ClipboardOnly | ClipboardAndSave
- SaveFolder
- Hotkey

7) Coordinate Systems & DPI (critical)

You must handle:

Multiple monitors with different DPI scaling

Negative coordinates (monitors left/up of primary)

Converting between:

DIPs (UI coords)

Screen coords (Win32)

Physical pixels (what capture APIs need)

Approach

Use Win32 APIs to get monitor bounds in physical pixels:

GetCursorPos, MonitorFromPoint, GetMonitorInfo

Per-monitor DPI: GetDpiForMonitor (or GetDpiForWindow if overlay per monitor)

Ensure your overlay window uses per-monitor DPI awareness v2 in app manifest.

All capture rectangles should ultimately be computed and stored in physical pixel coordinates.

8) Capture Rectangle Algorithm
Inputs

Aspect ratio: rw:rh

Cursor position: (x, y) in physical pixels

Current monitor bounds: (left, top, right, bottom) in physical pixels

Size policy:

Max-fit: pick the largest rectangle (by area) that fits within monitor while honoring ratio, then clamp position.

Fixed-size: use stored widthPx and compute heightPx = widthPx * rh / rw (or vice versa)

Max-fit policy (simple + nice UX)

Decide a default max dimension relative to monitor (example: 40% of min(monitorWidth, monitorHeight)).

Compute (w, h) that matches aspect.

Rectangle anchor:

Center anchor: left = x - w/2, top = y - h/2

Clamp left/top so rect stays within monitor bounds

Clamp
left = clamp(left, monitorLeft, monitorRight - w)
top  = clamp(top,  monitorTop,  monitorBottom - h)

Edge cases

If ratio is extreme (e.g., 100:1), ensure minimum width/height (e.g., 20px) and clamp.

If monitor is too small for min size, reduce size.

9) Overlay Rendering
Visuals

Full-screen translucent dark overlay

Capture rectangle:

Bright border

Optional “rule of thirds” grid toggle

Label showing WxH px and ratio (optional)

Input

Mouse move updates rectangle

Left click: capture

Right click: open quick ratio picker (optional)

ESC cancels

Wheel could resize rectangle (optional v1.1)

Performance

Render at 60fps without heavy allocations

Avoid per-frame bitmap operations; just draw vectors

10) Capture Engine Details
Preferred: Windows Graphics Capture

Two implementation patterns:

Capture from monitor output then crop region

Or capture full desktop and crop (less efficient)

For MVP practicality:

Capture the monitor containing the rectangle using Graphics Capture, then crop the region from the resulting frame.

Steps

Identify monitor for rectangle

Start capture session for that monitor

Acquire one frame

Crop to rectangle relative to monitor origin

Encode to PNG (or keep as BGRA for clipboard)

Clipboard

Put PNG or DIB on clipboard

Ensure correct alpha handling (PNG is fine)

Save

Encode PNG

Write async to disk

11) Error Handling / Reliability

If capture API fails (permissions, OS version issues), fallback to GDI BitBlt for region (optional).

Ensure overlay always closes on exceptions (try/finally).

Prevent multiple simultaneous overlays (debounce hotkey).

12) Security / Privacy

All processing local

No network calls

If adding telemetry later, make opt-in

13) Testing Plan
Functional

Ratio presets & custom parsing

Click capture returns correct size ratio (within ±1px due to rounding)

Multi-monitor: left/right/top/bottom arrangements

Mixed DPI monitors (100% + 150% scaling)

Negative virtual coords

Regression checklist

Hotkey works after sleep/resume

Overlay cancels cleanly

Clipboard contains image in common apps (Paint, Slack, Chrome paste)

14) Milestones
M0 — Skeleton

Tray app + ratio selection + hotkey

Overlay window shows and tracks cursor

M1 — Capture works

Compute fixed-ratio rect + click capture

Copy to clipboard

M2 — Polish

Save to file + toast

Settings persistence

DPI/multi-monitor hardening

M3 (optional)

Resize with wheel

Fixed pixel presets (e.g., 1080×1080)

Annotation/open editor

15) Implementation Notes (practical choices)

Start with WPF if you want fastest path to a reliable overlay.

Use Windows Graphics Capture if you want clean DPI/multi-monitor behavior long-term.

Treat “coordinate correctness” as the main engineering risk—build a small diagnostic overlay that prints cursor coords, monitor bounds, and computed rectangle in pixels to validate early.

If you tell me whether you want WPF or WinUI 3, I can turn this into:

a file-by-file project layout,

concrete class interfaces,

and a first-pass “MVP task list” you can drop into Linear/Jira.