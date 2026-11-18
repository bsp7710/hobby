from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from make_cast import Artist, textures
from grip_source import make_grip
out=Path(__file__).resolve().parent.parent/'data'/'company.cast'
temporary=out.with_suffix('.partial')
with ZipFile(temporary,'w',ZIP_DEFLATED,compresslevel=7) as archive:
    textures(archive)
    for i in range(3):
        actor=Artist(i)
        actor.build()
        archive.writestr('role%d.rig'%i,actor.binary())
    make_grip(archive)
temporary.replace(out)
