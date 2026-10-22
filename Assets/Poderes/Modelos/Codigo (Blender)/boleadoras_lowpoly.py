# -*- coding: utf-8 -*-
"""
boleadoras_lowpoly.py  --  Boleadoras (tres bolas) low poly para props de Unity.

Uso:
  * En Blender 2.8 o superior: pestana Scripting > abrir este archivo > Run Script.
  * Fuera de Blender (python boleadoras_lowpoly.py): no crea nada, solo imprime un
    resumen de la malla (triangulos, medidas en cm, si esta cerrada).

Convenciones:
  * Metros, tamano real (bolas de ~7 cm). Eje Z hacia arriba.
  * Origen en el centro de la huella del objeto sobre la mesa; la base queda en z = 0.
  * Un solo objeto ("Boleadoras") con 5 material slots de color plano, flat shading.

Como se midio la foto (vista 3/4 elevada, 2048 x 2048 px):
  * Las bolas grandes miden ~290 px de diametro  ->  7 cm  ->  S = 41,4 px/cm.
  * Se asumio una camara elevada ~50 grados sobre la mesa: lo que va hacia el fondo se ve
    acortado por sin(50) y la altura sobre la mesa desplaza el punto hacia arriba por
    cos(50). Con eso se pasaron los puntos de la foto (px) al plano de la mesa (cm).
  * El rollo de soga mide ~14 cm de diametro exterior.
Lo que la foto no muestra esta marcado con "ASUMIDO".
"""
import math
from collections import defaultdict

try:
    import bpy  # solo existe dentro de Blender
except ImportError:
    bpy = None

# =============================================================== CONFIGURACION
FBX_PATH = ""            # si tiene una ruta (p. ej. r"C:\proy\Assets\boleadoras.fbx") exporta un .fbx
OBJECT_NAME = "Boleadoras"

# ====================================================== MEDIDAS (en cm, al final -> m)
S = 41.4                 # px de la foto por cm (bolas de 7 cm = 290 px)
CAM_ELEV = math.radians(50.0)
SIN_E, COS_E = math.sin(CAM_ELEV), math.cos(CAM_ELEV)
REF_PX = (342.0, 942.0)  # centro del rollo en la foto (a z = 1,7 cm): es el (0, 0) del plano
REF_Z = 1.7

ROPE_R = 0.65            # radio de la soga (1,3 cm de diametro, medido en la foto: ~55 px)
ROPE_TWIST = math.radians(40.0)   # giro entre anillos: las facetas forman franjas diagonales (torsion)
ROPE_SIDES = 5
SHANK_SIDES = 6          # mango de madera hexagonal
BALL_SIDES = 8
BALL_D = (7.0, 7.0, 6.0) # diametros de las bolas A (arriba), B (abajo) y C (derecha): C se ve ~18 % menor
BALL_PROFILE = [(0.00, 0.34), (0.14, 0.80), (0.42, 1.00), (0.72, 0.92), (0.92, 0.60), (1.00, 0.30)]
BALL_LEN = 1.0           # largo del huevo / diametro (la foto muestra bolas casi redondas, algo afinadas al mango)
SHANK_KNOB_R = 1.05      # radio del nudo "diamante" donde la soga entra al mango
SHANK_R0, SHANK_R1 = 0.55, 0.62

# --- rollo (soga de la bola A enroscada: ~2,75 vueltas hacia arriba, mirando desde arriba en sentido antihorario)
COIL_R0, COIL_R1 = 6.5, 4.2
COIL_Z0, COIL_Z1 = ROPE_R, ROPE_R + 2.4
COIL_TURNS = 2.75
COIL_ANG0 = 150.0        # angulo donde la soga entra al rollo (cm sobre el plano de la mesa, 0 = +X)
COIL_STEP = 40.0         # grados por segmento (9 por vuelta -> aspecto facetado)

# --- puntos tomados de la foto (px) ; el 3er valor es la altura sobre la mesa en cm
KNOB_A = (1097, 678)                         # nudo y atadura de la bola A
KNOB_B = (1118, 959)                         # nudo del mango de la bola B
KNOB_C = (1526, 991)                         # nudo del mango de la bola C
BALL_CENTER_PX = [(1478, 656), (1422, 1292), (1882, 1198)]   # centros de las bolas A, B, C
JUNCTION_PX = (520, 664)                     # donde se juntan las tres sogas (detras del rollo)
ARC_A_PX = [(1030, 668), (960, 596), (830, 552), (680, 548), (530, 585), (410, 632), (290, 680)]
ROPE_B_PX = [(742, 678), (901, 721), (1018, 806), (1102, 933)]
ROPE_C_PX = [(848, 668), (1166, 731), (1325, 859), (1484, 954)]

# --- materiales (nombre, color sRGB 0-255 tomado de la foto)
MATS = [
    ("Soga_clara",   (130, 78, 48)),     # 0 mitad clara de la torsion
    ("Soga_oscura",  (84, 48, 30)),      # 1 mitad oscura de la torsion y nudo central
    ("Bola_madera",  (170, 108, 60)),    # 2 bolas
    ("Mango_madera", (136, 80, 45)),     # 3 mangos y nudos de los mangos
    ("Atadura",      (229, 206, 184)),   # 4 atadura crema junto a la bola A
]
M_ROPE_L, M_ROPE_D, M_BALL, M_SHANK, M_BIND = range(5)


# ============================================================ VECTORES
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def mul(a, k): return (a[0] * k, a[1] * k, a[2] * k)
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def length(a): return math.sqrt(dot(a, a))
def unit(a):
    n = length(a)
    return (a[0] / n, a[1] / n, a[2] / n)


def px_to_cm(p, z):
    """Punto de la foto (px) a coordenadas del plano de la mesa (cm), dado su altura z en cm."""
    x = (p[0] - REF_PX[0]) / S
    y = ((REF_PX[1] - p[1]) - (z - REF_Z) * S * COS_E) / (S * SIN_E)
    return (x, y, z)


# ================================================================== MALLA
class Mesh:
    def __init__(self):
        self.v, self.f, self.m, self.pieces = [], [], [], []
        self._start = 0

    def verts(self, pts):
        ids = []
        for p in pts:
            self.v.append((float(p[0]), float(p[1]), float(p[2])))
            ids.append(len(self.v) - 1)
        return ids

    def face(self, ids, mat):
        """Cara convexa: tri, quad (plano o no) o abanico."""
        if len(ids) == 3:
            self.f.append(tuple(ids)); self.m.append(mat)
        elif len(ids) == 4:
            a, b, c, d = ids
            n = cross(sub(self.v[b], self.v[a]), sub(self.v[c], self.v[a]))
            ln = length(n)
            dist = abs(dot(n, sub(self.v[d], self.v[a]))) / ln if ln > 1e-14 else 0.0
            if dist < 1e-6:
                self.f.append((a, b, c, d)); self.m.append(mat)
            else:
                self.f.append((a, b, c)); self.m.append(mat)
                self.f.append((a, c, d)); self.m.append(mat)
        else:
            for j in range(1, len(ids) - 1):
                self.f.append((ids[0], ids[j], ids[j + 1])); self.m.append(mat)

    def begin(self):
        self._start = len(self.f)

    def end(self):
        """Cierra la pieza: orienta todas sus caras hacia afuera (volumen positivo)."""
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
                vol += dot(p0, cross(self.v[f[j]], self.v[f[j + 1]]))
        return vol / 6.0

    def tri_count(self):
        return sum(len(f) - 2 for f in self.f)


def sweep(mesh, pts, radii, sides, matfn, twist=0.0):
    """Tubo cerrado a lo largo de una poligonal. Los anillos se cortan en ingletes (el ancho se
    conserva en las curvas) y las tapas son poligonos convexos. Con twist != 0 cada anillo gira
    respecto del anterior: cada lado queda como dos triangulos (matfn(i, k, parte))."""
    n = len(pts)
    d = [unit(sub(pts[i + 1], pts[i])) for i in range(n - 1)]
    t, m, inv = [], [], []
    for i in range(n):
        if i == 0:
            t.append(d[0]); m.append(None); inv.append(1.0)
        elif i == n - 1:
            t.append(d[-1]); m.append(None); inv.append(1.0)
        else:
            t.append(unit(add(d[i - 1], d[i])))
            c = max(-1.0, min(1.0, dot(d[i - 1], d[i])))
            half = math.acos(c) / 2.0
            if half < 1e-6:
                m.append(None); inv.append(1.0)
            else:
                m.append(unit(sub(d[i], d[i - 1]))); inv.append(1.0 / math.cos(half))
    n0 = cross(t[0], (0.0, 0.0, 1.0))
    if length(n0) < 1e-6:
        n0 = cross(t[0], (1.0, 0.0, 0.0))
    nn = unit(sub(n0, mul(t[0], dot(n0, t[0]))))
    mesh.begin()
    rings = []
    for i in range(n):
        nn = unit(sub(nn, mul(t[i], dot(nn, t[i]))))
        bb = cross(t[i], nn)
        ring = []
        for k in range(sides):
            a = 2.0 * math.pi * k / sides + twist * i
            v = add(mul(nn, radii[i] * math.cos(a)), mul(bb, radii[i] * math.sin(a)))
            if m[i] is not None:
                v = add(v, mul(m[i], dot(v, m[i]) * (inv[i] - 1.0)))
            ring.append(add(pts[i], v))
        rings.append(mesh.verts(ring))
    for i in range(n - 1):
        for k in range(sides):
            j = (k + 1) % sides
            a, b, c, e = rings[i][k], rings[i][j], rings[i + 1][j], rings[i + 1][k]
            if twist == 0.0:
                mesh.face([a, b, c, e], matfn(i, k, 0))
            else:
                mesh.face([a, b, c], matfn(i, k, 0))
                mesh.face([a, c, e], matfn(i, k, 1))
    mesh.face(list(reversed(rings[0])), matfn(0, 0, 0))
    mesh.face(list(rings[-1]), matfn(n - 2, 0, 1))
    mesh.end()


def smooth(points, nseg):
    """Curva suave (Catmull-Rom) por los puntos, remuestreada en nseg tramos de igual largo."""
    P = [points[0]] + list(points) + [points[-1]]
    dense = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for s_ in range(16):
            u_ = s_ / 16.0
            dense.append(tuple(0.5 * ((2 * p1[c]) + (-p0[c] + p2[c]) * u_ + (2 * p0[c] - 5 * p1[c] + 4 * p2[c] - p3[c]) * u_ ** 2
                                      + (-p0[c] + 3 * p1[c] - 3 * p2[c] + p3[c]) * u_ ** 3) for c in range(3)))
    dense.append(points[-1])
    acc = [0.0]
    for i in range(1, len(dense)):
        acc.append(acc[-1] + length(sub(dense[i], dense[i - 1])))
    out = []
    for q in range(nseg + 1):
        target = acc[-1] * q / nseg
        j = 1
        while j < len(acc) - 1 and acc[j] < target:
            j += 1
        f = (target - acc[j - 1]) / max(acc[j] - acc[j - 1], 1e-12)
        out.append(tuple(dense[j - 1][c] + (dense[j][c] - dense[j - 1][c]) * f for c in range(3)))
    return out


def rope_mat(i, k, part):
    return M_ROPE_L if part == 0 else M_ROPE_D


def box(mesh, c, ax, hx, hy, hz, mat):
    """Caja centrada en c, con el lado hx a lo largo del vector horizontal ax."""
    ex = unit((ax[0], ax[1], 0.0))
    ey = (-ex[1], ex[0], 0.0)
    def P(sx, sy, sz):
        return (c[0] + sx * hx * ex[0] + sy * hy * ey[0], c[1] + sx * hx * ex[1] + sy * hy * ey[1], c[2] + sz * hz)
    mesh.begin()
    lo = mesh.verts([P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1)])
    hi = mesh.verts([P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1)])
    mesh.face(list(reversed(lo)), mat)
    mesh.face(hi, mat)
    for i in range(4):
        j = (i + 1) % 4
        mesh.face([lo[i], lo[j], hi[j], hi[i]], mat)
    mesh.end()


def ball(mesh, neck, axis, diameter, mat):
    """Bola 'huevo' tipo torno alrededor de axis (desde el cuello hacia la punta). Se apoya sobre una
    cara plana. Devuelve (cuello_final): el cuello se desplaza en z para que la bola apoye en z = 0."""
    a = unit(axis)
    u = unit(cross(a, (0.0, 0.0, 1.0)))
    w = cross(a, u)
    R = diameter / 2.0
    Lb = diameter * BALL_LEN
    off = math.pi / BALL_SIDES                  # un lado plano queda abajo: apoya estable
    rings = []
    allp = []
    for tt, rr in BALL_PROFILE:
        ring = []
        for k in range(BALL_SIDES):
            ang = off + 2.0 * math.pi * k / BALL_SIDES
            p = add(add(neck, mul(a, tt * Lb)), add(mul(u, R * rr * math.cos(ang)), mul(w, R * rr * math.sin(ang))))
            ring.append(p)
            allp.append(p)
        rings.append(ring)
    dz = -min(p[2] for p in allp)
    mesh.begin()
    ids = [mesh.verts([(p[0], p[1], p[2] + dz) for p in ring]) for ring in rings]
    for i in range(len(ids) - 1):
        for k in range(BALL_SIDES):
            j = (k + 1) % BALL_SIDES
            mesh.face([ids[i][k], ids[i][j], ids[i + 1][j], ids[i + 1][k]], mat)
    mesh.face(list(reversed(ids[0])), mat)
    mesh.face(list(ids[-1]), mat)
    mesh.end()
    return (neck[0], neck[1], neck[2] + dz)


def shank_and_ball(mesh, knob, ball_center_px, diameter, with_binding):
    """Mango de madera (con nudo en diamante) que sale del nudo de la soga y lleva la bola."""
    zc = 0.46 * diameter + 0.4
    bc = px_to_cm(ball_center_px, zc)               # centro de la bola sobre el plano de la mesa
    horiz = unit((bc[0] - knob[0], bc[1] - knob[1], 0.0))
    Lb = diameter * BALL_LEN
    pitch = 0.2
    neck = None
    for _ in range(4):                              # el mango sube de la mesa al cuello de la bola
        axis = (horiz[0] * math.cos(pitch), horiz[1] * math.cos(pitch), math.sin(pitch))
        n0 = (bc[0] - axis[0] * 0.45 * Lb, bc[1] - axis[1] * 0.45 * Lb, 0.0)
        probe = Mesh()
        neck = ball(probe, (n0[0], n0[1], 2.0), axis, diameter, M_BALL)
        run = math.hypot(neck[0] - knob[0], neck[1] - knob[1])
        pitch = math.atan2(neck[2] - knob[2], run)
    axis = (horiz[0] * math.cos(pitch), horiz[1] * math.cos(pitch), math.sin(pitch))
    neck = ball(mesh, (n0[0], n0[1], 2.0), axis, diameter, M_BALL)
    d = unit(sub(neck, knob))
    L = length(sub(neck, knob)) + 0.3
    pts = [add(knob, mul(d, s)) for s in (0.0, 1.0, 2.0, L)]
    sweep(mesh, pts, [0.45, SHANK_KNOB_R, SHANK_R0, SHANK_R1], SHANK_SIDES, lambda i, k, p: M_SHANK)
    if with_binding:
        sweep(mesh, [add(knob, mul(d, 2.45)), add(knob, mul(d, 3.25))], [0.72, 0.72], SHANK_SIDES,
              lambda i, k, p: M_BIND)
    return d


# ============================================================ CONSTRUCCION
def build_mesh():
    mesh = Mesh()
    zr = ROPE_R                                    # las sogas apoyan sobre la mesa

    knobs = [px_to_cm(KNOB_A, zr), px_to_cm(KNOB_B, zr), px_to_cm(KNOB_C, zr)]
    J = px_to_cm(JUNCTION_PX, zr)
    dirs = []
    for kn, cpx, diam, bind in zip(knobs, BALL_CENTER_PX, BALL_D, (True, False, False)):
        dirs.append(shank_and_ball(mesh, kn, cpx, diam, bind))

    # --- soga B y soga C: del nudo central a los mangos (curva suave, N tramos)
    for k, pxs, nseg in ((1, ROPE_B_PX, 6), (2, ROPE_C_PX, 8)):
        end = add(knobs[k], mul(dirs[k], 0.4))
        raw = [J] + [px_to_cm(p, zr) for p in pxs[:-1]] + [end]
        pts = smooth(raw, nseg)
        sweep(mesh, pts, [ROPE_R] * len(pts), ROPE_SIDES, rope_mat, ROPE_TWIST)

    # --- soga A: del mango A, en arco por detras del rollo, 2,75 vueltas hacia arriba y al nudo central
    def polar(r, ang_deg, z):
        a_ = math.radians(ang_deg)
        return (r * math.cos(a_), r * math.sin(a_), z)

    end = add(knobs[0], mul(dirs[0], 0.4))
    coil = []
    n_steps = int(round(COIL_TURNS * 360.0 / COIL_STEP))
    for i in range(n_steps + 1):
        f = i / float(n_steps)
        coil.append(polar(COIL_R0 + (COIL_R1 - COIL_R0) * f, COIL_ANG0 + 360.0 * COIL_TURNS * f,
                          COIL_Z0 + (COIL_Z1 - COIL_Z0) * f))
    raw = [end] + [px_to_cm(p, zr) for p in ARC_A_PX] + [(-4.3, 6.8, zr), coil[0]]
    arc = smooth(raw, 9)
    ang_end = COIL_ANG0 + 360.0 * COIL_TURNS
    link = [polar(5.6, ang_end + 3.0, COIL_Z1 + 0.45), polar(7.7, ang_end + 5.0, 1.7), J]   # sube sobre el rollo y baja al nudo
    path = arc[:-1] + coil + link
    sweep(mesh, path, [ROPE_R] * len(path), ROPE_SIDES, rope_mat, ROPE_TWIST)

    # --- nudo central donde se unen las tres sogas
    box(mesh, (J[0], J[1], J[2] + 0.1), (1.0, 0.2, 0.0), 0.9, 0.9, 0.75, M_ROPE_D)

    # --- origen: centro de la huella en XY, base en z = 0 ; cm -> m
    xs = [p[0] for p in mesh.v]; ys = [p[1] for p in mesh.v]; zs = [p[2] for p in mesh.v]
    cx, cy, z0 = (min(xs) + max(xs)) / 2.0, (min(ys) + max(ys)) / 2.0, min(zs)
    mesh.v = [((x - cx) / 100.0, (y - cy) / 100.0, (z - z0) / 100.0) for x, y, z in mesh.v]
    return mesh


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
            if length(cross(sub(mesh.v[f[j]], p0), sub(mesh.v[f[j + 1]], p0))) < 1e-10:
                degenerate += 1
    vols = [mesh.signed_volume(range(s, e)) for s, e in mesh.pieces]
    xs, ys, zs = zip(*mesh.v)
    return {
        "triangulos": mesh.tri_count(), "caras": len(mesh.f), "vertices": len(mesh.v),
        "piezas": len(mesh.pieces), "aristas_abiertas": open_edges, "aristas_repetidas": repeated,
        "caras_degeneradas": degenerate, "volumen_min_cm3": min(vols) * 1e6,
        "cerrada": open_edges == 0 and repeated == 0,
        "min": (min(xs), min(ys), min(zs)), "max": (max(xs), max(ys), max(zs)),
        "dim_cm": ((max(xs) - min(xs)) * 100, (max(ys) - min(ys)) * 100, (max(zs) - min(zs)) * 100),
    }


def print_summary(mesh):
    r = analyze(mesh)
    print("=" * 60)
    print(OBJECT_NAME)
    print("Triangulos: %d  |  caras: %d  |  vertices: %d" % (r["triangulos"], r["caras"], r["vertices"]))
    print("Medidas (X ancho, Y fondo, Z alto): %.1f x %.1f x %.1f cm" % r["dim_cm"])
    print("z minima: %.4f m (la base debe quedar en 0)" % r["min"][2])
    print("Piezas solidas: %d | volumen minimo: %.2f cm3 (todas positivas: %s)"
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
