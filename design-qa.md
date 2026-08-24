# Design QA

## Reference

- Source visual:
  `H:\AI_HUB\ТЗ\Материалы\Анализ_изображений\2026-08-24_набросок_рабочего_экрана.png`
- Implemented surface:
  `H:\AI_HUB\Исходники\AIHub\Controls\ImageAnalysisWorkspaceControl.xaml`
- Implementation screenshots: supplied by the user during manual 1080p/4K and
  real-model testing on 2026-08-24; temporary chat attachments were reviewed
  but are not stored as project sources.

## Target state

- Desktop WPF workspace for the single-image literary description scenario.
- Permanent image card and stage navigation remain outside three physically
  separate work areas; there is no shared body scroller.
- Central work area contains contextual actions, version selection, revision,
  preview, export and completion controls instead of model output.
- The right rail contains only a non-scrolling short review summary produced
  by the core; it never displays the internal Kimi report or a literary-text
  excerpt.
- Left rail shows a chronological program-event tree with hover and keyboard
  details, activation, hidden scrollbar and automatic follow-to-end.
- The existing Sandbox Matrix overlay is reused for active model work with
  role colors: core green, visual analyst blue, localizer red.
- Completed work exposes `Новый анализ` and `На главную`.

## Static checks

- Source hierarchy and bindings reviewed.
- Debug and Release builds completed without errors or warnings for
  `0.1.1-dev`.
- Debug and Release automated tests completed successfully: `275/275`.
- Localization key parity and JSON parsing completed successfully.
- Strict UTF-8 validation completed successfully for `755` text files.
- No invented raster or vector assets were added.

## Visual comparison

The user completed the required manual run because Codex was not allowed to
start AI HUB. Supplied screenshots confirmed the permanent image card, fixed
stage navigation, physically separate event/action/review areas, absence of a
shared body scrollbar, compact right summary and separate full-text preview.
The user also confirmed that the current screen looks and works acceptably for
this project stage.

## Manual acceptance result

- Multiple real-model runs completed without a visible layout failure.
- The 1080p working state fits without a shared page scrollbar.
- The right rail remains a compact semantic check rather than a literary-text
  viewer.
- Full output opens in the separate preview surface.
- Further reference-image testing and small accuracy polish are future work,
  not blockers for this visual contract.

final result: passed

Evidence limit: keyboard-only behavior and automated pixel comparison were not
independently run by Codex; acceptance is based on the user's manual tests and
screenshots.
