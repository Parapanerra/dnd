"""Keep only item rules referenced by the existing Artificer plan tables; no art."""
import hashlib
import json
import re
import urllib.request
from pathlib import Path

root = Path(__file__).resolve().parents[2]
revision = '8790755429c346d20020eed7826d312d484c393f'
code = (root/'Assets/Skripts/Domain/CharacterArtificerPlans.cs').read_text(encoding='utf-8')
names = {name.lower().removeprefix('replicate:') for name in re.findall(r'new ArtificerPlanOption\(\d+, \d+, "([^"]+)"', code) if not name.startswith('custom.')}
url = f'https://raw.githubusercontent.com/5etools-mirror-3/5etools-src/{revision}/data/items.json'
raw = urllib.request.urlopen(url, timeout=45).read()
items = json.loads(raw)['item']
selected = [item for item in items if item['name'].lower() in names and item['source'] in ('DMG','XDMG','EFA','ERLW','TCE','FTD')]
# Preserve named copy parents as well, so inherited rules can be inspected.
parents = {(item['_copy']['name'], item['_copy']['source']) for item in selected if '_copy' in item}
selected += [item for item in items if (item['name'],item['source']) in parents and item not in selected]
out = root/'tools/character-data/cache/artificer-plan-items.json'
variant_url = url.replace('items.json','magicvariants.json')
variant_raw = urllib.request.urlopen(variant_url, timeout=45).read()
variants = [v for v in json.loads(variant_raw)['magicvariant'] if v['name'].lower() in names and v.get('inherits',{}).get('source',v.get('source')) in ('DMG','XDMG','EFA')]
out.write_text(json.dumps({'source':url,'sourceSha256':hashlib.sha256(raw).hexdigest(),'variantSource':variant_url,'variantSha256':hashlib.sha256(variant_raw).hexdigest(),'item':selected,'magicvariant':variants}, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'Kept {len(selected)} relevant item records ({out.stat().st_size} bytes); full catalogue was not saved.')
