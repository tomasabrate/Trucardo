# -*- coding: utf-8 -*-
"""
Caballo low poly con silla y brida para Blender (2.9x / 3.x / 4.x).
Uso: Scripting > New > pegar > Run Script.

- Unidades: metros. Alzada (cruz) = 1.60 m, largo cuerpo ~1.65 m, nariz a cola ~2.5 m.
- Orientacion: mira hacia -Y, Z arriba, pies en Z = 0, origen entre las patas.
- Un solo objeto "Horse", flat shading, 10 materiales (listo para FBX/Unity).
"""
import math
import random

try:
    import bpy
except ImportError:          # permite importar el modulo fuera de Blender
    bpy = None

# ----------------------------- AJUSTES -----------------------------
SCALE = 1.0                  # escala global (podes cambiarla aca o en Unity)
JITTER = 0.005               # irregularidad de facetas (m). 0 = perfectamente limpio
SEED = 11
CLEAR_SCENE = True           # borra los objetos de la escena antes de crear el caballo
CREATE_PREVIEW_SCENE = True  # camara + luz + fondo (borrables)
EXPORT_FBX = ""              # ej: "C:/proyecto/Horse.fbx"  (vacio = no exporta)

COLORS = {
    "horse_body":   (0.90, 0.88, 0.84),
    "mane":         (0.93, 0.91, 0.86),
    "hoof":         (0.66, 0.58, 0.47),
    "muzzle":       (0.62, 0.57, 0.55),
    "eye":          (0.05, 0.04, 0.04),
    "leather":      (0.46, 0.22, 0.12),
    "saddle_panel": (0.60, 0.33, 0.17),
    "saddle_seat":  (0.16, 0.09, 0.06),
    "cream":        (0.85, 0.80, 0.66),
    "metal":        (0.72, 0.72, 0.74),
}
MAT_NAMES = list(COLORS.keys())

# ----------------------------- VECTORES -----------------------------
def add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def mul(a, k): return (a[0] * k, a[1] * k, a[2] * k)
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def length(a): return math.sqrt(dot(a, a))
def norm(a):
    l = length(a)
    return (a[0] / l, a[1] / l, a[2] / l) if l > 1e-9 else (0.0, 0.0, 1.0)
def lerp(a, b, t): return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))
def P(x, f, z):
    """x lateral, f hacia adelante (el caballo mira a -Y), z altura."""
    return (x, -f, z)


class Mesh:
    def __init__(self):
        self.v, self.f, self.m = [], [], []
        self.rng = random.Random(SEED)

    def add_verts(self, pts, jitter=0.0):
        base = len(self.v)
        for p in pts:
            if jitter:
                p = (p[0] + self.rng.uniform(-jitter, jitter),
                     p[1] + self.rng.uniform(-jitter, jitter),
                     p[2] + self.rng.uniform(-jitter, jitter))
            self.v.append(p)
        return base

    def face(self, idx, mat):
        self.f.append(tuple(idx))
        self.m.append(MAT_NAMES.index(mat))


# ----------------------------- PRIMITIVAS -----------------------------
def frame(d, hint=(1.0, 0.0, 0.0)):
    d = norm(d)
    s = sub(hint, mul(d, dot(hint, d)))
    if length(s) < 1e-4:
        s = sub((0.0, 1.0, 0.0), mul(d, d[1]))
        if length(s) < 1e-4:
            s = (1.0, 0.0, 0.0)
    s = norm(s)
    return s, cross(d, s)


def ring_dirs(rings):
    out = []
    for i, r in enumerate(rings):
        if len(r) > 3 and r[3] is not None:
            out.append(norm(r[3]))
        else:
            a = rings[max(i - 1, 0)][0]
            b = rings[min(i + 1, len(rings) - 1)][0]
            out.append(norm(sub(b, a)))
    return out


def loft(M, rings, n=8, mat="horse_body", jitter=0.0, hint=(1.0, 0.0, 0.0), caps=True, rot=None):
    """Tubo por anillos. rings = [(centro, rx, rh[, direccion]), ...]. mat: str o lista por segmento."""
    if rot is None:
        rot = math.radians(22.5) if n == 8 else (math.radians(45) if n == 4 else 0.0)
    sc = math.sqrt(2.0) if n == 4 else 1.0
    ds = ring_dirs(rings)
    mats = list(mat) if isinstance(mat, (list, tuple)) else [mat] * (len(rings) - 1)
    idx = []
    for r, d in zip(rings, ds):
        c, rx, rh = r[0], r[1] * sc, r[2] * sc
        side, upv = frame(d, hint)
        pts = []
        for k in range(n):
            a = rot + 2.0 * math.pi * k / n
            ca, sa = math.cos(a), math.sin(a)
            pts.append(tuple(c[i] + side[i] * rx * ca + upv[i] * rh * sa for i in range(3)))
        idx.append(M.add_verts(pts, jitter))
    for i in range(len(rings) - 1):
        for k in range(n):
            a = idx[i] + k
            b = idx[i] + (k + 1) % n
            c = idx[i + 1] + (k + 1) % n
            d = idx[i + 1] + k
            M.face((a, b, c, d), mats[i])
    if caps:
        M.face(tuple(reversed(range(idx[0], idx[0] + n))), mats[0])
        M.face(tuple(range(idx[-1], idx[-1] + n)), mats[-1])


def box(M, c, h, mat):
    x, y, z = c
    hx, hy, hz = h
    p = [(x + sx * hx, y + sy * hy, z + sz * hz) for sz in (-1, 1) for sy in (-1, 1) for sx in (-1, 1)]
    b = M.add_verts(p)
    for q in ((0, 2, 3, 1), (4, 5, 7, 6), (0, 1, 5, 4), (2, 6, 7, 3), (0, 4, 6, 2), (1, 3, 7, 5)):
        M.face(tuple(b + i for i in q), mat)


def pyramid(M, base, apex, mat):
    cen = tuple(sum(p[i] for p in base) / len(base) for i in range(3))
    nrm = (0.0, 0.0, 0.0)
    for i in range(len(base)):
        nrm = add(nrm, cross(base[i], base[(i + 1) % len(base)]))
    if dot(nrm, sub(apex, cen)) > 0:
        base = list(reversed(base))
    b = M.add_verts(list(base) + [apex])
    n = len(base)
    M.face(tuple(b + i for i in range(n)), mat)
    for i in range(n):
        M.face((b + (i + 1) % n, b + i, b + n), mat)


def strap(M, pts, hx, hh, mat, hint=(1.0, 0.0, 0.0)):
    loft(M, [(p, hx, hh) for p in pts], n=4, mat=mat, hint=hint)


# ----------------------------- DATOS DEL CABALLO -----------------------------
# (f adelante, z centro, rx semiancho, rh semialto)
TORSO = [(-0.82, 1.20, .15, .22), (-0.72, 1.23, .22, .31), (-0.50, 1.25, .26, .35),
         (-0.28, 1.22, .26, .34), (-0.05, 1.20, .26, .35), (0.18, 1.21, .25, .36),
         (0.40, 1.26, .24, .37), (0.60, 1.27, .21, .33), (0.74, 1.24, .16, .26),
         (0.82, 1.20, .11, .19)]

NECK = [(0.48, 1.36, .18, .28), (0.64, 1.55, .155, .245), (0.80, 1.73, .13, .20),
        (0.93, 1.87, .108, .17), (1.01, 1.94, .095, .14)]

HEAD = [(1.00, 1.93, .090, .115), (1.10, 1.83, .095, .135), (1.20, 1.72, .075, .105),
        (1.29, 1.62, .062, .088), (1.36, 1.545, .066, .082), (1.40, 1.50, .052, .062)]

FRONT_LEG = [(.52, 1.12, .100, .130), (.515, .92, .088, .115), (.51, .72, .068, .088),
             (.505, .54, .066, .082), (.50, .47, .048, .058), (.50, .26, .041, .050),
             (.49, .19, .052, .062), (.505, .115, .042, .050), (.52, .075, .056, .066),
             (.545, .0, .066, .088)]

HIND_LEG = [(-.52, 1.22, .125, .170), (-.56, 1.02, .115, .160), (-.62, .84, .090, .130),
            (-.70, .64, .068, .095), (-.77, .52, .062, .085), (-.74, .40, .046, .062),
            (-.71, .24, .040, .050), (-.705, .175, .052, .062), (-.69, .10, .043, .052),
            (-.675, .07, .056, .066), (-.65, .0, .066, .088)]


def torso_spec(f):
    for i in range(len(TORSO) - 1):
        a, b = TORSO[i], TORSO[i + 1]
        if a[0] <= f <= b[0]:
            t = (f - a[0]) / (b[0] - a[0])
            return tuple(a[k] + (b[k] - a[k]) * t for k in range(1, 4))
    raise ValueError("f fuera del torso: %s" % f)


def torso_top(f):
    zc, rx, rh = torso_spec(f)
    return zc + 0.924 * rh


def torso_pt(f, t, off=0.0, s=1):
    """Punto sobre la superficie del torso. t = indice continuo de vertice (0..8); 1.5 = centro del lomo."""
    zc, rx, rh = torso_spec(f)

    def v(k):
        a = math.radians(22.5 + 45.0 * k)
        return rx * math.cos(a), rh * math.sin(a)
    k = int(math.floor(t))
    fr = t - k
    a, b = v(k), v(k + 1)
    x = a[0] + (b[0] - a[0]) * fr
    z = a[1] + (b[1] - a[1]) * fr
    L = math.hypot(x, z)
    x += x / L * off
    z += z / L * off
    return (s * x, -f, zc + z)


def torso_band(M, f0, f1, inf, mat):
    rs = []
    for f in (f0, f1):
        zc, rx, rh = torso_spec(f)
        rs.append((P(0, f, zc), rx + inf, rh + inf, (0.0, -1.0, 0.0)))
    loft(M, rs, 8, mat)


# ----------------------------- CONSTRUCCION -----------------------------
def build():
    M = Mesh()

    # --- torso, cuello, cabeza ---
    loft(M, [(P(0, f, z), rx, rh) for f, z, rx, rh in [(t[0], t[1], t[2], t[3]) for t in TORSO]],
         8, "horse_body", JITTER)

    neck_rings = [(P(0, f, z), rx, rh) for f, z, rx, rh in NECK]
    loft(M, neck_rings, 8, "horse_body", JITTER * 0.8)

    head_rings = [(P(0, f, z), rx, rh) for f, z, rx, rh in HEAD]
    head_ds = ring_dirs(head_rings)
    loft(M, head_rings, 8, ["horse_body"] * 3 + ["muzzle"] * 2, JITTER * 0.8)

    def head_at(u):
        i = min(int(u), len(head_rings) - 2)
        t = u - i
        a, b = head_rings[i], head_rings[i + 1]
        c = lerp(a[0], b[0], t)
        rx = a[1] + (b[1] - a[1]) * t
        rh = a[2] + (b[2] - a[2]) * t
        d = norm(lerp(head_ds[i], head_ds[i + 1], t))
        side, upv = frame(d)
        return c, rx, rh, d, side, upv

    # --- patas ---
    for s in (-1, 1):
        for spec, x in ((FRONT_LEG, 0.150), (HIND_LEG, 0.160)):
            rings = [(P(s * x, f, z), rx, rh) for f, z, rx, rh in spec]
            rings[-1] = rings[-1] + ((0.0, 0.0, -1.0),)
            mats = ["horse_body"] * (len(rings) - 2) + ["hoof"]
            loft(M, rings, 8, mats)

    # --- orejas ---
    c0, rx0, rh0, d0, side0, upv0 = head_at(0.05)
    for s in (-1, 1):
        p = add(add(c0, mul(upv0, rh0 * 0.7)), mul(side0, s * 0.055))
        e = norm(add(mul(upv0, 0.55), (0.0, 0.0, 0.9)))
        loft(M, [(p, .030, .045), (add(p, mul(e, .08)), .028, .040),
                 (add(p, mul(e, .16)), .006, .008)], 4, "horse_body")

    # --- ojos y narinas ---
    ce, rxe, rhe, de, se, ue = head_at(1.35)
    cn, rxn, rhn, dn, sn, un = head_at(4.55)
    for s in (-1, 1):
        p = add(add(ce, mul(se, s * rxe * 0.924)), mul(ue, rhe * 0.30))
        base = [add(p, mul(de, .022)), add(p, mul(ue, .014)), add(p, mul(de, -.022)), add(p, mul(ue, -.014))]
        pyramid(M, base, add(p, mul(se, s * .008)), "eye")
        p = add(add(cn, mul(sn, s * rxn * 0.924)), mul(un, rhn * 0.10))
        base = [add(p, mul(dn, .016)), add(p, mul(un, .011)), add(p, mul(dn, -.016)), add(p, mul(un, -.011))]
        pyramid(M, base, add(p, mul(sn, s * .005)), "eye")

    # --- crin ---
    crest = []
    nds = ring_dirs(neck_rings)
    for r, d in zip(neck_rings, nds):
        _, upv = frame(d)
        crest.append(add(r[0], mul(upv, r[2] * 0.93)))

    def crest_at(t):
        x = t * (len(crest) - 1)
        i = min(int(x), len(crest) - 2)
        return lerp(crest[i], crest[i + 1], x - i)

    nb = 11
    for i in range(nb):
        t = i / (nb - 1.0)
        s = 1 if i % 2 else -1
        k = 0.55 + 0.45 * t
        pc = crest_at(t)
        pts = [add(pc, (0, -0.02, -0.03)),
               add(pc, (s * .030, .10 * k, .05 * k)),
               add(pc, (s * .070, .24 * k, .02 * k)),
               add(pc, (s * .100, .40 * k, -.08 * k))]
        loft(M, [(pts[0], .022, .070), (pts[1], .020, .085), (pts[2], .016, .070), (pts[3], .006, .020)],
             4, "mane")

    # copete
    cf, rxf, rhf, df, sf, uf = head_at(0.40)
    pc = add(cf, mul(uf, rhf * 0.95))
    for s in (-1, 1):
        pts = [add(pc, mul(uf, -.02)), add(add(pc, mul(df, .07)), mul(uf, .03)),
               add(add(pc, mul(df, .14)), mul(uf, .02))]
        pts = [add(p, (s * .015, 0, 0)) for p in pts]
        loft(M, [(pts[0], .012, .022), (pts[1], .010, .022), (pts[2], .004, .008)], 4, "mane")

    # --- cola ---
    tail = [(-.80, 1.36), (-.90, 1.22), (-.97, 0.95), (-1.00, 0.68)]
    for i, (xo, ln, sw) in enumerate([(-.08, .55, .02), (-.05, .80, .05), (-.02, .95, .08), (.01, 1.0, .10),
                                      (.04, .90, .12), (.07, .75, .09), (.00, .65, .15)]):
        pts = []
        for j, (f, z) in enumerate(tail):
            u = j / 3.0
            z2 = 1.36 - (1.36 - z) * ln
            pts.append(P(xo + sw * u * (1 if xo >= 0 else -1), f - 0.05 * u * (i % 3), z2))
        loft(M, [(pts[0], .040, .060), (pts[1], .050, .075), (pts[2], .040, .065), (pts[3], .010, .018)],
             4, "mane")

    # --- silla ---
    saddle_f = [-0.26, -0.20, 0.0, 0.20, 0.30]
    panel = [(P(0, f, torso_top(f) - 0.004), rx, rh)
             for f, rx, rh in zip(saddle_f, (.15, .19, .20, .19, .15), (.05, .075, .08, .075, .05))]
    loft(M, panel, 8, "saddle_panel")

    seat_spec = [(-0.24, .11, .030), (-0.18, .16, .080), (-0.08, .15, .060), (0.02, .14, .045),
                 (0.12, .14, .055), (0.22, .15, .080), (0.30, .10, .035)]
    seat = [(P(0, f, torso_top(f) + 0.064 + rh * 0.9 - 0.01), rx, rh) for f, rx, rh in seat_spec]
    loft(M, seat, 8, "saddle_seat")

    for s in (-1, 1):
        fl = [(.185, .04, 1.50, .17), (.226, .045, 1.38, .165), (.254, .05, 1.22, .15), (.256, .05, 1.14, .13)]
        loft(M, [(P(s * x, f, z), .011, rh) for x, f, z, rh in fl], 4, "saddle_panel")
        # estribo (cuero + hierro)
        sx = s * 0.285
        box(M, P(sx, .045, 1.20), (.007, .016, .21), "leather")
        box(M, P(sx, .045, .846), (.012, .060, .008), "metal")
        for ds in (-1, 1):
            box(M, P(sx, .045 + ds * .053, .91), (.007, .007, .07), "metal")
        box(M, P(sx, .045, .975), (.007, .030, .007), "metal")
        # contrafuerte crema + hebilla
        box(M, P(s * .268, .17, 1.28), (.006, .020, .17), "cream")
        box(M, P(s * .272, .17, 1.44), (.004, .015, .012), "metal")

    torso_band(M, 0.13, 0.19, 0.014, "cream")   # cincha

    # --- pechera ---
    torso_band(M, 0.60, 0.66, 0.012, "leather")
    belly = [torso_pt(f, 5.5, 0.008) for f in (0.60, 0.50, 0.40, 0.30, 0.19)]
    strap(M, belly, .012, .005, "leather")
    for s in (-1, 1):
        pts = [torso_pt(f, t, 0.010, s) for f, t in ((0.30, 1.0), (0.40, 0.8), (0.50, 0.55), (0.60, 0.35))]
        strap(M, pts, .007, .007, "leather")

    # --- cabezada, bocado y riendas ---
    def head_band(u0, u1, inf, mat):
        rs = []
        for u in (u0, u1):
            c, rx, rh, d, _, _ = head_at(u)
            rs.append((c, rx + inf, rh + inf, d))
        loft(M, rs, 8, mat)

    head_band(0.55, 0.70, 0.010, "leather")   # frontalera / ahogadero
    head_band(3.55, 3.75, 0.010, "leather")   # muserola
    for s in (-1, 1):
        pts = []
        for u, k in ((0.62, 0.10), (1.5, -0.05), (2.5, -0.20), (3.5, -0.30), (4.1, -0.35)):
            c, rx, rh, d, side, upv = head_at(u)
            pts.append(add(add(c, mul(side, s * (rx * 0.93 + 0.004))), mul(upv, rh * k)))
        strap(M, pts, .004, .009, "leather")
        bit = pts[-1]
        box(M, add(bit, (s * 0.010, 0, 0)), (.010, .012, .012), "metal")
        rein = [bit, P(s * .12, 1.15, 1.40), P(s * .16, .95, 1.35), P(s * .20, .74, 1.33),
                P(s * .235, .58, 1.40), P(s * .215, .40, 1.52)]
        strap(M, rein, .007, .007, "leather")

    return M


# ----------------------------- BLENDER -----------------------------
def create_in_blender(M):
    scene = bpy.context.scene
    if CLEAR_SCENE:
        for o in list(bpy.data.objects):
            bpy.data.objects.remove(o, do_unlink=True)

    mesh = bpy.data.meshes.new("HorseMesh")
    mesh.from_pydata([(x * SCALE, y * SCALE, z * SCALE) for x, y, z in M.v], [], M.f)
    for name in MAT_NAMES:
        r, g, b = COLORS[name]
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = (r, g, b, 1.0)
        try:
            mat.use_nodes = True
            bsdf = mat.node_tree.nodes.get("Principled BSDF")
            if bsdf:
                bsdf.inputs["Base Color"].default_value = (r, g, b, 1.0)
                bsdf.inputs["Roughness"].default_value = 0.9
        except Exception:
            pass
        mesh.materials.append(mat)
    mesh.update()
    mesh.polygons.foreach_set("material_index", M.m)
    mesh.polygons.foreach_set("use_smooth", [False] * len(mesh.polygons))
    mesh.update()
    mesh.validate()

    obj = bpy.data.objects.new("Horse", mesh)
    scene.collection.objects.link(obj)

    if CREATE_PREVIEW_SCENE:
        try:
            world = bpy.data.worlds.new("HorseWorld")
            scene.world = world
            world.use_nodes = True
            world.node_tree.nodes["Background"].inputs[0].default_value = (0.82, 0.78, 0.71, 1.0)
        except Exception:
            pass
        target = bpy.data.objects.new("HorseTarget", None)
        target.location = (0.0, 0.0, 0.95)
        scene.collection.objects.link(target)
        cam = bpy.data.objects.new("HorseCam", bpy.data.cameras.new("HorseCam"))
        cam.location = (-4.4, -2.9, 1.65)
        cam.data.lens = 50
        con = cam.constraints.new('TRACK_TO')
        con.target = target
        con.track_axis = 'TRACK_NEGATIVE_Z'
        con.up_axis = 'UP_Y'
        scene.collection.objects.link(cam)
        scene.camera = cam
        sun = bpy.data.objects.new("HorseSun", bpy.data.lights.new("HorseSun", 'SUN'))
        sun.data.energy = 3.0
        sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
        scene.collection.objects.link(sun)

    xs = [v[0] for v in M.v]; ys = [v[1] for v in M.v]; zs = [v[2] for v in M.v]
    print("Horse: %d verts, %d caras | ancho %.2f, largo %.2f, alto %.2f m (x SCALE)" % (
        len(M.v), len(M.f), (max(xs) - min(xs)) * SCALE, (max(ys) - min(ys)) * SCALE, (max(zs) - min(zs)) * SCALE))

    if EXPORT_FBX:
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.export_scene.fbx(filepath=EXPORT_FBX, use_selection=True, object_types={'MESH'})
    return obj


if bpy is not None:
    create_in_blender(build())
