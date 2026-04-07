# Skin Tone Analyzer

OpenCV + MediaPipe pipeline that extracts face ROI, analyzes lighting, smooths skin tone, and prints JSON output for app integration.

## Current Structure

```text
PyCharmMiscProject/
  main.py
  core/
    config.py
    frame_processor.py
    json_output.py
  analysis/
    lighting.py
    tone.py
  vision/
    camera.py
    face.py
    roi.py
  models/
    face_landmarker.task
```

## Recommended Rule

- Use the project root tree as the source of truth: `main.py`, `core/`, `analysis/`, `vision/`, `models/`.
- Keep generated outputs and caches out of version control.

## Quick Start

```bash
python -m pip install -r requirements.txt
python main.py
```

## JSON Output Shape

- `timestamp`
- `face_detected`
- `lighting` (`status`, `score`)
- `skin_tone` (`r`, `g`, `b`)
- `personal_color` (`type`, `lab`)

## Next Extensions

- Add skin texture features with `scikit-image` under `analysis/texture.py`.
- Add unit tests for pure functions (`analyze_lighting`, `rgb_to_lab`, seasonal classifier).

