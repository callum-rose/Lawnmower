#!/bin/bash
# raster.sh in.svg out.png scale
in="$1"; out="$2"; s="${3:-3}"
w=$(grep -o 'width="[0-9.]*"' "$in" | head -1 | grep -o '[0-9.]*'); h=$(grep -o 'height="[0-9.]*"' "$in" | head -1 | grep -o '[0-9.]*')
W=$(python3 -c "import math;print(math.ceil($w*$s))"); H=$(python3 -c "import math;print(math.ceil($h*$s))")
tmp=$(mktemp -d)
cp "$in" "$tmp/a.svg"
cat > "$tmp/a.html" <<H
<html><head><style>html,body{margin:0;padding:0;background:transparent;overflow:hidden}img{display:block;width:${W}px;height:${H}px}</style></head><body><img src="a.svg"></body></html>
H
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new --disable-gpu --hide-scrollbars --force-device-scale-factor=1 --default-background-color=00000000 --window-size=$W,$H --screenshot="$out" "file://$tmp/a.html" >/dev/null 2>&1
rm -rf "$tmp"
echo "$out ${W}x${H}"
