"""Read-only extraction of registration evidence from the installed game's ZIP.

Python's ZIP reader handles this archive's local headers; .NET Framework 4.7.2
rejects some entries. Output is a local test input, never part of the mod package.
"""
import json
from pathlib import Path, PurePosixPath
import sys
import zipfile

NAMES = {
    "BarbarianClass", "SkaldClass", "BloodragerClass", "PrimalistArchetype",
    "FearlessRageFeature", "FearlessRageRageBuff", "RagePowerSelection",
    "SkaldRagePowerSelection", "InstinctualWarriorRagePowerSelection",
    "ExtraRagePowerSelection", "StandartRageBuff", "StandartFocusedRageBuff",
    "PrimalistRagePowersBuff", "InspiredRageEffectBuff", "InspiredRageEffectBuffMythic",
    "LethalStanceFeature", "LethalStanceEffectBuff",
}
records = []
with zipfile.ZipFile(sys.argv[1]) as archive:
    for entry in archive.infolist():
        name = PurePosixPath(entry.filename).stem
        if name in NAMES and entry.filename.endswith(".jbp"):
            data = json.loads(archive.read(entry))
            data["TestName"] = name
            data["TestSourcePath"] = entry.filename
            records.append(data)
assert len(records) == len(NAMES), "Missing or ambiguous native registration asset"
Path(sys.argv[2]).write_text(json.dumps(records, ensure_ascii=False), encoding="utf-8")
print(f"Extracted {len(records)} native registration assets (read-only; not packaged).")
