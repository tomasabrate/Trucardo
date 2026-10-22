# -*- coding: utf-8 -*-
"""
guitarra_lowpoly.py  --  Guitarra criolla (clasica) low poly para props de Unity.

Uso:
  * En Blender 2.8 o superior: pestana Scripting > abrir este archivo > Run Script.
  * Fuera de Blender (python guitarra_lowpoly.py): no crea nada, solo imprime un
    resumen de la malla (triangulos, medidas en cm, si esta cerrada).

Convenciones:
  * Metros, tamano real (guitarra de 0,99 m de largo total).
  * Eje Z hacia arriba. El origen esta en el centro de la base, que queda en z = 0.
  * Un solo objeto ("Guitarra") con 6 material slots de color plano, flat shading.

Las proporciones salen de la foto de referencia: se enderezo la silueta y se midio
todo como fraccion del largo total (u = 0 en el fondo de la caja, u = 1 en la punta
de la paleta; las mitades de ancho "hw" tambien son fracciones de L).
Lo que la foto no muestra (profundidad de la caja, fondo, inclinacion de la paleta)
esta asumido y marcado con "ASUMIDO".
"""
import math
from collections import defaultdict

try:
    import bpy  # solo existe dentro de Blender
except ImportError:
    bpy = None

# =============================================================== CONFIGURACION
FBX_PATH = ""            # si tiene una ruta (p. ej. r"C:\proy\Assets\guitarra.fbx") exporta un .fbx
OBJECT_NAME = "Guitarra"
POSE = "acostada"        # "acostada": apoyada sobre el fondo, tapa hacia arriba (ASUMIDO)
                         # "parada":   apoyada sobre el borde inferior, tapa hacia -Y
HEAD_TILT_DEG = 12.0     # inclinacion de la paleta hacia atras (ASUMIDO, 0 = plana)

# ============================================================ MEDIDAS (metros)
L = 0.99                 # largo total, de la base de la caja a la punta de la paleta

# --- caja: mitad del contorno (u, hw) medida sobre la silueta de la foto
OUTLINE = [
    (0.0000, 0.0500), (0.0070, 0.0850), (0.0153, 0.1100), (0.0413, 0.1520),
    (0.0817, 0.1848), (0.1230, 0.1980), (0.1567, 0.1987), (0.1922, 0.1917),
    (0.2245, 0.1785), (0.3009, 0.1327), (0.3250, 0.1275), (0.4104, 0.1482),
    (0.4544, 0.1430), (0.4980, 0.1084), (0.5173, 0.0660), (0.5222, 0.0328),
]
BODY_END_U = OUTLINE[-1][0]
DEPTH_TAIL = 0.096       # alto de la caja en el talon (ASUMIDO: guitarra clasica ~9,5 cm)
DEPTH_NECK = 0.080       # alto de la caja junto al mastil (ASUMIDO: ~8 cm)

# --- boca y roseta (centro en el eje)
HOLE_U, HOLE_R, ROSETTE_R = 0.367, 0.046, 0.069
RING_SIDES = 12
HOLE_DEPTH = 0.020       # profundidad visible de la boca (el interior se pinta oscuro)

# --- diapason, cejuela y mastil
FB_U0 = 0.398            # donde termina el diapason sobre la tapa
NUT_U0, NUT_U1 = 0.8424, 0.8495   # cejuela (cara hacia el diapason / cara hacia la paleta)
FB_HW0, FB_HW1 = 0.0353, 0.0265   # mitad de ancho del diapason en FB_U0 y en la cejuela
FB_Z_TOP = 0.0905        # altura de la cara superior del diapason (horizontal)
FB_THICK = 0.0065
NECK_DEPTH = 0.021
SCALE_LEN = 0.655        # largo de cuerda: cejuela a cejilla del puente (ASUMIDO ~65 cm)
N_FRETS = 19

# --- puente
BRIDGE_U = 0.168         # centro del puente
BRIDGE_HL = 0.0965       # mitad del largo (en L)
BRIDGE_HY = 0.0170       # mitad del ancho en y (m)

# --- paleta con dos ranuras y 3 clavijas por lado
HEAD_THICK = 0.016
HEAD_HALF = [(0.0267, 0.8495), (0.0326, 0.8675), (0.0360, 0.9000),
             (0.0363, 0.9808), (0.0120, 1.0000)]      # lado derecho (hw, u)
SLOT_XC, SLOT_HA = 0.01845, 0.00795                    # centro y semiancho de ranura (en L)
SLOT_U0, SLOT_U1, SLOT_R = 0.8675, 0.9500, 0.006
TUNER_U = (0.8853, 0.9156, 0.9459)

# --- materiales (nombre, color sRGB 0-255 tomado de la foto)
MATS = [
    ("Cuerpo_madera", (214, 129, 46)),   # 0 tapa, aros y fondo
    ("Mastil_paleta", (98, 40, 30)),     # 1 mastil y paleta
    ("Diapason",      (78, 74, 72)),     # 2 diapason
    ("Puente_boca",   (42, 29, 26)),     # 3 puente e interior de la boca
    ("Crema",         (232, 221, 203)),  # 4 cejuela, cejilla, trastes, cuerdas, clavijas
    ("Roseta",        (179, 81, 46)),    # 5 anillo de la roseta
]
M_BODY, M_NECK, M_FRET, M_DARK, M_CREAM, M_ROSE = range(6)


# ============================================================ GEOMETRIA 2D
def _cross(o, a, b):
    return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])


def _area2(poly):
    s = 0.0
    for i in range(len(poly)):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % len(poly)]
        s += x1 * y2 - x2 * y1
    return s


def _in_poly(pt, poly):
    x, y = pt
    inside = False
    n = len(poly)
    for i in range(n):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % n]
        if (y1 > y) != (y2 > y):
            if x1 + (y - y1) * (x2 - x1) / (y2 - y1) > x:
                inside = not inside
    return inside


def _seg_cross(p1, p2, p3, p4):
    d1, d2 = _cross(p3, p4, p1), _cross(p3, p4, p2)
    d3, d4 = _cross(p1, p2, p3), _cross(p1, p2, p4)
    return ((d1 > 0 > d2) or (d1 < 0 < d2)) and ((d3 > 0 > d4) or (d3 < 0 < d4))


def _in_tri(p, a, b, c, eps=1e-12):
    return (_cross(a, b, p) >= -eps and _cross(b, c, p) >= -eps and _cross(c, a, p) >= -eps)


def _bridge(P, poly, hole, others):
    """Une un agujero al contorno con un puente (indices repetidos, sin vertices nuevos)."""
    edges = []
    for loop in [poly, hole] + others:
        for i in range(len(loop)):
            edges.append((loop[i], loop[(i + 1) % len(loop)]))
    cands = sorted((math.dist(P[a], P[b]), pi, hi)
                   for pi, a in enumerate(poly) for hi, b in enumerate(hole))
    ppoly = [P[i] for i in poly]
    phole = [P[i] for i in hole]
    for _, pi, hi in cands:
        a, b = poly[pi], hole[hi]
        pa, pb = P[a], P[b]
        if any(_seg_cross(pa, pb, P[e1], P[e2]) for e1, e2 in edges if a not in (e1, e2) and b not in (e1, e2)):
            continue
        mid = ((pa[0] + pb[0]) / 2, (pa[1] + pb[1]) / 2)
        if not _in_poly(mid, ppoly) or _in_poly(mid, phole):
            continue
        if any(_in_poly(mid, [P[i] for i in o]) for o in others):
            continue
        return poly[:pi + 1] + hole[hi:] + hole[:hi] + [hole[hi], a] + poly[pi + 1:]
    raise ValueError("no se pudo unir el agujero al contorno")


def triangulate(outer, holes):
    """Triangula un poligono con agujeros. Devuelve (P, tris): P = outer + holes (en ese
    orden) y tris = triples de indices a P con orientacion antihoraria."""
    P = list(outer)
    starts = []
    for h in holes:
        starts.append(len(P))
        P += list(h)
    poly = list(range(len(outer)))
    if _area2(outer) < 0:
        poly.reverse()
    hole_idx = []
    for k, h in enumerate(holes):
        idx = list(range(starts[k], starts[k] + len(h)))
        if _area2(h) > 0:
            idx.reverse()
        hole_idx.append(idx)
    order = sorted(range(len(holes)), key=lambda k: -max(P[i][0] for i in hole_idx[k]))
    pending = [hole_idx[k] for k in order]
    while pending:
        h = pending.pop(0)
        poly = _bridge(P, poly, h, pending)
    tris = []
    while len(poly) > 3:
        n = len(poly)
        for i in range(n):
            a, b, c = poly[i - 1], poly[i], poly[(i + 1) % n]
            pa, pb, pc = P[a], P[b], P[c]
            if _cross(pa, pb, pc) <= 1e-12:
                continue
            blocked = False
            for j in poly:
                if j in (a, b, c) or P[j] in (pa, pb, pc):
                    continue
                if _in_tri(P[j], pa, pb, pc):
                    blocked = True
                    break
            if not blocked:
                tris.append((a, b, c))
                del poly[i]
                break
        else:
            raise ValueError("fallo al triangular el poligono")
    tris.append(tuple(poly))
    return P, tris


# ================================================================== MALLA
class Mesh:
    def __init__(self):
        self.v, self.f, self.m, self.pieces = [], [], [], []
        self._start = 0

    def vert(self, p):
        self.v.append((float(p[0]), float(p[1]), float(p[2])))
        return len(self.v) - 1

    def verts(self, pts):
        return [self.vert(p) for p in pts]

    def tri(self, a, b, c, mat):
        self.f.append((a, b, c))
        self.m.append(mat)

    def quad(self, a, b, c, d, mat):
        """Un quad si es plano; si no, dos triangulos."""
        pa, pb, pc, pd = (self.v[i] for i in (a, b, c, d))
        u = _sub(pb, pa)
        w = _sub(pc, pa)
        n = _cross3(u, w)
        ln = math.sqrt(_dot(n, n))
        dist = abs(_dot(n, _sub(pd, pa))) / ln if ln > 1e-14 else 0.0
        if dist < 1e-6:
            self.f.append((a, b, c, d))
            self.m.append(mat)
        else:
            self.tri(a, b, c, mat)
            self.tri(a, c, d, mat)

    def poly(self, ids, mat):
        """Cara convexa: tri, quad plano o abanico."""
        if len(ids) == 3:
            self.tri(ids[0], ids[1], ids[2], mat)
        elif len(ids) == 4:
            self.quad(ids[0], ids[1], ids[2], ids[3], mat)
        else:
            for j in range(1, len(ids) - 1):
                self.tri(ids[0], ids[j], ids[j + 1], mat)

    # ---- piezas: cada pieza es un solido cerrado; al cerrarla se orienta hacia afuera
    def begin(self):
        self._start = len(self.f)

    def end(self):
        s, e = self._start, len(self.f)
        if self.signed_volume(range(s, e)) < 0:
            for i in range(s, e):
                self.f[i] = tuple(reversed(self.f[i]))
        self.pieces.append((s, e))

    def signed_volume(self, face_ids):
        vol = 0.0
        for i in face_ids:
            f = self.f[i]
            p0 = self.v[f[0]]
            for j in range(1, len(f) - 1):
                p1, p2 = self.v[f[j]], self.v[f[j + 1]]
                vol += _dot(p0, _cross3(p1, p2))
        return vol / 6.0

    def tube(self, rings, mat):
        """Solido a partir de anillos de k puntos (cada anillo en un plano), con tapas."""
        ids = [self.verts(r) for r in rings]
        k = len(ids[0])
        for r in range(len(ids) - 1):
            for i in range(k):
                j = (i + 1) % k
                self.quad(ids[r][i], ids[r][j], ids[r + 1][j], ids[r + 1][i], mat)
        self.poly(list(reversed(ids[0])), mat)
        self.poly(ids[-1], mat)

    def tri_count(self):
        return sum(len(f) - 2 for f in self.f)

    def apply(self, fn):
        self.v = [fn(p) for p in self.v]


def _sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def _dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def _cross3(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def _norm(a):
    n = math.sqrt(_dot(a, a))
    return (a[0] / n, a[1] / n, a[2] / n)


def box(mesh, x0, x1, y0, y1, z0, z1, mat):
    mesh.begin()
    mesh.tube([[(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)],
               [(x0, y1, z0), (x1, y1, z0), (x1, y1, z1), (x0, y1, z1)]], mat)
    mesh.end()


def wall(mesh, top_ids, bot_ids, mat):
    """Pared entre dos anillos cerrados (contorno antihorario visto desde arriba = hacia afuera)."""
    n = len(top_ids)
    for i in range(n):
        j = (i + 1) % n
        mesh.quad(bot_ids[i], bot_ids[j], top_ids[j], top_ids[i], mat)


def string_prism(mesh, p0, p1, mat, r=0.0014):
    d = _norm(_sub(p1, p0))
    side = _norm(_cross3(d, (0.0, 0.0, 1.0)))
    up = _cross3(side, d)
    rings = []
    for p in (p0, p1):
        ring = []
        for ang in (90.0, 210.0, 330.0):
            a = math.radians(ang)
            ring.append(tuple(p[k] + r * (math.cos(a) * side[k] + math.sin(a) * up[k]) for k in range(3)))
        rings.append(ring)
    mesh.begin()
    mesh.tube(rings, mat)
    mesh.end()


# ============================================================ CONSTRUCCION
def z_top(y):
    """Altura de la tapa en y: la caja se afina del talon al mastil."""
    return DEPTH_TAIL - (DEPTH_TAIL - DEPTH_NECK) * y / (BODY_END_U * L)


def build_mesh():
    mesh = Mesh()
    t_head = math.radians(HEAD_TILT_DEG)

    # ---------- caja (tapa + aros + fondo + boca con roseta)
    half = [(u * L, hw * L) for u, hw in OUTLINE]
    outline = [(hw, y) for y, hw in half] + [(-hw, y) for y, hw in reversed(half)]   # antihorario
    ycen = HOLE_U * L
    ring_r = [(ROSETTE_R * L * math.cos(2 * math.pi * k / RING_SIDES),
               ycen + ROSETTE_R * L * math.sin(2 * math.pi * k / RING_SIDES)) for k in range(RING_SIDES)]
    ring_h = [(HOLE_R * L * math.cos(2 * math.pi * k / RING_SIDES),
               ycen + HOLE_R * L * math.sin(2 * math.pi * k / RING_SIDES)) for k in range(RING_SIDES)]
    mesh.begin()
    P, tris = triangulate(outline, [ring_r])
    top_ids = mesh.verts([(x, y, z_top(y)) for x, y in P])
    for a, b, c in tris:
        mesh.tri(top_ids[a], top_ids[b], top_ids[c], M_BODY)
    n_out = len(outline)
    back_ids = mesh.verts([(x, y, 0.0) for x, y in outline])
    for a, b, c in tris_back(outline):
        mesh.tri(back_ids[a], back_ids[b], back_ids[c], M_BODY)
    wall(mesh, top_ids[:n_out], back_ids, M_BODY)
    # roseta: anillo entre el circulo exterior (ya en la tapa) y el borde de la boca
    r_ids = top_ids[n_out:]
    h_ids = mesh.verts([(x, y, z_top(y)) for x, y in ring_h])
    for k in range(RING_SIDES):
        j = (k + 1) % RING_SIDES
        mesh.quad(r_ids[k], r_ids[j], h_ids[j], h_ids[k], M_ROSE)
    # boca: pared interior y fondo oscuro
    b_ids = mesh.verts([(x, y, z_top(y) - HOLE_DEPTH) for x, y in ring_h])
    for k in range(RING_SIDES):
        j = (k + 1) % RING_SIDES
        mesh.quad(h_ids[k], h_ids[j], b_ids[j], b_ids[k], M_DARK)
    mesh.poly(b_ids, M_DARK)
    mesh.end()

    # ---------- diapason (horizontal, sobre la tapa y el mastil)
    y_f0 = FB_U0 * L
    y_nf = NUT_U0 * L
    zb0 = z_top(y_f0) - 0.001
    zb1 = FB_Z_TOP - FB_THICK
    hw0, hw1 = FB_HW0 * L, FB_HW1 * L

    def hw_fb(y):
        return hw0 + (hw1 - hw0) * (y - y_f0) / (y_nf - y_f0)

    def zb(y):
        return zb0 + (zb1 - zb0) * (y - y_f0) / (y_nf - y_f0)

    mesh.begin()
    mesh.tube([[(-hw0, y_f0, zb0), (hw0, y_f0, zb0), (hw0, y_f0, FB_Z_TOP), (-hw0, y_f0, FB_Z_TOP)],
               [(-hw1, y_nf, zb1), (hw1, y_nf, zb1), (hw1, y_nf, FB_Z_TOP), (-hw1, y_nf, FB_Z_TOP)]], M_FRET)
    mesh.end()

    # ---------- trastes (prismas triangulares, posiciones de 12 notas por octava)
    y_sad = y_nf - SCALE_LEN
    for n in range(1, N_FRETS + 1):
        y = y_nf - SCALE_LEN * (1.0 - 2.0 ** (-n / 12.0))
        w = hw_fb(y) - 0.0004
        tri_yz = [(y - 0.0011, FB_Z_TOP - 0.0004), (y + 0.0011, FB_Z_TOP - 0.0004), (y, FB_Z_TOP + 0.0013)]
        mesh.begin()
        mesh.tube([[(-w, a, b) for a, b in tri_yz], [(w, a, b) for a, b in tri_yz]], M_CREAM)
        mesh.end()

    # ---------- mastil (madera bajo el diapason, entra en la caja)
    y_h = NUT_U1 * L                       # donde empieza la paleta
    rings = []
    for y in (0.47 * L, y_h + 0.010):
        zt = zb(y) + 0.001
        hwt = hw_fb(y) - 0.0015
        hwm, hwb = hwt + 0.0005, hwt * 0.62
        rings.append([(-hwt, y, zt), (hwt, y, zt), (hwm, y, zt - 0.010), (hwb, y, zt - NECK_DEPTH),
                      (-hwb, y, zt - NECK_DEPTH), (-hwm, y, zt - 0.010)])
    mesh.begin()
    mesh.tube(rings, M_NECK)
    mesh.end()

    # ---------- cejuela
    box(mesh, -hw1, hw1, y_nf, y_h, FB_Z_TOP - 0.006, FB_Z_TOP + 0.0045, M_CREAM)

    # ---------- puente + cejilla
    yb = BRIDGE_U * L
    hl = BRIDGE_HL * L
    prof = [(-hl, -0.0015), (hl, -0.0015), (hl, 0.0045), (0.040, 0.0085), (-0.040, 0.0085), (-hl, 0.0045)]
    mesh.begin()
    mesh.tube([[(x, y, z_top(y) + dz) for x, dz in prof] for y in (yb - BRIDGE_HY, yb + BRIDGE_HY)], M_DARK)
    mesh.end()
    zs = z_top(y_sad)
    box(mesh, -0.044, 0.044, y_sad - 0.00175, y_sad + 0.00175, zs + 0.0075, zs + 0.0120, M_CREAM)

    # ---------- paleta (en su marco local: x ancho, y' hacia la punta, z' normal a la cara)
    z_h = FB_Z_TOP - 0.001

    def head_pt(x, yp, zp):
        return (x, y_h + yp * math.cos(t_head) + zp * math.sin(t_head),
                z_h - yp * math.sin(t_head) + zp * math.cos(t_head))

    def hp(u):
        return (u - NUT_U1) * L

    right = [(hw * L, hp(u)) for hw, u in HEAD_HALF]
    head_out = right + [(-x, y) for x, y in reversed(right)]
    slots = []
    for sgn in (-1, 1):
        xc, a, r = sgn * SLOT_XC * L, SLOT_HA * L, SLOT_R
        y0, y1 = hp(SLOT_U0), hp(SLOT_U1)
        slots.append([(xc - a, y0 + r), (xc, y0), (xc + a, y0 + r),
                      (xc + a, y1 - r), (xc, y1), (xc - a, y1 - r)])
    mesh.begin()
    P, tris = triangulate(head_out, slots)
    t_ids = mesh.verts([head_pt(x, y, 0.0) for x, y in P])
    b_ids = mesh.verts([head_pt(x, y, -HEAD_THICK) for x, y in P])
    for a, b, c in tris:
        mesh.tri(t_ids[a], t_ids[b], t_ids[c], M_NECK)
        mesh.tri(b_ids[c], b_ids[b], b_ids[a], M_NECK)
    n0 = len(head_out)
    wall(mesh, t_ids[:n0], b_ids[:n0], M_NECK)
    pos = n0
    for s in slots:
        m = len(s)
        # los agujeros se recorren en sentido horario para que la pared mire hacia la ranura
        order = list(range(pos, pos + m))
        if _area2(s) > 0:
            order.reverse()
        wall(mesh, [t_ids[i] for i in order], [b_ids[i] for i in order], M_NECK)
        pos += m
    mesh.end()

    def head_hw(u):
        pts = HEAD_HALF
        for (h0, u0), (h1, u1) in zip(pts, pts[1:]):
            if u0 <= u <= u1:
                return (h0 + (h1 - h0) * (u - u0) / (u1 - u0)) * L
        return pts[-1][0] * L

    # clavijas: boton al costado + rodillo cruzando la ranura
    roller_x = {}
    for sgn in (-1, 1):
        for k, u in enumerate(TUNER_U):
            yc = hp(u)
            hw_p = head_hw(u)
            xs = sorted([sgn * (hw_p - 0.002), sgn * (hw_p + 0.008)])
            c = [head_pt(x, y, z) for x in xs for y in (yc - 0.010, yc + 0.010) for z in (-0.014, -0.002)]
            mesh.begin()
            mesh.tube([[head_pt(xs[0], yc - 0.010, -0.014), head_pt(xs[1], yc - 0.010, -0.014),
                        head_pt(xs[1], yc - 0.010, -0.002), head_pt(xs[0], yc - 0.010, -0.002)],
                       [head_pt(xs[0], yc + 0.010, -0.014), head_pt(xs[1], yc + 0.010, -0.014),
                        head_pt(xs[1], yc + 0.010, -0.002), head_pt(xs[0], yc + 0.010, -0.002)]], M_CREAM)
            mesh.end()
            xr = sorted([sgn * (SLOT_XC - SLOT_HA) * L - sgn * 0.002, sgn * (SLOT_XC + SLOT_HA) * L + sgn * 0.002])
            mesh.begin()
            mesh.tube([[head_pt(xr[0], yc - 0.0035, -0.010), head_pt(xr[1], yc - 0.0035, -0.010),
                        head_pt(xr[1], yc - 0.0035, 0.002), head_pt(xr[0], yc - 0.0035, 0.002)],
                       [head_pt(xr[0], yc + 0.0035, -0.010), head_pt(xr[1], yc + 0.0035, -0.010),
                        head_pt(xr[1], yc + 0.0035, 0.002), head_pt(xr[0], yc + 0.0035, 0.002)]], M_CREAM)
            mesh.end()
            roller_x[(sgn, k)] = (sgn * SLOT_XC * L, yc)

    # ---------- cuerdas: cejilla -> cejuela -> rodillos (las de adentro van al rodillo mas lejano)
    sp_b, sp_n = 0.01174, 0.00845
    z_sad_top = zs + 0.0120 + 0.0002
    z_nut_top = FB_Z_TOP + 0.0045 + 0.0002
    y_nut_mid = (y_nf + y_h) / 2
    for i in range(6):
        off = i - 2.5
        xb, xn = off * sp_b, off * sp_n
        string_prism(mesh, (xb, y_sad, z_sad_top), (xn, y_nut_mid, z_nut_top), M_CREAM)
        sgn = -1 if i < 3 else 1
        k = 2 - i if i < 3 else i - 3        # 0 = rodillo mas cercano a la cejuela (la cuerda mas interna va al mas cercano)
        xr, yr = roller_x[(sgn, k)]
        string_prism(mesh, (xn, y_nut_mid, z_nut_top), head_pt(xr, yr, 0.0018), M_CREAM)

    # ---------- pose y origen
    y0 = BODY_END_U * L / 2.0               # centro de la base de la caja
    if POSE == "parada":
        mesh.apply(lambda p: (p[0], -(p[2] - DEPTH_TAIL / 2.0), p[1]))
    else:
        mesh.apply(lambda p: (p[0], p[1] - y0, p[2]))
    return mesh


def tris_back(outline):
    """Triangulos del fondo (mirando hacia abajo): misma triangulacion invertida."""
    _, tris = triangulate(outline, [])
    return [(c, b, a) for a, b, c in tris]


# ================================================================ VERIFICACION
def analyze(mesh):
    """Cierre de la malla, volumenes por pieza y medidas."""
    dir_edges = defaultdict(int)
    for f in mesh.f:
        for i in range(len(f)):
            dir_edges[(f[i], f[(i + 1) % len(f)])] += 1
    open_edges = sum(1 for (a, b) in dir_edges if dir_edges.get((b, a), 0) != 1)
    repeated = sum(1 for c in dir_edges.values() if c > 1)
    degenerate = 0
    for f in mesh.f:
        p0 = mesh.v[f[0]]
        for j in range(1, len(f) - 1):
            n = _cross3(_sub(mesh.v[f[j]], p0), _sub(mesh.v[f[j + 1]], p0))
            if math.sqrt(_dot(n, n)) < 1e-10:
                degenerate += 1
    vols = [mesh.signed_volume(range(s, e)) for s, e in mesh.pieces]
    xs, ys, zs_ = zip(*mesh.v)
    return {
        "triangulos": mesh.tri_count(), "caras": len(mesh.f), "vertices": len(mesh.v),
        "piezas": len(mesh.pieces), "aristas_abiertas": open_edges, "aristas_repetidas": repeated,
        "caras_degeneradas": degenerate, "volumen_min_cm3": min(vols) * 1e6,
        "cerrada": open_edges == 0 and repeated == 0,
        "min": (min(xs), min(ys), min(zs_)), "max": (max(xs), max(ys), max(zs_)),
        "dim_cm": ((max(xs) - min(xs)) * 100, (max(ys) - min(ys)) * 100, (max(zs_) - min(zs_)) * 100),
    }


def print_summary(mesh):
    r = analyze(mesh)
    print("=" * 60)
    print("%s  (pose: %s)" % (OBJECT_NAME, POSE))
    print("Triangulos: %d  |  caras: %d  |  vertices: %d" % (r["triangulos"], r["caras"], r["vertices"]))
    print("Medidas (X ancho, Y largo, Z alto): %.1f x %.1f x %.1f cm" % r["dim_cm"])
    print("z minima: %.4f m (la base debe quedar en 0)" % r["min"][2])
    print("Piezas solidas: %d | volumen minimo: %.1f cm3 (todas positivas: %s)"
          % (r["piezas"], r["volumen_min_cm3"], r["volumen_min_cm3"] > 0))
    print("Malla cerrada: %s (aristas abiertas: %d, repetidas: %d, caras degeneradas: %d)"
          % ("SI" if r["cerrada"] else "NO", r["aristas_abiertas"], r["aristas_repetidas"], r["caras_degeneradas"]))
    print("Material slots: %d" % len(MATS))
    print("=" * 60)
    return r


# ==================================================================== BLENDER
def _srgb_to_linear(c):
    c = c / 255.0
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def create_blender_object(mesh):
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    old = bpy.data.objects.get(OBJECT_NAME)
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    me = bpy.data.meshes.new(OBJECT_NAME + "_malla")
    me.from_pydata(mesh.v, [], [list(f) for f in mesh.f])
    for name, rgb in MATS:
        mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        lin = tuple(_srgb_to_linear(c) for c in rgb) + (1.0,)
        mat.diffuse_color = lin
        try:
            mat.use_nodes = True
            bsdf = mat.node_tree.nodes.get("Principled BSDF")
            bsdf.inputs["Base Color"].default_value = lin
            bsdf.inputs["Roughness"].default_value = 0.85
            bsdf.inputs["Metallic"].default_value = 0.0
        except Exception:
            pass
        me.materials.append(mat)
    me.update()
    for poly, m in zip(me.polygons, mesh.m):
        poly.material_index = m
        poly.use_smooth = False              # flat shading
    me.update()
    obj = bpy.data.objects.new(OBJECT_NAME, me)
    scene.collection.objects.link(obj)
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    return obj


def export_fbx(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH, use_selection=True, object_types={'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z', axis_up='Y', bake_space_transform=True,
        mesh_smooth_type='FACE', add_leaf_bones=False)
    print("FBX exportado en:", FBX_PATH)


def main():
    mesh = build_mesh()
    result = print_summary(mesh)
    if bpy is not None:
        obj = create_blender_object(mesh)
        if FBX_PATH:
            export_fbx(obj)
        return obj
    return result


if __name__ == "__main__":
    main()
