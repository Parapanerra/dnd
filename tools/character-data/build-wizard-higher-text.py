"""Build the curated, localized 2nd–4th-level Wizard spell summaries."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent


def build():
    texts = {}
    translations = []
    for line in (SOURCE / "wizard-higher-spells.tsv").read_text(encoding="utf-8-sig").splitlines():
        if not line.strip():
            continue
        name, ukrainian_name, russian_name, ua, en, ru = line.split("\t")
        texts[name] = {"name": name, "classic": ua, "revised": ua}
        translations.extend(((ukrainian_name, name, russian_name), (ua, en, ru)))
    for line in (SOURCE / "wizard-higher-overrides.tsv").read_text(encoding="utf-8-sig").splitlines():
        if not line.strip():
            continue
        name, ua, en, ru = line.split("\t")
        texts[name]["revised"] = ua
        translations.append((ua, en, ru))
    output = ROOT / "Assets/Resources/CharacterCreation/WizardHigherSpellText.json"
    output.write_text(json.dumps({"entries": list(texts.values())}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    localization = ROOT / "Assets/Resources/Localization.txt"
    existing = localization.read_text(encoding="utf-8-sig")
    keys = {line.split("\t")[0] for line in existing.splitlines()}
    added = []
    for translation in translations:
        if translation[0] not in keys:
            added.append("\t".join(translation))
            keys.add(translation[0])
    localization.write_text(existing.rstrip("\n") + "\n" + "\n".join(added) + "\n", encoding="utf-8-sig")
    print(f"Built {len(texts)} higher Wizard spell summaries and added {len(added)} localization entries.")


if __name__ == "__main__":
    build()
