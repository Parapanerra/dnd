import json,re
from pathlib import Path
root=Path(__file__).resolve().parents[2]
names={
'Rat':('Щур','Крыса'),'Spider':('Павук','Паук'),'Wolf':('Вовк','Волк'),'Riding Horse':('Верховий кінь','Верховая лошадь'),
'Cat':('Кіт','Кошка'),'Badger':('Борсук','Барсук'),'Boar':('Кабан','Кабан'),'Elk':('Лось','Лось'),
'Crocodile':('Крокодил','Крокодил'),'Black Bear':('Чорний ведмідь','Чёрный медведь'),'Brown Bear':('Бурий ведмідь','Бурый медведь'),
'Dire Wolf':('Лютий вовк','Лютый волк'),'Giant Eagle':('Гігантський орел','Гигантский орёл'),'Polar Bear':('Білий ведмідь','Белый медведь'),
'Giant Scorpion':('Гігантський скорпіон','Гигантский скорпион'),'Elephant':('Слон','Слон'),'Giant Crocodile':('Гігантський крокодил','Гигантский крокодил'),'Mammoth':('Мамонт','Мамонт'),
'Air Elemental':('Повітряний елементаль','Воздушный элементаль'),'Earth Elemental':('Земляний елементаль','Земляной элементаль'),'Fire Elemental':('Вогняний елементаль','Огненный элементаль'),'Water Elemental':('Водяний елементаль','Водяной элементаль')}
actions={
'Bite':('Укус','Укус'),'Claw':('Кіготь','Коготь'),'Claws':('Кігті','Когти'),'Hooves':('Копита','Копыта'),'Hoof':('Копито','Копыто'),'Ram':('Таран','Таран'),'Tusk':('Ікла','Клыки'),'Tusks':('Ікла','Клыки'),
'Beak':('Дзьоб','Клюв'),'Talons':('Пазурі','Когти'),'Sting':('Жало','Жало'),'Gore':('Бивні','Бивни'),'Stomp':('Тупіт','Топот'),'Tail':('Хвіст','Хвост'),
'Slam':('Удар','Удар'),'Touch':('Дотик','Касание'),'Tusk Gore':('Удар бивнями','Удар бивнями'),
'Burn':('Опік','Ожог'),'Rend':('Розривання','Разрывание'),'Rock Launch':('Кидок каменя','Бросок камня'),'Scratch':('Подряпина','Царапина'),'Thunderous Slam':('Громовий удар','Громовой удар')}
skill_ids={'acrobatics':0,'animal handling':1,'arcana':2,'athletics':3,'deception':4,'history':5,'insight':6,'intimidation':7,'investigation':8,'medicine':9,'nature':10,'perception':11,'performance':12,'persuasion':13,'religion':14,'sleight of hand':15,'stealth':16,'survival':17}
def speed(x):return int(x.get('number',0)) if isinstance(x,dict) else int(x or 0)
def cr(x):
 if isinstance(x,dict):x=x['cr']
 return eval(x,{'__builtins__':{}},{})
records=[];source=[];translations=[]
for year,filename in [(2014,'Temp/druid-mm-source.json'),(2024,'Temp/druid-xmm-source.json')]:
 for monster in json.loads((root/'tools/character-data/druid-beasts-source.json').read_text(encoding='utf-8'))['monster']:
  if monster['source'] != ('MM' if year==2014 else 'XMM'):continue
  name=monster['name']
  if name not in names or year==2024 and 'Elemental' in name:continue
  source.append(monster)
  def ac():
   x=monster['ac'][0];return x['ac'] if isinstance(x,dict) else x
  attacks=[]
  for action in monster.get('action',[]):
   text=json.dumps(action.get('entries',[]),ensure_ascii=False)
   hit=re.search(r'\{@hit ([+-]?\d+)\}',text)
   if not hit:continue
   label=action['name'];assert label in actions,(year,name,label)
   ua,ru=actions[label];translations.append((ua,label,ru))
   tail=text.split('{@h}',1)[1];fixed=re.match(r'(\d+) (?:Piercing|Bludgeoning|Slashing|Fire|Cold|Lightning|Thunder|Poison|Necrotic|Radiant|Force|Psychic) damage',tail,re.I)
   die=re.search(r'\{@damage ([^}]+)\}',tail)
   damage=fixed.group(1) if fixed else die.group(1).replace(' ','') if die else ''
   attacks.append({'name':ua,'bonus':int(hit.group(1)),'damage':damage})
  sp=monster.get('speed',{})
  record={'id':'druid.'+str(year)+'.'+name.lower().replace(' ','-'),'name':names[name][0],'sourceName':name,'edition':year,'cr':cr(monster['cr']),'elemental':'Elemental' in name,'ac':ac(),'hp':monster['hp']['average'],'speed':speed(sp.get('walk')),'fly':speed(sp.get('fly')),'swim':speed(sp.get('swim')),'climb':speed(sp.get('climb')),'burrow':speed(sp.get('burrow')),'scores':[monster[k] for k in ('str','dex','con','int','wis','cha')],'passive':monster.get('passive',10),'attacks':attacks,'skills':[{'index':skill_ids[k],'bonus':int(v)} for k,v in monster.get('skill',{}).items() if k in skill_ids],'saves':[{'index':('str','dex','con','int','wis','cha').index(k),'bonus':int(v)} for k,v in monster.get('save',{}).items() if k in ('str','dex','con','int','wis','cha')]}
  notes=json.loads((root/'tools/character-data/druid-beast-notes.json').read_text(encoding='utf-8'))
  keys=[]
  if year==2014 and name in ['Badger','Black Bear','Brown Bear','Cat','Polar Bear','Rat']:keys.append('keen')
  if year==2014 and name in ['Wolf','Dire Wolf']:keys.append('hearing')
  if name in ['Wolf','Dire Wolf']:keys.append('pack')
  if name=='Spider':keys.append('climb')
  if name in ['Black Bear','Polar Bear'] or year==2014 and name=='Brown Bear':keys.append('bear14' if year==2014 else 'bear24')
  if str(year)+'.'+name in notes:keys.append(str(year)+'.'+name)
  values=[notes[k] for k in keys]
  words={'darkvision':('Темнозір','Тёмное зрение'),'blindsight':('Сліпозір','Слепое зрение'),'tremorsense':('Треморчуття','Чувство вибрации'),
    'necrotic':('некротична','некротический'),'radiant':('промениста','лучистый'),'psychic':('психічна','психический'),'force':('силова','силовой'),'charmed':('зачарований','очарован'),'frightened':('наляканий','испуган'),'acid':('кислотна','кислотный'),'cold':('холодна','холодный'),'fire':('вогняна','огненный'),'lightning':('блискавична','электрический'),'thunder':('громова','звуковой'),'poison':('отруйна','ядом'),
    'bludgeoning':('дробляча','дробящий'),'piercing':('колюча','колющий'),'slashing':('рубляча','рубящий'),
    'exhaustion':('втома','истощение'),'grappled':('схоплений','схвачен'),'paralyzed':('паралізований','парализован'),'petrified':('скам’янілий','окаменевший'),'poisoned':('отруєний','отравлен'),'prone':('збитий з ніг','сбит с ног'),'restrained':('обмежений','опутан'),'unconscious':('непритомний','без сознания')}
  for sense in monster.get('senses',[]):
   match=re.match(r'(darkvision|blindsight|tremorsense) (\d+) ft',sense)
   if match:
    key,num=match.groups();values.append([words[key][0]+' '+num+' футів.',key.title()+' '+num+' feet.',words[key][1]+' '+num+' футов.'])
  for field,labels in [('resist',('Опір: ','Resistance: ','Сопротивление: ')),('immune',('Імунітет до шкоди: ','Damage immunity: ','Иммунитет к урону: ')),('conditionImmune',('Імунітет до станів: ','Condition immunity: ','Иммунитет к состояниям: ')),('vulnerable',('Вразливість: ','Vulnerability: ','Уязвимость: '))]:
   for item in monster.get(field,[]):
    conditional=isinstance(item,dict);parts=item.get(field,[]) if conditional else [item]
    if not parts:continue
    for part in parts:assert part in words,part
    values.append([labels[0]+', '.join(words[x][0] for x in parts)+(' від немагічних атак.' if conditional else '.'),labels[1]+', '.join(parts)+(' from nonmagical attacks.' if conditional else '.'),labels[2]+', '.join(words[x][1] for x in parts)+(' от немагических атак.' if conditional else '.')])
  record['notes']='\n'.join(v[0] for v in values)
  translations.extend(tuple(v) for v in values)
  records.append(record);translations.append((record['name'],name,names[name][1]))
assert len(records)==40,len(records)
(root/'tools/character-data/druid-beasts-source.json').write_text(json.dumps({'revision':'8790755429c346d20020eed7826d312d484c393f','monster':source},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(root/'Assets/Resources/CharacterCreation/DruidBeasts.json').write_text(json.dumps({'entries':records},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=root/'Assets/Resources/Localization.txt';old=p.read_text(encoding='utf-8-sig');known={s.split('\t')[0] for s in old.splitlines()};new=[]
for values in translations:
 if values[0] not in known:new.append('\t'.join(values));known.add(values[0])
p.write_text(old.rstrip('\n')+'\n'+'\n'.join(new)+'\n',encoding='utf-8')
print('Druid beast records',len(records),'translations',len(new))
