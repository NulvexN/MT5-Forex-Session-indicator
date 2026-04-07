<div align="center">

# Session Highlighter MT5/MT4

Modern trading session visualization tool with real-time chart rendering, session overlays, and a custom-built desktop interface. Designed for traders who want clean, fast, and informative session-based analysis.

<br>

[![Platform](https://img.shields.io/badge/MT5%20%2F%20MT4-Compatible-4D9FFF?style=for-the-badge)](https://www.metatrader5.com)
[![UI](https://img.shields.io/badge/Interface-Modern%20Desktop-00D4AA?style=for-the-badge)]
[![Status](https://img.shields.io/badge/Status-Active-00D4AA?style=for-the-badge)]
[![License](https://img.shields.io/badge/License-MIT-4D9FFF?style=for-the-badge)](LICENSE)

</div>

---

## Screenshot

<div align="center">

<p align="center">
  <img src="https://i.ibb.co/N2bTxGrY/v.png" alt="photo" width="820">
</p>

</div>

---

## 🎬 Demo

<div align="center">

<img src="https://i.imgur.com/8pBsuyr.gif" alt="Demo">

</div>

---

## Overview

Session Highlighter is a visual trading tool focused on highlighting major market sessions directly on the chart.

It provides a clean and responsive interface to track:

- London session  
- New York session  
- Asia session  
- Session overlaps  

All rendered in real-time with a custom chart engine.

---

## What It Does

This project simulates a full session-based trading environment with visual feedback.

| Module | Description |
|------|--------|
| Session Engine | Calculates and displays session time ranges |
| Chart Renderer | Custom candlestick + grid system |
| Session Overlay | Colors session zones directly on chart |
| Overlap Detection | Highlights London–NY overlap |
| Live Price Feed | Simulated real-time price movement |
| Volume Layer | Optional volume visualization |

---

## Features

| Feature | Description |
|--------|------------|
| Real-Time Chart | Smooth custom-rendered candles |
| Session Zones | Asia, London, New York overlays |
| Overlap Highlight | Special zone for session intersections |
| Live Price Line | Dynamic price tracking |
| Volume Bars | Optional volume visualization |
| Pair Selector | EURUSD, GBPUSD, USDJPY, etc. |
| Timeframes | M1, M5, M15, M30, H1, H4 |
| Session Cards | Detailed stats per session |
| Status Tracking | Active / Upcoming / Closed |
| Visibility Controls | Toggle sessions and volume |

---

## Session Logic

Each session is mapped to chart time blocks:

- Asia → 00:00 – 09:00  
- London → 08:00 – 17:00  
- New York → 13:00 – 22:00  
- Overlap → 13:00 – 17:00  

Overlap zones are automatically detected and highlighted.

---

## Interface Structure

```
[ Chart Area ]
 ├── Session Zones (colored)
 ├── Candlesticks
 ├── Volume Bars
 └── Live Price Line

[ Session Cards ]
 ├── Status (Active / Upcoming / Closed)
 ├── Avg Range
 ├── Volume %

[ Controls ]
 ├── Pair Selector
 ├── Timeframe Buttons
 ├── Session Toggles
 └── Apply to Chart
```

---

## System Behavior

- Fully self-contained (no external API)
- Real-time UI updates
- Smooth rendering with double buffering
- Session-aware candle coloring
- Dynamic status updates

---

## Quick Start

**Requirements:**
- Windows 10 / 11  
- .NET 6+  
- Visual Studio 2022  

```bash
git clone https://github.com/your-username/session-highlighter.git
```

Open solution → Press **F5**

---

## How to Use

1. Launch application  
2. Select trading pair  
3. Choose timeframe  
4. Enable / disable sessions  
5. Click **Apply to Chart**  
6. Monitor sessions visually  

---

## Visual Behavior

- Blue → London  
- Orange → New York  
- Purple → Asia  
- Green → Overlap  

Candles adapt to session color dynamically.

---

## Roadmap

- [x] Custom chart engine  
- [x] Session overlays  
- [x] Overlap detection  
- [x] UI dashboard  
- [ ] MT5 real integration  
- [ ] Indicator export (MQ5 / MQ4)  
- [ ] Strategy signals  
- [ ] Multi-symbol tracking  

---

## Contributing

```
1. Fork repository
2. Create feature branch
3. Commit changes
4. Push branch
5. Open Pull Request
```

---

## License

MIT

---

<div align="center">

Session Highlighter MT5/MT4 · v1.0

</div>
```
