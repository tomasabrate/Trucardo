"""
LA MANO DE DIOS  (pelota de futbol low poly + mano con la bandera + nubes)

Unidades: metros, tamano real (pelota de 22 cm de diametro; con las nubes
mide ~34 cm de ancho y ~23 cm de alto). Z hacia arriba. La pelota mira hacia
-Y (la mano queda de frente en la vista frontal de Blender, tecla 1 del
teclado numerico). El origen esta en el centro de la base, sobre las nubes.

La mano con la bandera es una TEXTURA (sin relieve) que el propio script
genera por codigo: no hace falta ningun archivo de imagen. La textura tambien
lleva los paneles negros y blancos de la pelota y las costuras.

Un solo objeto "ManoDeDios" con 4 materiales:
    Balon (con la textura), Nube_A, Nube_B, Nube_C (tres tonos de nube).

USO EN BLENDER (2.8 o superior):
    Pestana Scripting > Open > este archivo > Run Script (Alt+P).
    Para exportar a Unity, pone una ruta en FBX_PATH mas abajo, por ejemplo:
    FBX_PATH = "C:/Users/TuUsuario/Desktop/ManoDeDios.fbx"
    Junto al .fbx se guarda tambien la textura (<nombre>_Textura.png) y queda
    embebida en el .fbx.

Fuera de Blender (python manodedios_lowpoly.py) solo imprime un resumen.
"""
import math
import os
import random

import numpy as np

# ----------------------------------------------------------------- ajustes
BALL_R = 0.11          # radio de la pelota (m)
BALL_ZC = 0.1181       # altura del centro de la pelota sobre la base (m)
BALL_SUBDIV = 0        # 0 = 180 triangulos (facetada como la foto), 1 = 720 triangulos
CLOUD_SUBDIV = 1       # 0 = nubes mas toscas, 1 = nubes mas facetadas
TEX_W, TEX_H = 2048, 1024   # textura: mitad izquierda = frente, derecha = dorso
SEED = 5
FBX_PATH = ""          # ruta para exportar .fbx desde Blender ("" = no exportar)
TEXTURE_PATH = ""      # ruta extra para guardar la textura ("" = junto al .fbx)

MATERIALS = [          # nombre, color sRGB, rugosidad
    ("Balon",  "#FFFFFF", 0.55),   # el color real viene de la textura
    ("Nube_A", "#F4F7FA", 0.9),
    ("Nube_B", "#D9E5EF", 0.9),
    ("Nube_C", "#B7CFE3", 0.9),
]
BALON, NUBES = 0, (1, 2, 3)

# colores de la textura
C_BLACK = "#33322F"
C_WHITE = "#EDEDED"
C_SEAM = "#9B9B9B"
C_CELESTE = "#6BAEDB"
C_BANDA = "#F6F6F6"
C_OUTLINE = "#4F7FA6"
C_SUN_RAY = "#E0AC2C"
C_SUN_DISC = "#EDB92E"
C_SUN_RING = "#C9921B"
C_SUN_FACE = "#A56F18"

# Contorno de la mano (medido sobre la foto): coordenadas normalizadas al radio
# de la pelota, (x a la derecha, z hacia arriba), con (0, 0) en el centro.
HAND = [
    (0.0608, 0.6833), (-0.0008, 0.6861), (-0.0442, 0.6385), (-0.0777, 0.3095),
    (-0.0735, 0.1695), (-0.0889, 0.1723), (-0.1043, 0.2255), (-0.1659, 0.5531),
    (-0.2205, 0.6035), (-0.2723, 0.5923), (-0.3045, 0.5377), (-0.2905, 0.3571),
    (-0.2471, 0.1065), (-0.2513, -0.0502), (-0.2625, -0.1062), (-0.2863, -0.1524),
    (-0.3325, -0.1440), (-0.4375, 0.0226), (-0.5117, 0.0716), (-0.5943, 0.0660),
    (-0.6209, 0.0282), (-0.6069, -0.0208), (-0.4781, -0.2252), (-0.4249, -0.3456),
    (-0.3101, -0.5094), (-0.1589, -0.6227), (0.0034, -0.6073), (0.1266, -0.6227),
    (0.2358, -0.5695), (0.2932, -0.4674), (0.3212, -0.3610), (0.3688, -0.0502),
    (0.4766, 0.2073), (0.5088, 0.3165), (0.4976, 0.3753), (0.4458, 0.3949),
    (0.4038, 0.3697), (0.2904, 0.1205), (0.2582, 0.0744), (0.2358, 0.0828),
    (0.2708, 0.2409), (0.3114, 0.5377), (0.2820, 0.5965), (0.2218, 0.6077),
    (0.1770, 0.5545), (0.1406, 0.3501), (0.0944, 0.1807), (0.0818, 0.1793),
    (0.0958, 0.6343),
]
BAND_TOP, BAND_BOTTOM = 0.217, -0.308   # bandas de la bandera (z normalizado)
OUTLINE_W = 4.0                          # grosor del contorno de la mano (texels)
SUN_CENTER = (0.0087, -0.0411)           # centro del Sol de Mayo (normalizado)
SUN_RHO = 0.098                          # radio del disco del Sol (normalizado)
SUN_ROUT = 0.238                         # radio de los rayos (normalizado)

# Banco de nubes: x, y, radio, altura de la parte de arriba (m)
CLOUDS_MAIN = [
    (-0.105, -0.095, 0.050, 0.038), (-0.035, -0.115, 0.052, 0.034),
    (0.040, -0.112, 0.054, 0.036), (0.108, -0.090, 0.050, 0.040),
    (-0.122, -0.030, 0.050, 0.056), (0.125, -0.030, 0.050, 0.058),
    (-0.095, 0.060, 0.056, 0.066), (0.000, 0.105, 0.058, 0.062),
    (0.100, 0.060, 0.056, 0.066),
]
CLOUDS_SMALL = [
    (-0.143, -0.065, 0.030, 0.034), (-0.140, 0.020, 0.030, 0.040),
    (0.147, -0.060, 0.032, 0.036), (0.142, 0.025, 0.030, 0.040),
    (-0.072, -0.138, 0.034, 0.026), (0.078, -0.138, 0.034, 0.026),
]

PHI = (1 + 5 ** 0.5) / 2


# ------------------------------------------------------------- utilidades
def hex_to_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def add(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1],
            a[2] * b[0] - a[0] * b[2],
            a[0] * b[1] - a[1] * b[0])


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def unit(a):
    l = math.sqrt(dot(a, a)) or 1.0
    return (a[0] / l, a[1] / l, a[2] / l)


def face_normal(a, b, c):
    return unit(cross(sub(b, a), sub(c, a)))


def signed_volume(verts, faces):
    return sum(dot(verts[a], cross(verts[b], verts[c])) for a, b, c in faces) / 6.0


# --------------------------------------------- pelota: icosaedro truncado
def _cyc(v):
    x, y, z = v
    return [(x, y, z), (z, x, y), (y, z, x)]


def _signs(v):
    return [(v[0] * sx, v[1] * sy, v[2] * sz)
            for sx in (1, -1) for sy in (1, -1) for sz in (1, -1)]


def _dedup(points):
    seen, out = set(), []
    for p in points:
        k = tuple(round(c, 6) + 0.0 for c in p)
        if k not in seen:
            seen.add(k)
            out.append(p)
    return out


def _truncated_icosahedron():
    """Devuelve (vertices unitarios, paneles). Cada panel = (lados, centro, anillo)."""
    base = [(0, 1, 3 * PHI), (1, 2 + PHI, 2 * PHI), (PHI, 2, 2 * PHI + 1)]
    verts = _dedup([s for b in base for c in _cyc(b) for s in _signs(c)])
    assert len(verts) == 60
    verts = [unit(v) for v in verts]
    pent = _dedup([s for c in _cyc((0, 1, PHI)) for s in _signs(c)])
    hexa = _dedup([s for s in _signs((1, 1, 1))] +
                  [s for c in _cyc((0, PHI, 1 / PHI)) for s in _signs(c)])
    assert len(pent) == 12 and len(hexa) == 20
    panels = []
    for sides, centers in ((5, pent), (6, hexa)):
        for c in centers:
            c = unit(c)
            d = [dot(v, c) for v in verts]
            mx = max(d)
            idx = [i for i, x in enumerate(d) if x > mx - 1e-6]
            assert len(idx) == sides
            a = (1, 0, 0) if abs(c[0]) < 0.9 else (0, 1, 0)
            e1 = unit(cross(c, a))
            e2 = cross(c, e1)
            idx.sort(key=lambda i: math.atan2(dot(verts[i], e2), dot(verts[i], e1)))
            panels.append((sides, c, idx))
    return verts, panels, [unit(p) for p in pent]


def build_ball():
    """Devuelve (vertices, triangulos, uvs por triangulo, panel de cada triangulo,
    lados de cada panel)."""
    verts, panels, pent = _truncated_icosahedron()

    # Orientacion: un pentagono al centro mirando a -Y y otro pentagono vecino
    # justo arriba (como en la foto: 5 pentagonos negros alrededor de la mano).
    c = pent[0]
    p = next(q for q in pent if abs(dot(q, c) - 1 / 5 ** 0.5) < 1e-6)
    up = unit(sub(p, tuple(dot(p, c) * k for k in c)))
    ay = tuple(-k for k in c)
    ax = cross(ay, up)

    def rot(v):
        return (dot(v, ax), dot(v, ay), dot(v, up))

    verts = [rot(v) for v in verts]
    tris, kinds = [], []
    for pid, (sides, cen, ring) in enumerate(panels):
        ci = len(verts)
        verts.append(rot(cen))
        kinds.append(sides)
        for k in range(sides):
            tris.append((ci, ring[k], ring[(k + 1) % sides], pid))

    for _ in range(BALL_SUBDIV):
        cache, new = {}, []

        def mid(i, j):
            key = (min(i, j), max(i, j))
            if key not in cache:
                cache[key] = len(verts)
                verts.append(unit(add(verts[i], verts[j])))
            return cache[key]

        for a, b, cc, pid in tris:
            ab, bc, ca = mid(a, b), mid(b, cc), mid(cc, a)
            new += [(a, ab, ca, pid), (ab, b, bc, pid),
                    (ca, bc, cc, pid), (ab, bc, ca, pid)]
        tris = new

    faces, ids, uvs = [], [], []
    for a, b, cc, pid in tris:
        va, vb, vc = verts[a], verts[b], verts[cc]
        cen = ((va[0] + vb[0] + vc[0]) / 3, (va[1] + vb[1] + vc[1]) / 3,
               (va[2] + vb[2] + vc[2]) / 3)
        if dot(face_normal(va, vb, vc), cen) < 0:
            b, cc = cc, b
            vb, vc = vc, vb
        front = cen[1] < 0
        if front:   # proyeccion plana desde el frente -> mitad izquierda
            uv = tuple((0.25 + v[0] / 4, 0.5 + v[2] / 2) for v in (va, vb, vc))
        else:       # proyeccion plana desde atras -> mitad derecha
            uv = tuple((0.75 - v[0] / 4, 0.5 + v[2] / 2) for v in (va, vb, vc))
        faces.append((a, b, cc))
        ids.append(pid)
        uvs.append(uv)
    world = [(BALL_R * v[0], BALL_R * v[1], BALL_R * v[2] + BALL_ZC) for v in verts]
    return world, faces, uvs, ids, kinds


# ------------------------------------------------------------ nubes
def _icosphere(sub_levels):
    t = PHI
    v = [(-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t),
         (0, -1, -t), (0, 1, -t), (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1)]
    v = [unit(p) for p in v]
    f = [(0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11),
         (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8),
         (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9),
         (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1)]
    for _ in range(sub_levels):
        cache, new = {}, []

        def mid(i, j):
            key = (min(i, j), max(i, j))
            if key not in cache:
                cache[key] = len(v)
                v.append(unit(add(v[i], v[j])))
            return cache[key]

        for a, b, c in f:
            ab, bc, ca = mid(a, b), mid(b, c), mid(c, a)
            new += [(a, ab, ca), (ab, b, bc), (ca, bc, c), (ab, bc, ca)]
        f = new
    return v, f


def build_blob(cx, cy, r, top, levels, rnd):
    """Nube: elipsoide aplastado, con vertices irregulares y base plana en z=0."""
    v, f = _icosphere(levels)
    rz = 0.55 * r
    zc = top - rz
    out = []
    for p in v:
        k = 1 + rnd.uniform(-0.14, 0.14)
        out.append((cx + r * p[0] * k, cy + r * p[1] * k,
                    max(0.0, zc + rz * p[2] * k)))
    if signed_volume(out, f) < 0:
        f = [(a, c, b) for a, b, c in f]
    return out, f


# ------------------------------------------------------------ malla completa
def build_mesh():
    """Devuelve (vertices, caras, material por cara, uv por cara, info_pelota)."""
    rnd = random.Random(SEED)
    verts, faces, fmats, uvs = [], [], [], []

    bv, bf, buv, ids, kinds = build_ball()
    base = len(verts)
    verts += bv
    faces += [(a + base, b + base, c + base) for a, b, c in bf]
    fmats += [BALON] * len(bf)
    uvs += buv

    blobs = [(b, CLOUD_SUBDIV) for b in CLOUDS_MAIN] + \
            [(b, max(0, CLOUD_SUBDIV - 1)) for b in CLOUDS_SMALL]
    for (cx, cy, r, top), lv in blobs:
        v, f = build_blob(cx, cy, r, top, lv, rnd)
        base = len(verts)
        verts += v
        for a, b, c in f:
            n = face_normal(v[a], v[b], v[c])
            shade = 0 if n[2] > 0.45 else (1 if n[2] > -0.15 else 2)
            if rnd.random() < 0.18:
                shade = min(2, max(0, shade + rnd.choice((-1, 1))))
            faces.append((a + base, b + base, c + base))
            fmats.append(NUBES[shade])
            uvs.append(((0.01, 0.01),) * 3)
    return verts, faces, fmats, uvs, (buv, ids, kinds)


# ------------------------------------------------------------ textura
def _poly_inside(px, py, poly):
    inside = np.zeros(px.shape, bool)
    j = len(poly) - 1
    for i in range(len(poly)):
        xi, yi = poly[i]
        xj, yj = poly[j]
        cond = ((yi > py) != (yj > py)) & \
               (px < (xj - xi) * (py - yi) / (yj - yi + 1e-12) + xi)
        inside ^= cond
        j = i
    return inside


def _dist_to_poly(px, py, poly):
    d = np.full(px.shape, 1e9)
    for i in range(len(poly)):
        x0, y0 = poly[i]
        x1, y1 = poly[(i + 1) % len(poly)]
        dx, dy = x1 - x0, y1 - y0
        t = np.clip(((px - x0) * dx + (py - y0) * dy) / (dx * dx + dy * dy + 1e-12), 0, 1)
        d = np.minimum(d, np.hypot(px - (x0 + t * dx), py - (y0 + t * dy)))
    return d


def _raster_ids(tri_uvs, tri_ids):
    """Pinta en un mapa el panel al que pertenece cada texel (-1 = vacio)."""
    idmap = np.full((TEX_H, TEX_W), -1, np.int16)
    for uv, pid in zip(tri_uvs, tri_ids):
        pts = [(u * TEX_W, v * TEX_H) for u, v in uv]
        x0 = max(int(math.floor(min(p[0] for p in pts))) - 1, 0)
        x1 = min(int(math.ceil(max(p[0] for p in pts))) + 1, TEX_W - 1)
        y0 = max(int(math.floor(min(p[1] for p in pts))) - 1, 0)
        y1 = min(int(math.ceil(max(p[1] for p in pts))) + 1, TEX_H - 1)
        jj, ii = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        (ax, ay), (bx, by), (cx, cy) = pts
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-9:
            continue
        l0 = ((by - cy) * (jj - cx) + (cx - bx) * (ii - cy)) / den
        l1 = ((cy - ay) * (jj - cx) + (ax - cx) * (ii - cy)) / den
        l2 = 1 - l0 - l1
        m = (l0 >= -0.03) & (l1 >= -0.03) & (l2 >= -0.03)
        idmap[y0:y1 + 1, x0:x1 + 1][m] = pid
    return idmap


def _draw_hand(tex):
    half = TEX_W // 4          # 512: texels por radio de pelota en la mitad frontal
    cj, ci = TEX_W // 4, TEX_H // 2
    poly = [(cj + x * half, ci + z * half) for x, z in HAND]
    jmin = int(min(p[0] for p in poly)) - 8
    jmax = int(max(p[0] for p in poly)) + 8
    imin = int(min(p[1] for p in poly)) - 8
    imax = int(max(p[1] for p in poly)) + 8
    jj, ii = np.meshgrid(np.arange(jmin, jmax) + 0.5, np.arange(imin, imax) + 0.5)

    cov = sum(_poly_inside(jj + dx, ii + dy, poly)
              for dx, dy in ((-.25, -.25), (.25, -.25), (-.25, .25), (.25, .25))) / 4.0
    d = _dist_to_poly(jj, ii, poly)
    t = np.clip(d - OUTLINE_W + 0.5, 0, 1)[..., None]
    zn = (ii - ci) / half
    celeste = np.array(hex_to_rgb(C_CELESTE), np.float32)
    banda = np.array(hex_to_rgb(C_BANDA), np.float32)
    outline = np.array(hex_to_rgb(C_OUTLINE), np.float32)
    flag = np.where(((zn > BAND_TOP) | (zn < BAND_BOTTOM))[..., None], celeste, banda)
    col = outline * (1 - t) + flag * t
    region = tex[imin:imax, jmin:jmax]
    a = cov[..., None]
    region[:] = region * (1 - a) + col * a

    # Sol de Mayo: 16 rayos rectos + 16 rayos ondulados, disco con cara
    sj, si = cj + SUN_CENTER[0] * half, ci + SUN_CENTER[1] * half
    rho, rout = SUN_RHO * half, SUN_ROUT * half
    dx, dy = jj - sj, ii - si
    r = np.hypot(dx, dy)
    th = np.arctan2(dy, dx) % (2 * math.pi)
    rin = rho * 0.92
    sector = math.pi / 8
    hw = 0.42 * (2 * math.pi * rin / 32) * (1 - (r - rin) / (rout - rin))
    lat1 = (((th + sector / 2) % sector) - sector / 2) * r
    lat2 = (((th % sector) - sector / 2) * r) - 3.0 * np.sin(2 * math.pi * (r - rin) / 26.0)
    c_in = np.clip(r - rin + .5, 0, 1)
    ray1 = np.clip(hw - np.abs(lat1) + .5, 0, 1) * c_in * np.clip(rout - r + .5, 0, 1)
    ray2 = np.clip(0.95 * hw - np.abs(lat2) + .5, 0, 1) * c_in * np.clip(rout - 6 - r + .5, 0, 1)
    rays = np.maximum(ray1, ray2)[..., None]
    region[:] = region * (1 - rays) + np.array(hex_to_rgb(C_SUN_RAY), np.float32) * rays
    disc = np.clip(rho - r + .5, 0, 1)[..., None]
    region[:] = region * (1 - disc) + np.array(hex_to_rgb(C_SUN_DISC), np.float32) * disc
    ring = (np.clip(rho - r + .5, 0, 1) * np.clip(r - (rho - 3.0) + .5, 0, 1))[..., None]
    region[:] = region * (1 - ring) + np.array(hex_to_rgb(C_SUN_RING), np.float32) * ring

    def ellipse(ex, ey, axx, ayy):
        return np.clip(1.0 - np.sqrt(((dx - ex * rho) / (axx * rho)) ** 2 +
                                     ((dy - ey * rho) / (ayy * rho)) ** 2), 0, 1) * 6
    face = np.minimum(1.0, ellipse(-0.34, 0.16, 0.17, 0.085) + ellipse(0.34, 0.16, 0.17, 0.085) +
                      ellipse(0.0, -0.45, 0.24, 0.065) +
                      (np.abs(dx) < 0.05 * rho) * (dy > -0.22 * rho) * (dy < 0.12 * rho))
    face = (face * (r < rho - 3))[..., None]
    region[:] = region * (1 - face) + np.array(hex_to_rgb(C_SUN_FACE), np.float32) * face


def make_texture(ball_info):
    """Devuelve la textura (TEX_H, TEX_W, 4) float32, fila 0 = abajo."""
    tri_uvs, tri_ids, kinds = ball_info
    idmap = _raster_ids(tri_uvs, tri_ids)
    kind_arr = np.array(list(kinds) + [6])          # el indice -1 cae en el ultimo (blanco)
    is_pent = kind_arr[idmap] == 5
    tex = np.empty((TEX_H, TEX_W, 3), np.float32)
    tex[:] = hex_to_rgb(C_WHITE)
    tex[is_pent] = hex_to_rgb(C_BLACK)
    r1, r2 = np.roll(idmap, 1, 0), np.roll(idmap, 1, 1)
    seam = ((idmap >= 0) & (r1 >= 0) & (idmap != r1)) | ((idmap >= 0) & (r2 >= 0) & (idmap != r2))
    seam |= np.roll(seam, -1, 0) | np.roll(seam, -1, 1)
    tex[seam] = hex_to_rgb(C_SEAM)
    _draw_hand(tex)
    rgba = np.ones((TEX_H, TEX_W, 4), np.float32)
    rgba[..., :3] = tex
    return rgba


# ---------------------------------------------------------- verificacion
def check_mesh(verts, faces):
    edges = {}
    for f in faces:
        for i in range(3):
            e = (f[i], f[(i + 1) % 3])
            edges[e] = edges.get(e, 0) + 1
    closed = all(c == 1 and edges.get((b, a)) == 1 for (a, b), c in edges.items())
    xs, ys, zs = zip(*verts)
    return {
        "triangulos": len(faces),
        "vertices": len(verts),
        "cerrada_y_consistente": closed,
        "volumen_firmado_cm3": round(signed_volume(verts, faces) * 1e6, 1),
        "medidas_cm": (round((max(xs) - min(xs)) * 100, 1),
                       round((max(ys) - min(ys)) * 100, 1),
                       round((max(zs) - min(zs)) * 100, 1)),
        "z_min_cm": round(min(zs) * 100, 2),
    }


# ---------------------------------------------------------------- Blender
def build_in_blender(verts, faces, fmats, uvs, tex):
    import bpy

    mesh = bpy.data.meshes.new("ManoDeDios")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    uv_layer = mesh.uv_layers.new(name="UVMap")
    for poly in mesh.polygons:
        for k, li in enumerate(poly.loop_indices):
            uv_layer.data[li].uv = uvs[poly.index][k]
    obj = bpy.data.objects.new("ManoDeDios", mesh)
    bpy.context.scene.collection.objects.link(obj)

    # Textura generada por codigo
    img = bpy.data.images.new("ManoDeDios_Textura", TEX_W, TEX_H, alpha=False)
    img.pixels.foreach_set(tex.ravel())
    img.update()
    tex_path = TEXTURE_PATH or (os.path.splitext(FBX_PATH)[0] + "_Textura.png" if FBX_PATH else "")
    if tex_path:
        img.filepath_raw = tex_path
        img.file_format = "PNG"
        img.save()
    else:
        img.pack()

    for i, (mname, hx, rough) in enumerate(MATERIALS):
        lin = tuple(srgb_to_linear(c) for c in hex_to_rgb(hx)) + (1.0,)
        mat = bpy.data.materials.new(mname)
        mat.use_nodes = True
        nt = mat.node_tree
        bsdf = nt.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Roughness"].default_value = rough
            if i == BALON:
                node = nt.nodes.new("ShaderNodeTexImage")
                node.image = img
                nt.links.new(node.outputs["Color"], bsdf.inputs["Base Color"])
            else:
                bsdf.inputs["Base Color"].default_value = lin
        mat.diffuse_color = lin
        mesh.materials.append(mat)

    for poly, m in zip(mesh.polygons, fmats):
        poly.material_index = m
        try:
            poly.use_smooth = False
        except Exception:
            pass

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    if FBX_PATH:
        bpy.ops.export_scene.fbx(
            filepath=FBX_PATH, use_selection=True, object_types={"MESH"},
            apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
            axis_forward="-Z", axis_up="Y", bake_space_transform=True,
            mesh_smooth_type="FACE", path_mode="COPY", embed_textures=True)
    return obj


def _in_blender():
    try:
        import bpy  # noqa: F401
        return hasattr(bpy.context, "scene") and bpy.context.scene is not None \
            and bpy.app.background is False
    except ImportError:
        return False


if __name__ == "__main__":
    V, F, M, UV, INFO = build_mesh()
    if _in_blender():
        build_in_blender(V, F, M, UV, make_texture(INFO))
    else:
        print(check_mesh(V, F))
