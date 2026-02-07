# AGENTS.md

## Project Mission
Build FrameSnap: a reliable Windows fixed-aspect screenshot utility.

`REQUIREMENTS.md` is the source of truth for product and technical scope. If implementation details conflict with old notes, follow `REQUIREMENTS.md`.

## Engineering Priorities
1. Coordinate correctness across mixed-DPI multi-monitor setups.
2. Fast and predictable capture UX.
3. Reliability of global hotkey and overlay lifecycle.
4. Minimal background footprint for tray app behavior.

## Decision Baseline
- Language/runtime: C# on .NET 8.
- Desktop framework: WPF.
- Primary capture backend: Windows Graphics Capture.
- Packaging during early development: unpackaged desktop app.

## Working Rules For Agents
- Keep changes small and testable; do not refactor broadly without direct value.
- Treat DPI/coordinate math as high-risk code and document assumptions inline.
- Keep capture rectangles and crop math in physical pixels.
- Preserve single active capture session; never allow stacked overlays.
- Avoid adding external dependencies unless they remove clear implementation risk.
- Any new feature must map to a requirement or an explicit new milestone.

## Implementation Checklist
Use this checklist to keep delivery on track.

### Phase M0: Shell + Overlay Skeleton
- [x] Create solution and project structure for tray utility.
- [x] Implement tray icon with menu: Capture, Ratio submenu, Exit.
- [x] Implement `HotkeyManager` with default `Ctrl+Shift+S`.
- [x] Implement `OverlayWindow` full-screen dim layer and cancel path.
- [x] Wire capture request flow from tray and hotkey to overlay.

### Phase M1: Core Capture
- [x] Implement ratio model with presets and custom `W:H` parsing.
- [x] Implement rectangle algorithm (center anchor, clamped to monitor).
- [x] Implement monitor detection and physical-pixel coordinate pipeline.
- [x] Implement `CaptureEngine` using Windows Graphics Capture.
- [x] Implement crop + clipboard output path.
- [x] Add re-entry guard so only one capture session can run.

### Phase M2: Stability + Persistence
- [x] Implement `SettingsStore` for ratio and output mode persistence.
- [x] Implement save-to-disk output mode and naming convention.
- [x] Harden overlay cleanup with try/finally and global exception handling.
- [x] Verify hotkey behavior after sleep/resume and app restart.
- [x] Add basic user feedback for success/failure (status text or toast).


### Phase M3: Windowed Control Surface
- [x] Add a windowed control UI with snipping-tool-inspired layout while preserving tray and hotkey entry points.
- [x] Wire window controls for capture start, ratio selection, and output mode selection.
- [x] Surface capture status feedback in the window status area.
- [x] Remove right-side status panel from the window UI.
- [x] Open or restore the control window on tray icon left-click.

### Phase M4: Branding Polish
- [x] Add FrameSnap logo asset and wire it to app, window, and tray icons.

### Validation Checklist (Required Before Calling MVP Done)
- [ ] Test on single monitor at 100% DPI.
- [ ] Test on multi-monitor with at least one non-100% DPI monitor.
- [ ] Test with monitor layouts that include negative coordinates.
- [ ] Verify ratio accuracy within +/-1 px for all presets.
- [ ] Verify clipboard paste in Paint and browser input target.
- [ ] Verify `Esc` always cancels and removes overlay.

## Task Management Protocol
- Before implementing, identify target milestone (`M0`, `M1`, `M2`, or `M3`).
- During implementation, update checklist items in this file as completed.
- In each summary to the user, include:
  - milestone being advanced,
  - items completed,
  - any blockers or open decisions.

## Definition of Done (Per Task)
A task is done only if:
- Code compiles.
- Behavior is manually exercised for the affected flow.
- Edge cases relevant to DPI/coordinates were considered.
- The checklist state in `AGENTS.md` is updated.
- User-facing summary includes what changed and how it was verified.

## Open Decisions Log
Track unresolved choices here and resolve before M2 freeze.
- [ ] Keep WPF long-term or migrate shell to WinUI 3.
- [ ] Add GDI fallback for capture backend failures.
- [ ] Finalize notification approach (in-app status vs system toast).
