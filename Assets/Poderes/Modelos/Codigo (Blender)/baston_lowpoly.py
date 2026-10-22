# -*- coding: utf-8 -*-
"""
baston_lowpoly.py  --  Baston de mando presidencial (representativo) low poly para props de Unity.

Uso:
  * En Blender 2.8 o superior: pestana Scripting > abrir este archivo > Run Script.
  * Fuera de Blender (python baston_lowpoly.py): no crea nada, solo imprime un resumen de la
    malla (triangulos, medidas en cm, si esta cerrada).

Convenciones:
  * Metros. Largo total 0,80 m (maximo pedido). Eje Z hacia arriba.
  * Origen en el centro de la base donde apoya sobre la mesa; la base queda en z = 0.
  * Un solo objeto ("Baston") con 6 material slots de color plano, flat shading.

Como se midio la foto: se enderezo la silueta (el baston de la foto esta inclinado) y se midio el
radio de cada anillo, banda y seccion a lo largo del eje, como fraccion del largo total (f = 0 en el
pomo, f = 1 en la punta del regaton). Eso se paso a cm con el largo de 80 cm.
PROPORCIONES: la foto es mucho mas gruesa que un baston real (10:1). Se afino la cana (3,6 -> 2,9 cm
de diametro) y el pomo se dejo mas ancho (cañon de 4,9 cm) para que el escudo y los aros se lean.
Lo que la foto no muestra esta marcado con "ASUMIDO".
"""
import math
from collections import defaultdict

try:
    import bpy  # solo existe dentro de Blender
except ImportError:
    bpy = None

# =============================================================== CONFIGURACION
FBX_PATH = ""            # si tiene una ruta (p. ej. r"C:\proy\Assets\baston.fbx") exporta un .fbx
OBJECT_NAME = "Baston"
POSE = "acostado"        # "acostado": tendido sobre la mesa con el escudo hacia arriba (ASUMIDO)
                         # "parado":   apoyado sobre la punta del regaton, el escudo hacia -Y
LENGTH_CM = 80.0         # largo total (no pasar de 80)
SIDES = 8                # lados de las secciones de revolucion (facetado)

BASE_L = 80.0            # el perfil de abajo esta escrito para 80 cm; se escala con LENGTH_CM / BASE_L
K = LENGTH_CM / BASE_L

# --- materiales (nombre, color sRGB 0-255 tomado de la foto)
MATS = [
    ("Madera",      (112, 51, 38)),     # 0 cana, mastil del gorro
    ("Plata",       (186, 184, 182)),   # 1 pomo, aros, canon, regaton
    ("Oro",         (232, 192, 106)),   # 2 sol, laurel, manos
    ("Celeste",     (125, 174, 219)),   # 3 campo superior del escudo, lazo
    ("Blanco",      (240, 240, 240)),   # 4 campo inferior del escudo
    ("Gris_oscuro", (112, 108, 105)),   # 5 bandas grabadas del pomo
]
M_WOOD, M_SILVER, M_GOLD, M_BLUE, M_WHITE, M_GRAY = range(6)

# --- perfil de revolucion medido en la foto: (f desde el pomo, radio en cm, material del tramo que sigue)
# Dos filas con el mismo f = un escalon plano (cara anular).
PROFILE = [
    (0.0000, 1.28, M_SILVER),   # tapa superior del pomo (plana, achaflanada)
    (0.0110, 2.41, M_SILVER),
    (0.0260, 3.23, M_SILVER),   # borde mas ancho del pomo (6,5 cm)
    (0.0330, 3.20, M_SILVER),
    (0.0404, 2.73, M_SILVER),   # cuello acampanado
    (0.0640, 2.39, M_SILVER),
    (0.0665, 2.69, M_SILVER),   # aro 1
    (0.0735, 2.78, M_SILVER),
    (0.0795, 2.53, M_SILVER),
    (0.0808, 2.48, M_GRAY),     # banda grabada 1
    (0.1040, 2.43, M_SILVER),
    (0.1075, 2.66, M_SILVER),   # aro 2
    (0.1139, 2.73, M_SILVER),
    (0.1200, 2.50, M_SILVER),
    (0.1212, 2.44, M_SILVER),   # canon del escudo (ligeramente conico)
    (0.2290, 2.29, M_SILVER),
    (0.2320, 2.53, M_SILVER),   # aro 3
    (0.2351, 2.60, M_SILVER),
    (0.2424, 2.34, M_SILVER),
    (0.2440, 2.34, M_GRAY),     # banda grabada 2
    (0.2660, 2.30, M_SILVER),
    (0.2685, 2.53, M_SILVER),   # aro 4
    (0.2755, 2.66, M_SILVER),
    (0.2828, 2.37, M_SILVER),
    (0.2840, 2.34, M_SILVER),   # casquillo inferior
    (0.3085, 2.27, M_SILVER),
    (0.3105, 2.27, M_SILVER),   # escalon: el casquillo apoya sobre la cana
    (0.3105, 1.80, M_WOOD),     # cana: 3,6 cm arriba ...
    (0.9253, 1.45, M_SILVER),   # ... 2,9 cm abajo
    (0.9253, 1.50, M_SILVER),   # regaton: aro y cono
    (0.9293, 1.50, M_SILVER),
    (0.9293, 1.38, M_SILVER),
    (0.9844, 1.12, M_SILVER),
    (0.9954, 0.60, M_SILVER),
    (1.0000, 0.45, M_SILVER),   # punta plana
]

# --- escudo (plano del escudo, cm; u = derecha, v = arriba; origen = centro del escudo)
EMBLEM_F = 0.174         # posicion del centro del escudo sobre el eje (en el canon)
EMBLEM_EMBED = 1.0       # cuanto se hunde la base de las piezas dentro del canon (queda oculta)
EMBLEM_SCALE = 1.2       # el escudo se agranda un 20 % respecto de la foto para que se lea (ASUMIDO)
H_FIELD, H_SUN, H_WREATH, H_POLE, H_CAP, H_HANDS, H_BOW = 0.16, 0.24, 0.30, 0.30, 0.34, 0.38, 0.34


# ============================================================ GEOMETRIA 2D
def _cross2(o, a, b):
    return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])


def _area2(poly):
    s = 0.0
    for i in range(len(poly)):
        x1, y1 = poly[i]
        x2, y2 = poly[(i + 1) % len(poly)]
        s += x1 * y2 - x2 * y1
    return s


def _in_tri(p, a, b, c, eps=1e-12):
    return _cross2(a, b, p) >= -eps and _cross2(b, c, p) >= -eps and _cross2(c, a, p) >= -eps


def triangulate(poly):
    """Triangula un poligono simple (corte de orejas). Devuelve (poly_antihorario, triangulos)."""
    pts = list(poly)
    if _area2(pts) < 0:
        pts.reverse()
    idx = list(range(len(pts)))
    tris = []
    guard = 0
    while len(idx) > 3 and guard < 10000:
        guard += 1
        n = len(idx)
        for i in range(n):
            a, b, c = idx[i - 1], idx[i], idx[(i + 1) % n]
            if _cross2(pts[a], pts[b], pts[c]) <= 1e-12:
                continue
            if any(j not in (a, b, c) and pts[j] not in (pts[a], pts[b], pts[c])
                   and _in_tri(pts[j], pts[a], pts[b], pts[c]) for j in idx):
                continue
            tris.append((a, b, c))
            del idx[i]
            break
        else:
            raise ValueError("fallo al triangular el poligono")
    tris.append(tuple(idx))
    return pts, tris


# ================================================================== MALLA
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def length(a): return math.sqrt(dot(a, a))


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


def lathe(mesh):
    """Todo el baston (pomo, aros, canon, cana, regaton) como un solo solido de revolucion."""
    off = math.pi / SIDES                          # un lado plano queda hacia -Y y hacia +Y
    rings = []
    for f, r, _m in PROFILE:
        z = (1.0 - f) * LENGTH_CM
        ring = [(r * K * math.cos(off + 2.0 * math.pi * k / SIDES),
                 r * K * math.sin(off + 2.0 * math.pi * k / SIDES), z) for k in range(SIDES)]
        rings.append(mesh.verts(ring))
    mesh.begin()
    for i in range(len(rings) - 1):
        mat = PROFILE[i][2]
        for k in range(SIDES):
            j = (k + 1) % SIDES
            mesh.face([rings[i][k], rings[i][j], rings[i + 1][j], rings[i + 1][k]], mat)
    mesh.face(list(reversed(rings[0])), M_SILVER)
    mesh.face(list(rings[-1]), M_SILVER)
    mesh.end()


def barrel_apothem(f):
    """Apotema (distancia al centro de una cara plana) del canon en la posicion f."""
    (f0, r0, _), (f1, r1, _) = PROFILE[14], PROFILE[15]
    r = r0 + (r1 - r0) * (f - f0) / (f1 - f0)
    return r * K * math.cos(math.pi / SIDES)


def plate(mesh, poly, z_c, a, h, mat):
    """Pieza del escudo: poligono 2D (u, v) extruido sobre la cara plana del canon que mira a -Y.
    La base queda hundida dentro del canon; la cara superior sobresale h cm."""
    pts, tris = triangulate([(u * K * EMBLEM_SCALE, v * K * EMBLEM_SCALE) for u, v in poly])
    y_top, y_bot = -(a + h), -(a - EMBLEM_EMBED)
    top = mesh.verts([(u, y_top, z_c + v) for u, v in pts])
    bot = mesh.verts([(u, y_bot, z_c + v) for u, v in pts])
    mesh.begin()
    for t in tris:
        mesh.face([top[t[0]], top[t[1]], top[t[2]]], mat)
        mesh.face([bot[t[2]], bot[t[1]], bot[t[0]]], mat)
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        mesh.face([top[j], top[i], bot[i], bot[j]], mat)
    mesh.end()


def ellipse(cu, cv, ru, rv, a0, a1, n):
    return [(cu + ru * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cv + rv * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def emblem(mesh):
    """Escudo nacional estilizado en relieve: campo celeste/blanco, sol, laurel, gorro con pica,
    manos y lazo (medidas sacadas de la foto: ~2,8 x 4,3 cm, x1,2 = 3,4 x 5,2 cm sobre el canon)."""
    z_c = (1.0 - EMBLEM_F) * LENGTH_CM
    a = barrel_apothem(EMBLEM_F)
    oc = (0.0, -0.18)                               # centro del ovalo
    # campo: mitad superior celeste, mitad inferior blanca
    plate(mesh, ellipse(oc[0], oc[1], 0.94, 1.34, 0, 180, 8), z_c, a, H_FIELD, M_BLUE)
    plate(mesh, ellipse(oc[0], oc[1], 0.94, 1.34, 180, 360, 8), z_c, a, H_FIELD, M_WHITE)
    # sol: media estrella de 7 rayos sobre el ovalo
    sun = []
    for k in range(13):
        ang = math.radians(180.0 * k / 12.0)
        r = 1.14 if k % 2 == 0 else 0.78
        sun.append((r * math.cos(ang), 1.06 + r * math.sin(ang)))
    plate(mesh, sun, z_c, a, H_SUN, M_GOLD)
    # laurel: dos ramas en arco alrededor del ovalo (la izquierda y su espejo)
    cl = (0.0, -0.28)
    left = ellipse(cl[0], cl[1], 1.42, 1.68, 140, 258, 4) + ellipse(cl[0], cl[1], 1.00, 1.30, 258, 140, 4)
    plate(mesh, left, z_c, a, H_WREATH, M_GOLD)
    plate(mesh, [(-u, v) for u, v in left], z_c, a, H_WREATH, M_GOLD)
    # pica con gorro frigio y manos entrelazadas
    plate(mesh, [(-0.05, -0.60), (0.05, -0.60), (0.05, 0.32), (-0.05, 0.32)], z_c, a, H_POLE, M_WOOD)
    plate(mesh, [(-0.30, 0.30), (0.30, 0.30), (0.28, 0.55), (0.14, 0.85), (-0.10, 0.92), (-0.26, 0.65)],
          z_c, a, H_CAP, M_WOOD)
    plate(mesh, [(-0.80, -0.45), (-0.30, -0.30), (0.40, -0.38), (0.85, -0.55), (0.30, -0.72), (-0.45, -0.68)],
          z_c, a, H_HANDS, M_GOLD)
    # lazo celeste al pie del laurel
    plate(mesh, [(-0.40, -1.56), (-0.05, -1.72), (0.05, -1.72), (0.40, -1.56),
                 (0.40, -1.96), (0.05, -1.80), (-0.05, -1.80), (-0.40, -1.96)], z_c, a, H_BOW, M_BLUE)


# ============================================================ CONSTRUCCION
def build_mesh():
    mesh = Mesh()
    lathe(mesh)
    emblem(mesh)

    # --- pose y origen (todo en cm hasta aca; al final -> m)
    if POSE == "parado":
        pts = list(mesh.v)                                     # eje Z, punta abajo, escudo hacia -Y
    else:
        pts = [(x, z, -y) for x, y, z in mesh.v]               # acostado: eje Y, escudo hacia +Z
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]; zs = [p[2] for p in pts]
    if POSE == "parado":
        cx, cy = 0.0, 0.0
    else:
        cx, cy = 0.0, (min(ys) + max(ys)) / 2.0
    z0 = min(zs)
    mesh.v = [((x - cx) / 100.0, (y - cy) / 100.0, (z - z0) / 100.0) for x, y, z in pts]
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
    print("%s  (pose: %s)" % (OBJECT_NAME, POSE))
    print("Triangulos: %d  |  caras: %d  |  vertices: %d" % (r["triangulos"], r["caras"], r["vertices"]))
    print("Medidas (X, Y, Z): %.1f x %.1f x %.1f cm" % r["dim_cm"])
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
            bsdf.inputs["Roughness"].default_value = 0.6
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
