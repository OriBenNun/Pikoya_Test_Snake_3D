"""Copy font/license and shader source from this project's installed Unity uGUI resources.

Unity imports the source and generates metadata. No package GUIDs or serialized assets are edited.
"""
import pathlib
import tarfile

root = pathlib.Path(__file__).resolve().parent.parent
package = next((root / 'Library/PackageCache').glob('com.unity.ugui*/Package Resources/TMP Essential Resources.unitypackage'))
output = root / 'Assets/GardenSnake/UI'
with tarfile.open(package) as archive:
    for member in archive.getmembers():
        if not member.name.endswith('/pathname'):
            continue
        path = archive.extractfile(member).read().decode().strip()
        name = pathlib.PurePosixPath(path).name
        if name in ('LiberationSans.ttf', 'LiberationSans - OFL.txt'):
            target = output / name
        elif '/Shaders/' in path and name.endswith(('.shader', '.cginc', '.hlsl')):
            target = output / 'Shaders' / name
        else:
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(archive.extractfile(member.name.rsplit('/', 1)[0] + '/asset').read())
        print(target.relative_to(root))
