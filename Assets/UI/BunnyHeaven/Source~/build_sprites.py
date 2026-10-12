import re, os, sys, subprocess, json
HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, 'figma')
OUT = os.path.join(HERE, '..', 'Sprites')
STAGE = sys.argv[1]
RASTER = os.path.join(os.path.dirname(__file__), 'raster.sh')
os.makedirs(STAGE, exist_ok=True)

def paths(name):
    s = open(f'{RAW}/{name}.svg').read()
    w, h = re.search(r'width="([\d.]+)" height="([\d.]+)"', s).groups()
    return float(w), float(h), re.findall(r'<path[^>]*/>', s)

def whiten(p):
    p = re.sub(r'fill="(?!none)[^"]*"', 'fill="white"', p)
    return p

def svg(w, h, body, viewbox=None):
    vb = viewbox or f'0 0 {w:g} {h:g}'
    vw, vh = [float(x) for x in vb.split()[2:]]
    return f'<svg width="{vw:g}" height="{vh:g}" viewBox="{vb}" fill="none" xmlns="http://www.w3.org/2000/svg">{body}</svg>'

meta = {}
def emit(folder, name, content, scale, info=None):
    os.makedirs(f'{OUT}/{folder}', exist_ok=True)
    sp = f'{STAGE}/{name}.svg'
    open(sp, 'w').write(content)
    subprocess.run([RASTER, sp, f'{OUT}/{folder}/{name}.png', str(scale)], check=True, stdout=subprocess.DEVNULL)
    meta[f'{folder}/{name}'] = dict(scale=scale, **(info or {}))

S = 3
# frames: name, src, shadow?, border pts (l,b,r,t)
frames = [
    ('Card', 'settings_row_card', True, (40, 34, 40, 30)),
    ('ModalCard', 'modal_card', True, (46, 46, 46, 46)),
    ('Button', 'bigbutton_green_play', True, (46, 40, 46, 36)),
    ('ButtonSmall', 'bigbutton_blue', True, (36, 30, 36, 28)),
    ('Pill', 'pill_level', True, (34, 32, 34, 28)),
    ('Circle', 'round64_blue', True, None),
    ('Chip', 'settings_iconchip_blue', False, None),
    ('StatCircle', 'stat_circle', False, None),
    ('Ribbon', 'ribbon_red', True, (20, 20, 20, 20)),
    ('Banner', 'banner_green', False, (40, 40, 40, 40)),
    ('SegmentTrack', 'seg_track_graphics', False, (26, 20, 26, 20)),
    ('SegmentOn', 'seg_on_swipe', False, (22, 18, 22, 18)),
]
for name, src, has_shadow, border in frames:
    w, h, ps = paths(src)
    if has_shadow:
        emit('Frames', f'Frame_{name}_Shadow', svg(w, h, whiten(ps[0])), S, dict(border=border))
        body = ps[1:]
    else:
        body = ps
    emit('Frames', f'Frame_{name}', svg(w, h, ''.join(whiten(p) for p in body)), S, dict(border=border))

# toggle: track + knob
w, h, ps = paths('toggle_on')
emit('Frames', 'Toggle_Track', svg(w, h, whiten(ps[0])), S, dict(border=(20, 18, 20, 18)))
emit('Frames', 'Toggle_Knob', svg(w, h, whiten(ps[1]), '31 7 27 26'), S)
# slider: track + knob
w, h, ps = paths('slider_music')
track = re.sub(r'stroke="[^"]*"', 'stroke="white"', ps[0])
emit('Frames', 'Slider_Track', svg(w, h, track, '2 11 134 14'), S, dict(border=(10, 0, 10, 0)))
emit('Frames', 'Slider_Knob', svg(w, h, whiten(ps[2]), '69.5 4.8 26 27'), S)

# doodle pattern
s = open(f'{RAW}/menu_doodle.svg').read()
s = re.sub(r'(stroke|fill)="#[0-9A-Fa-f]{6}"', r'\1="white"', s).replace('opacity="0.55"', '')
s = re.sub(r'preserveAspectRatio="none" overflow="visible" style="display: block;" ', '', s)
open(f'{STAGE}/doodle_src.svg', 'w').write(s)
emit('Patterns', 'DoodlePattern', s, 2)

# icons
icons = {
    'play': 'icon_play', 'levels': 'icon_grid', 'settings': 'icon_gear', 'howtoplay': 'icon_book',
    'restart': 'icon_restart', 'pause': 'icon_pause', 'check': 'icon_check', 'back': 'icon_back',
    'next': 'icon_next_white', 'flag': 'icon_flag', 'sparkle': 'icon_sparkle', 'move': 'icon_move',
    'vibrate': 'icon_vibrate', 'music': 'icon_music', 'sound': 'icon_sound', 'trash': 'icon_trash',
}
def white_icon(s):
    s = re.sub(r'(stroke|fill)="#[0-9A-Fa-f]{6}"', r'\1="white"', s)
    return re.sub(r'preserveAspectRatio="none" overflow="visible" style="display: block;" ', '', s)
for name, src in icons.items():
    s = white_icon(open(f'{RAW}/{src}.svg').read())
    w = float(re.search(r'width="([\d.]+)"', s).group(1))
    emit('Icons', f'Icon_{name}', s, 96 / w, dict(size=w))

# icons not exported from Figma (rate limit): drawn in the same Lucide-style stroke language
stroke = 'stroke="white" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round"'
custom = {
    'undo': f'<path d="M9 14 4 9l5-5" {stroke}/><path d="M4 9h10.5a5.5 5.5 0 0 1 0 11H11" {stroke}/>',
    'redo': f'<path d="m15 14 5-5-5-5" {stroke}/><path d="M20 9H9.5a5.5 5.5 0 0 0 0 11H13" {stroke}/>',
    'menu': f'<path d="M15 21v-8a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v8" {stroke}/><path d="M3 10a2 2 0 0 1 .709-1.528l7-5.999a2 2 0 0 1 2.582 0l7 5.999A2 2 0 0 1 21 10v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" {stroke}/>',
    'lock': f'<rect x="4" y="11" width="16" height="10" rx="2" {stroke}/><path d="M8 11V7a4 4 0 0 1 8 0v4" {stroke}/>',
}
for name, body in custom.items():
    emit('Icons', f'Icon_{name}', svg(24, 24, body), 4, dict(size=24))
meta['Icons/Icon_resume'] = 'alias:Icon_play'
json.dump(meta, open(f'{STAGE}/meta.json', 'w'), indent=1)
print(len(meta), 'sprites')
