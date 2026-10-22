# -*- coding: utf-8 -*-
"""
MATE  (guampa de cuerno con virolas de plata + bombilla)  -  low poly para Unity
================================================================================
Script de Blender (2.8 o superior). Construye el modelo desde cero con codigo
puro, sin depender de archivos externos.

USO
  Blender > pestana Scripting > abrir este archivo > Run Script.
  Fuera de Blender ("python mate_lowpoly.py") arma la malla, la verifica e
  imprime un resumen (triangulos, medidas en cm, si esta cerrada).

CONVENCIONES
  - Metros, tamano real (guampa de 12 cm de alto, bombilla incluida ~17,9 cm).
  - Eje Z arriba. Origen en el centro de la base (z = 0 = mesa).
  - Frente del objeto = lado -Y (vista Front de Blender), +X = derecha.
  - Un solo objeto "Mate", flat shading, 6 material slots de color plano.
  - Piezas (islas cerradas dentro del mismo objeto): guampa y bombilla.

MEDIDAS SACADAS DE LA FOTO (2048 x 2048 px)
  Se midio la silueta (fila por fila) y se llevo a cm tomando la guampa = 12 cm
  entre el centro de la base y el centro de la boca (1 px ~ 0,099 mm).
    cuerpo: ~4,3 cm de diametro en la parte angosta, ~6,1 cm en la boca
    curva del eje: -0,5 cm a la izquierda a media altura, +1,0 cm a la derecha
                   en la boca (el cuerno se abre hacia la derecha)
    virola inferior (plata): de la base a 1,95 cm de altura
    virola superior (plata): de 10,4 a 11,45 cm, labio de cuerno arriba (0,55 cm)
    bombilla: sale ~6 cm sobre la boca, inclinada 25-40 grados hacia la derecha,
              punta (boquilla) en x = 7,0 cm, z = 17,8 cm

LO QUE NO SE VE (supuestos)
  - La parte de atras es simetrica a la de adelante (seccion circular, el
    cuerno solo se curva en el plano de la foto, X-Z).
  - Boca cortada en horizontal; base plana (z = 0) con un pequeno chaflan.
  - La cavidad se corta a la altura de la yerba (~0,85 cm bajo el borde): lo de
    mas abajo es macizo, no se ve y ahorra poligonos.
  - La bombilla esta en el plano X-Z (y = 0) y se hunde 1,5 cm en la yerba; la
    parte que seria el filtro, dentro de la guampa, no se modela.
  - Los grabados de las virolas no se modelan (solo el color plata).
"""
import math

# --------------------------------------------------------------------------
# Si FBX_PATH tiene una ruta (ej. "C:/Proyecto/Assets/Models/Mate.fbx") el
# script exporta un .fbx para Unity. Si esta vacia ("") no exporta nada.
# --------------------------------------------------------------------------
FBX_PATH = ""

OBJECT_NAME = "Mate"
CM = 0.01            # el perfil se escribe en cm y se pasa a metros al final
N_SIDES = 10         # lados de la guampa
N_STRAW = 6          # lados de la bombilla

# Material slots: indice -> (nombre, color sRGB)
MATERIALS = [
    ("Mate_CuernoOscuro", "#3F3631"),   # 0 parte baja del cuerno
    ("Mate_CuernoClaro",  "#8A6B4B"),   # 1 parte alta del cuerno y labio
    ("Mate_Plata",        "#C4BFB8"),   # 2 virolas y detalle de la bombilla
    ("Mate_Yerba",        "#4F6030"),   # 3 yerba
    ("Mate_Bombilla",     "#B98D55"),   # 4 bombilla (laton/dorado)
    ("Mate_Interior",     "#3A2A1E"),   # 5 paredes de la cavidad y anillos oscuros
]
OSC, CLARO, PLATA, YERBA, BOMBA, INTERIOR = range(6)

# --------------------------------------------------------------------------
# PERFIL DE LA GUAMPA  (cm)
# --------------------------------------------------------------------------
# Eje del cuerno: (z, x). Se interpola linealmente.
AXIS = [(0.00, 0.00), (0.30, -0.02), (1.00, -0.10), (1.95, -0.24), (3.20, -0.38),
        (4.60, -0.54), (6.20, -0.51), (7.90, -0.29), (9.20, 0.08), (10.37, 0.51),
        (11.45, 0.94), (12.00, 1.05)]

# Anillos de abajo hacia arriba: (z, radio_x, material de la tira hasta el siguiente)
# Los pares con el mismo z son escalones (la virola sobresale 0,05 cm).
HORN_RINGS = [
    (0.00, 1.92, PLATA),      # base (chaflan)
    (0.30, 2.17, PLATA),
    (1.95, 2.17, PLATA),      # borde superior de la virola inferior
    (1.95, 2.10, OSC),        # escalon hacia el cuerno
    (3.20, 2.11, OSC),
    (4.60, 2.15, OSC),        # parte mas angosta
    (6.20, 2.31, CLARO),      # cambio de color del cuerno
    (7.90, 2.47, CLARO),
    (9.20, 2.63, CLARO),
    (10.37, 2.85, PLATA),     # escalon hacia la virola superior
    (10.37, 2.90, PLATA),
    (11.45, 3.12, PLATA),     # borde superior de la virola superior
    (11.45, 3.06, CLARO),     # escalon hacia el labio
    (12.00, 3.06, CLARO),     # labio -> cara superior del borde
    (12.00, 2.68, INTERIOR),  # pared interior de la boca
    (11.15, 2.62, YERBA),     # borde de la yerba
    (11.35, 1.75, YERBA),     # monticulo de yerba -> punta
]
YERBA_APEX_Z = 11.60

# --------------------------------------------------------------------------
# BOMBILLA (cm). Eje en el plano X-Z: curva medida en la foto.
# --------------------------------------------------------------------------
STRAW_Z0, STRAW_X0, STRAW_B, STRAW_C = 12.926, 3.455, 0.4540, 0.0565


def straw_x(z):
    dz = z - STRAW_Z0
    if dz <= 0:                       # tramo recto hacia abajo, misma pendiente
        return STRAW_X0 + STRAW_B * dz
    return STRAW_X0 + STRAW_B * dz + STRAW_C * dz * dz


# (z, radio, material de la tira hasta el siguiente anillo)
STRAW_RINGS = [
    (9.80, 0.34, PLATA),      # extremo inferior (queda tapado dentro de la guampa)
    (12.40, 0.37, PLATA),
    (13.30, 0.37, PLATA),
    (13.30, 0.45, PLATA),     # collar de plata
    (13.55, 0.45, PLATA),
    (13.55, 0.37, BOMBA),
    (14.30, 0.36, INTERIOR),
    (14.30, 0.44, INTERIOR),  # anillo oscuro
    (14.50, 0.44, INTERIOR),
    (14.50, 0.36, BOMBA),
    (16.45, 0.35, INTERIOR),
    (16.45, 0.42, INTERIOR),  # anillo oscuro en la base de la boquilla
    (16.65, 0.42, INTERIOR),
    (16.65, 0.37, BOMBA),
    (17.30, 0.24, BOMBA),     # boquilla conica
    (17.83, 0.09, INTERIOR),  # punta (la tapa lleva el material oscuro)
]


# --------------------------------------------------------------------------
# Utilidades de interpolacion y geometria
# --------------------------------------------------------------------------
def interp(table, z):
    if z <= table[0][0]:
        return table[0][1]
    for (z0, v0), (z1, v1) in zip(table, table[1:]):
        if z <= z1:
            t = 0.0 if z1 == z0 else (z - z0) / (z1 - z0)
            return v0 + (v1 - v0) * t
    return table[-1][1]


def axis_x(z):
    return interp(AXIS, z)


def axis_slope(z, h=0.25):
    return (axis_x(z + h) - axis_x(z - h)) / (2 * h)


class Mesh:
    """Malla de triangulos. Posiciones en cm; al final se pasan a metros."""

    def __init__(self):
        self.verts = []
        self.tris = []   # (a, b, c)
        self.mats = []   # material por triangulo

    def vert(self, x, y, z):
        self.verts.append((x, y, z))
        return len(self.verts) - 1

    def tri(self, a, b, c, mat):
        self.tris.append((a, b, c))
        self.mats.append(mat)

    def bridge(self, ra, rb, mat, phase=0):
        """Une dos anillos con triangulos (diagonales alternadas)."""
        n = len(ra)
        for k in range(n):
            k2 = (k + 1) % n
            a, b, c, d = ra[k], ra[k2], rb[k2], rb[k]
            if (k + phase) % 2 == 0:
                self.tri(a, b, c, mat)
                self.tri(a, c, d, mat)
            else:
                self.tri(a, b, d, mat)
                self.tri(b, c, d, mat)

    def fan(self, ring, center, mat, up):
        n = len(ring)
        for k in range(n):
            k2 = (k + 1) % n
            if up:
                self.tri(ring[k], ring[k2], center, mat)
            else:
                self.tri(ring[k2], ring[k], center, mat)


def build_horn(m):
    rings = []
    for z, r, _mat in HORN_RINGS:
        cx = axis_x(z)
        ry = r * math.cos(math.atan(axis_slope(z)))   # seccion circular inclinada
        ring = []
        for k in range(N_SIDES):
            a = 2 * math.pi * k / N_SIDES
            ring.append(m.vert(cx + r * math.cos(a), ry * math.sin(a), z))
        rings.append(ring)
    for i in range(len(rings) - 1):
        m.bridge(rings[i], rings[i + 1], HORN_RINGS[i][2], phase=i)
    # base (cara de abajo)
    base_c = m.vert(axis_x(0.0), 0.0, 0.0)
    m.fan(rings[0], base_c, PLATA, up=False)
    # punta del monticulo de yerba
    apex = m.vert(axis_x(YERBA_APEX_Z), 0.0, YERBA_APEX_Z)
    m.fan(rings[-1], apex, YERBA, up=True)


def build_straw(m):
    rings = []
    for z, r, _mat in STRAW_RINGS:
        px, pz = straw_x(z), z
        h = 0.05
        tx, tz = straw_x(z + h) - straw_x(z - h), 2 * h
        ln = math.hypot(tx, tz)
        tx, tz = tx / ln, tz / ln
        e1 = (tz, 0.0, -tx)          # e1 x e2 = t   (e2 = +Y)
        ring = []
        for k in range(N_STRAW):
            a = 2 * math.pi * k / N_STRAW
            c, s = math.cos(a), math.sin(a)
            ring.append(m.vert(px + r * c * e1[0], r * s, pz + r * c * e1[2]))
        rings.append(ring)
    for i in range(len(rings) - 1):
        m.bridge(rings[i], rings[i + 1], STRAW_RINGS[i][2], phase=i)
    z0, z1 = STRAW_RINGS[0][0], STRAW_RINGS[-1][0]
    c0 = m.vert(straw_x(z0), 0.0, z0)
    c1 = m.vert(straw_x(z1), 0.0, z1)
    m.fan(rings[0], c0, PLATA, up=False)
    m.fan(rings[-1], c1, INTERIOR, up=True)   # tapa oscura = agujero de la boquilla


def build_mesh():
    m = Mesh()
    build_horn(m)
    build_straw(m)
    return m


# --------------------------------------------------------------------------
# Verificacion de la malla (python puro)
# --------------------------------------------------------------------------
def analyze(m):
    verts, tris = m.verts, m.tris
    # 1) aristas: cada una debe aparecer 2 veces, en sentidos opuestos
    edges = {}
    for t in tris:
        for i in range(3):
            a, b = t[i], t[(i + 1) % 3]
            edges.setdefault((min(a, b), max(a, b)), []).append(a < b)
    open_edges = sum(1 for d in edges.values() if len(d) != 2)
    bad_orient = sum(1 for d in edges.values() if len(d) == 2 and d[0] == d[1])
    # 2) islas (piezas) por vertices compartidos
    parent = list(range(len(verts)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    for a, b, c in tris:
        parent[find(a)] = find(b)
        parent[find(b)] = find(c)
    comps = {}
    for t in tris:
        comps.setdefault(find(t[0]), []).append(t)
    volumes = []
    for ts in comps.values():
        v = 0.0
        for a, b, c in ts:
            ax, ay, az = verts[a]
            bx, by, bz = verts[b]
            cx, cy, cz = verts[c]
            v += (ax * (by * cz - bz * cy) - ay * (bx * cz - bz * cx)
                  + az * (bx * cy - by * cx)) / 6.0
        volumes.append(v)
    # 3) triangulos degenerados y vertices sin usar
    degenerate = 0
    for a, b, c in tris:
        p, q, r = verts[a], verts[b], verts[c]
        u = (q[0] - p[0], q[1] - p[1], q[2] - p[2])
        w = (r[0] - p[0], r[1] - p[1], r[2] - p[2])
        cr = (u[1] * w[2] - u[2] * w[1], u[2] * w[0] - u[0] * w[2], u[0] * w[1] - u[1] * w[0])
        if math.sqrt(cr[0] ** 2 + cr[1] ** 2 + cr[2] ** 2) < 1e-9:
            degenerate += 1
    used = set(i for t in tris for i in t)
    xs = [v[0] for v in verts]
    ys = [v[1] for v in verts]
    zs = [v[2] for v in verts]
    return {
        "triangulos": len(tris),
        "vertices": len(verts),
        "piezas": len(comps),
        "volumen_cm3": [round(v, 2) for v in volumes],
        "aristas_abiertas": open_edges,
        "aristas_mal_orientadas": bad_orient,
        "triangulos_degenerados": degenerate,
        "vertices_sin_usar": len(verts) - len(used),
        "bbox_cm": ((min(xs), max(xs)), (min(ys), max(ys)), (min(zs), max(zs))),
        "cerrada": open_edges == 0 and bad_orient == 0,
        "normales_afuera": all(v > 0 for v in volumes),
    }


def print_report(info, label="Mate"):
    (x0, x1), (y0, y1), (z0, z1) = info["bbox_cm"]
    print("=" * 56)
    print("%s - resumen de la malla" % label)
    print("  triangulos : %d   (vertices: %d)" % (info["triangulos"], info["vertices"]))
    print("  medidas    : ancho X %.1f cm  |  fondo Y %.1f cm  |  alto Z %.1f cm"
          % (x1 - x0, y1 - y0, z1 - z0))
    print("  rango      : X %.2f..%.2f   Y %.2f..%.2f   Z %.2f..%.2f cm" % (x0, x1, y0, y1, z0, z1))
    print("  piezas     : %d   volumen de cada una (cm3): %s" % (info["piezas"], info["volumen_cm3"]))
    print("  cerrada    : %s   (aristas abiertas: %d, mal orientadas: %d)"
          % ("SI" if info["cerrada"] else "NO", info["aristas_abiertas"], info["aristas_mal_orientadas"]))
    print("  normales hacia afuera (volumenes > 0): %s" % ("SI" if info["normales_afuera"] else "NO"))
    print("  triangulos degenerados: %d   vertices sin usar: %d"
          % (info["triangulos_degenerados"], info["vertices_sin_usar"]))
    print("=" * 56)


# --------------------------------------------------------------------------
# Blender
# --------------------------------------------------------------------------
def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_to_linear(h):
    h = h.lstrip("#")
    return tuple(srgb_to_linear(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4))


def make_material(bpy, name, hexcolor):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    lin = hex_to_linear(hexcolor)
    mat.diffuse_color = (lin[0], lin[1], lin[2], 1.0)
    tree = getattr(mat, "node_tree", None)
    if tree is None:                    # Blender 2.8 a 4.x: hay que activar los nodos
        try:
            mat.use_nodes = True
            tree = mat.node_tree
        except Exception:
            tree = None
    if tree is not None:
        bsdf = tree.nodes.get("Principled BSDF")
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = (lin[0], lin[1], lin[2], 1.0)
            bsdf.inputs["Roughness"].default_value = 0.85
    return mat


def build_in_blender(m):
    import bpy

    # si se corre dos veces, borra el objeto anterior
    old = bpy.data.objects.get(OBJECT_NAME)
    if old is not None:
        bpy.data.objects.remove(old, do_unlink=True)
    old_mesh = bpy.data.meshes.get(OBJECT_NAME)
    if old_mesh is not None:
        bpy.data.meshes.remove(old_mesh)

    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    verts_m = [(x * CM, y * CM, z * CM) for x, y, z in m.verts]
    mesh = bpy.data.meshes.new(OBJECT_NAME)
    mesh.from_pydata(verts_m, [], [tuple(t) for t in m.tris])
    mesh.update()
    mesh.validate()

    obj = bpy.data.objects.new(OBJECT_NAME, mesh)
    scene.collection.objects.link(obj)
    obj.location = (0.0, 0.0, 0.0)       # origen = centro de la base

    for name, hexcolor in MATERIALS:
        mesh.materials.append(make_material(bpy, name, hexcolor))
    mesh.polygons.foreach_set("material_index", m.mats)
    mesh.polygons.foreach_set("use_smooth", [False] * len(mesh.polygons))   # flat shading
    mesh.update()

    # seleccionar y dejar activo
    try:
        for o in bpy.context.view_layer.objects:
            o.select_set(False)
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
    except Exception:
        pass
    return obj


def export_fbx(bpy, path):
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",          # normales planas (flat)
        add_leaf_bones=False,
    )
    print("FBX exportado en:", path)


def main():
    m = build_mesh()
    info = analyze(m)
    print_report(info, OBJECT_NAME)
    if not info["cerrada"] or not info["normales_afuera"]:
        print("ATENCION: la malla no paso la verificacion.")
    try:
        import bpy  # noqa: F401
    except ImportError:
        print("(Fuera de Blender: solo se verifico la malla, no se creo ningun objeto.)")
        return m
    obj = build_in_blender(m)
    print("Objeto creado: '%s' con %d material slots." % (obj.name, len(obj.material_slots)))
    if FBX_PATH:
        import bpy
        export_fbx(bpy, FBX_PATH)
    return m


if __name__ == "__main__":
    main()
