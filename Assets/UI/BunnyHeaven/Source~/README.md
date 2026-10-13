# Bunny Heaven UI kit sources

Unity ignores this folder (`~` suffix).

- `figma/`: SVGs exported from the Figma file `bjqyzxfUn51rb8y7P6sXZK` (section `12:2`, style D) with the Figma MCP server.
- `build_sprites.py`: splits each hand-drawn frame into a white body (ink outline baked in) and a white shadow so the theme can tint them, whitens the icons and doodle pattern, adds the undo/redo/menu/lock icons that weren't exported, and rasterises everything into `../Sprites` with headless Chrome (`raster.sh`).

  `python3 build_sprites.py <scratch-dir>`

Then, in the editor: **Bunny Heaven > UI Kit > Rebuild Everything** (sprite import settings, TMP font assets, theme, prefabs).
