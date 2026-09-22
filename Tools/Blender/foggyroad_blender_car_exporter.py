bl_info = {
    "name": "Foggy Road Car Exporter",
    "author": "Foggy Road",
    "version": (1, 0, 0),
    "blender": (3, 6, 0),
    "location": "View3D > Sidebar > FoggyRoad / File > Export",
    "description": "Exports the complete Blender car scene to a Unity-ready FBX package with a material manifest and copied textures.",
    "category": "Import-Export",
}

import bpy
import json
import os
import re
import shutil
import sys
from pathlib import Path

from bpy.props import StringProperty


ROLE_BY_SOCKET = {
    "Base Color": "baseMap",
    "Metallic": "metallicMap",
    "Roughness": "roughnessMap",
    "Emission Color": "emissionMap",
}


def _clean_name(value):
    value = re.sub(r"[^A-Za-z0-9_.-]+", "_", value or "asset")
    return value.strip("._") or "asset"


def _float_value(value):
    try:
        return float(value)
    except (TypeError, ValueError):
        return 0.0


def _color_value(socket, fallback=(1.0, 1.0, 1.0, 1.0)):
    if socket is None or socket.is_linked:
        return list(fallback)
    value = socket.default_value
    return [_float_value(value[i]) for i in range(min(4, len(value)))]


def _material_socket(node, name):
    return node.inputs.get(name) if node else None


def _object_path(obj):
    parts = []
    current = obj
    while current is not None:
        parts.append(current.name)
        current = current.parent
    parts.reverse()
    return "/".join(parts)


def _find_principled(material):
    if not material or not material.use_nodes:
        return None
    return next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)


def _copy_image(image, texture_dir, image_cache, used_names):
    if image is None:
        return None

    cache_key = image.name
    if cache_key in image_cache:
        return image_cache[cache_key]

    source_path = bpy.path.abspath(image.filepath) if image.filepath else ""
    source_name = Path(source_path).name if source_path else image.name
    source_ext = Path(source_name).suffix.lower() or ".png"
    stem = _clean_name(Path(source_name).stem or image.name)
    candidate = f"{stem}{source_ext}"
    index = 2
    while candidate in used_names:
        candidate = f"{stem}_{index}{source_ext}"
        index += 1

    destination = texture_dir / candidate
    try:
        if image.packed_file:
            image.save_render(str(destination))
        elif source_path and os.path.isfile(source_path):
            shutil.copy2(source_path, destination)
        else:
            image.save_render(str(destination))
    except Exception as exc:
        print(f"[FoggyRoad] Texture export failed: {image.name}: {exc}")
        return None

    used_names.add(candidate)
    relative = f"Textures/{candidate}"
    image_cache[cache_key] = relative
    return relative


def _material_record(material, texture_dir, image_cache, used_names):
    principled = _find_principled(material)
    record = {
        "name": material.name,
        "baseColor": _color_value(_material_socket(principled, "Base Color")),
        "metallic": _float_value(_material_socket(principled, "Metallic").default_value if _material_socket(principled, "Metallic") else 0.0),
        "roughness": _float_value(_material_socket(principled, "Roughness").default_value if _material_socket(principled, "Roughness") else 0.5),
        "alpha": _float_value(_material_socket(principled, "Alpha").default_value if _material_socket(principled, "Alpha") else 1.0),
        "baseMap": None,
        "normalMap": None,
        "metallicMap": None,
        "roughnessMap": None,
        "emissionMap": None,
    }

    if not principled:
        return record

    for link in material.node_tree.links:
        source = link.from_node
        target = link.to_node
        role = None
        if target == principled:
            role = ROLE_BY_SOCKET.get(link.to_socket.name)
        elif target.type == "NORMAL_MAP" and link.to_socket.name == "Color":
            role = "normalMap"

        if role and source.type == "TEX_IMAGE" and source.image:
            record[role] = _copy_image(source.image, texture_dir, image_cache, used_names)

    return record


def export_car(output_dir, scene=None):
    scene = scene or bpy.context.scene
    output_dir = Path(bpy.path.abspath(str(output_dir))).expanduser().resolve()
    output_dir.mkdir(parents=True, exist_ok=True)
    texture_dir = output_dir / "Textures"
    texture_dir.mkdir(parents=True, exist_ok=True)

    blend_stem = _clean_name(Path(bpy.data.filepath).stem or scene.name)
    fbx_path = output_dir / f"{blend_stem}.fbx"
    manifest_path = output_dir / "CarExportManifest.json"

    image_cache = {}
    used_texture_names = set()
    material_records = {}
    object_records = []

    exportable_types = {"MESH", "ARMATURE", "EMPTY", "CURVE", "SURFACE", "META", "FONT"}
    exportable_objects = [obj for obj in scene.objects if obj.type in exportable_types]

    for obj in exportable_objects:
        material_names = []
        if obj.type == "MESH":
            for slot in obj.material_slots:
                material = slot.material
                if material is None:
                    continue
                material_names.append(material.name)
                if material.name not in material_records:
                    material_records[material.name] = _material_record(
                        material, texture_dir, image_cache, used_texture_names
                    )

        object_records.append(
            {
                "name": obj.name,
                "path": _object_path(obj),
                "type": obj.type,
                "materials": material_names,
            }
        )

    old_selection = list(bpy.context.selected_objects)
    old_active = bpy.context.view_layer.objects.active
    try:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in exportable_objects:
            obj.select_set(True)
        if exportable_objects:
            bpy.context.view_layer.objects.active = exportable_objects[0]

        bpy.ops.export_scene.fbx(
            filepath=str(fbx_path),
            use_selection=True,
            object_types={"MESH", "ARMATURE", "EMPTY", "OTHER"},
            apply_unit_scale=True,
            bake_space_transform=False,
            use_space_transform=True,
            axis_forward="-Z",
            axis_up="Y",
            add_leaf_bones=False,
            bake_anim=False,
            use_mesh_modifiers=True,
            mesh_smooth_type="FACE",
            use_custom_props=True,
            path_mode="AUTO",
            embed_textures=False,
        )
    finally:
        bpy.ops.object.select_all(action="DESELECT")
        for obj in old_selection:
            if obj and obj.name in bpy.data.objects:
                obj.select_set(True)
        if old_active and old_active.name in bpy.data.objects:
            bpy.context.view_layer.objects.active = old_active

    manifest = {
        "format": "FoggyRoadCarExport",
        "version": 1,
        "sourceBlend": bpy.data.filepath,
        "fbx": fbx_path.name,
        "rootName": blend_stem,
        "objects": object_records,
        "materials": sorted(material_records.values(), key=lambda item: item["name"].lower()),
    }
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[FoggyRoad] Export complete: {fbx_path}")
    print(f"[FoggyRoad] Objects: {len(object_records)} | Materials: {len(material_records)} | Textures: {len(image_cache)}")
    return str(fbx_path), str(manifest_path)


class FRCAR_OT_export(bpy.types.Operator):
    bl_idname = "frcar.export_unity_package"
    bl_label = "Export Car to Unity"
    bl_options = {"REGISTER", "UNDO"}

    directory: StringProperty(subtype="DIR_PATH")

    def invoke(self, context, event):
        self.directory = context.scene.frcar_output_dir or "//"
        context.window_manager.fileselect_add(self)
        return {"RUNNING_MODAL"}

    def execute(self, context):
        if not self.directory:
            self.report({"ERROR"}, "Select a Unity Assets destination folder.")
            return {"CANCELLED"}
        export_car(self.directory, context.scene)
        context.scene.frcar_output_dir = self.directory
        self.report({"INFO"}, "Unity car package exported.")
        return {"FINISHED"}


class FRCAR_PT_panel(bpy.types.Panel):
    bl_label = "Foggy Road Car Export"
    bl_idname = "FRCAR_PT_panel"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "FoggyRoad"

    def draw(self, context):
        layout = self.layout
        layout.prop(context.scene, "frcar_output_dir", text="Unity folder")
        layout.operator(FRCAR_OT_export.bl_idname, icon="EXPORT")
        layout.label(text="Exports FBX + textures + manifest")


def _menu_export(self, context):
    self.layout.operator(FRCAR_OT_export.bl_idname, text="Car for Unity (FBX + Materials)")


CLASSES = (FRCAR_OT_export, FRCAR_PT_panel)


def register():
    for cls in CLASSES:
        bpy.utils.register_class(cls)
    bpy.types.Scene.frcar_output_dir = StringProperty(
        name="Unity Export Folder",
        description="Folder inside the Unity project's Assets directory",
        subtype="DIR_PATH",
    )
    bpy.types.TOPBAR_MT_file_export.append(_menu_export)


def unregister():
    bpy.types.TOPBAR_MT_file_export.remove(_menu_export)
    if hasattr(bpy.types.Scene, "frcar_output_dir"):
        del bpy.types.Scene.frcar_output_dir
    for cls in reversed(CLASSES):
        bpy.utils.unregister_class(cls)


def _command_line_output():
    args = sys.argv
    if "--frcar-export" not in args:
        return None
    index = args.index("--frcar-export")
    if index + 1 >= len(args):
        raise RuntimeError("--frcar-export requires an output folder")
    return args[index + 1]


if __name__ == "__main__":
    register()
    output = _command_line_output()
    if output:
        export_car(output)
