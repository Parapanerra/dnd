using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // Optional creation-time equipment, separate from the native preparation checkboxes.
    public static class CharacterArtificerEquipment
    {
        public const string ScaleArmor = "armor.created.scale";
        public const string HalfPlate = "armor.created.halfplate";
        internal static bool IsCreatedArmor(string target) => target == ScaleArmor || target == HalfPlate;
        internal static int CreatedArmorBase(CharacterCreationDraft draft) => Selected(draft)
            .Select(slot => Target(draft, slot)).Where(IsCreatedArmor)
            .Select(target => target == HalfPlate ? 15 : 14).DefaultIfEmpty(0).Max();
        public static bool WearsArmor(CharacterCreationDraft draft) => CreatedArmorBase(draft) > 0 || WearsStarterArmor(draft);

        private static bool WearsStarterArmor(CharacterCreationDraft draft)
        {
            var owner = draft.PrimaryCharacter;
            return draft.IsSecondaryTrack ? CharacterNaturalArmor.StarterArmor(owner) && CharacterNaturalArmor.WearsArmor(owner) :
                (draft.edition == RulesEdition.Rules2014 || draft.equipment == 0) && CharacterNaturalArmor.WearsArmor(draft);
        }

        public static IEnumerable<(string target, string name)> Weapons(CharacterCreationDraft draft)
        {
            if (draft.IsSecondaryTrack)
            {
                var primary = draft.PrimaryCharacter;
                int slot = 0;
                foreach (string weapon in CharacterCreationRules.StartingWeapons(primary).Concat(CharacterBackgroundRules.Weapons(primary))
                             .Where(name => !string.IsNullOrWhiteSpace(name)).Distinct())
                    yield return ("weapon." + slot++, weapon);
                yield break;
            }
            if (!CharacterArtificerRules.Revised(draft))
            {
                yield return ("weapon.0", draft.artificerWeapon);
                yield return ("weapon.1", draft.artificerSecondWeapon);
                yield return ("crossbow", "Легкий арбалет");
                yield break;
            }

            var weapons = CharacterBackgroundRules.Weapons(draft);
            if (draft.equipment == 0) weapons = new[] { "Кинджал" }.Concat(weapons);
            int index = 0;
            foreach (string weapon in weapons)
                yield return ("weapon." + index++, weapon);
            if (draft.backgroundEquipmentChoice == 0 && (draft.background == "criminal" || draft.background == "wayfarer"))
                yield return ("weapon." + index, "Кинджал");
        }

        internal static string Weapon(CharacterCreationDraft draft, string target) =>
            Weapons(draft).FirstOrDefault(weapon => weapon.target == target).name ?? "";
        public static string TargetNumber(CharacterCreationDraft draft, string target)
        {
            string name = Weapon(draft, target);
            if (name.Length == 0) return "";
            string[] matching = Weapons(draft).Where(weapon => weapon.name == name).Select(weapon => weapon.target).ToArray();
            return matching.Length > 1 ? " (" + (Array.IndexOf(matching, target) + 1) + ")" : "";
        }
        public static string Label(CharacterCreationDraft d,string target)=>target==ScaleArmor?"Лускатий обладунок":target==HalfPlate?"Напівлати":target.StartsWith("worn.",StringComparison.Ordinal)?CharacterDescriptionText.Get("equip.worn"):target=="armor"?d.IsSecondaryTrack?"Споряджений обладунок":CharacterArtificerRules.Revised(d)?"Проклепана шкіра":d.artificerArmor:target=="armor.weapon"?CharacterDescriptionText.Get("equip.armorweapon"):target=="shield"?CharacterDescriptionText.Get("equip.shield"):Weapon(d,target);
        private static string Item(string plan)=>CharacterArtificerPlans.CanonicalName(plan.StartsWith("replicate:",StringComparison.Ordinal)?plan.Substring(10):plan);
        private static readonly string[] WornItems={"gauntlets of ogre power","headband of intellect","amulet of health","belt of hill giant strength","cloak of protection","ring of protection"};
        public static string[] Targets(CharacterCreationDraft d,int slot)
        {
            if(!CharacterArtificerRules.Active(d)||slot<0||slot>=d.artificerPlans.Count)return Array.Empty<string>();
            string plan=d.artificerPlans[slot];bool modern=CharacterArtificerRules.Revised(d);
            if(!CharacterArtificerPlans.All.Any(p=>p.edition==(modern?2024:2014)&&p.id==plan&&p.level<=d.level))return Array.Empty<string>();
            string item=Item(plan);
            if(WornItems.Contains(item))return new[]{"worn."+item};
            bool starter=WearsStarterArmor(d);
            if (modern && (plan == "+1 Armor" || plan == "+2 Armor") && d.PrimaryCharacter.species != "tortle")
                return starter
                    ? new[] { "armor", ScaleArmor, HalfPlate } : new[] { ScaleArmor, HalfPlate };
            if(plan=="Enhanced Defense")return starter?new[]{"armor"}:Array.Empty<string>();
            if(modern&&(plan=="+1 Shield"||plan=="+2 Shield"))return new[]{"shield"};
            if(!new[]{"Enhanced Weapon","Radiant Weapon","Repeating Shot","Returning Weapon","Dazzling Weapon","+1 Weapon","+2 Weapon"}.Contains(plan))return Array.Empty<string>();
            var targets = Weapons(d).Where(weapon => !string.IsNullOrEmpty(weapon.name)).ToList();
            if (starter && CharacterArtificerRules.Is(d, "armorer") && !d.IsSecondaryTrack)
                targets.Add(("armor.weapon", ""));
            return targets.Where(option =>
            {
                if(plan=="Repeating Shot")return new[]{"Легкий арбалет","Короткий лук","Праща"}.Contains(option.name);
                if(plan=="Returning Weapon")return new[]{"Кинджал","Ручна сокира","Метальний спис","Легкий молот","Спис","Дротик"}.Contains(option.name);
                return true;
            }).Select(option => option.target).ToArray();
        }
        public static string Target(CharacterCreationDraft d,int slot)=>d.artificerEquippedTargets!=null&&slot<d.artificerEquippedTargets.Count?d.artificerEquippedTargets[slot]??"":"";
        private static string PhysicalKey(CharacterCreationDraft d,string target)=>IsCreatedArmor(target)||target=="armor.weapon"&&!CharacterArtificerRules.Revised(d)&&d.level<9?"armor":target;
        private static bool ArmorExtra(CharacterCreationDraft d,string target)=>CharacterArtificerRules.Revised(d)?target=="armor"||IsCreatedArmor(target):target=="armor"||target=="armor.weapon";
        public static IEnumerable<int> Selected(CharacterCreationDraft d)=>Enumerable.Range(0,d.artificerPlans.Count).Where(i=>Targets(d,i).Contains(Target(d,i)));
        internal static bool RequiresAttunement(string id)=>WornItems.Contains(Item(id))||id=="Radiant Weapon"||id=="Dazzling Weapon"||id=="Repeating Shot";
        public static string ChoiceError(CharacterCreationDraft d)=>Check(d,-1,"");
        private static string Check(CharacterCreationDraft d,int overrideSlot,string overrideTarget)
        {
            if(!CharacterArtificerRules.Active(d))return null;
            string At(int i)=>i==overrideSlot?overrideTarget:Target(d,i);
            var chosen=Enumerable.Range(0,d.artificerPlans.Count).Where(i=>Targets(d,i).Contains(At(i))).ToArray();
            if(Enumerable.Range(0,d.artificerPlans.Count).Any(i=>At(i).Length>0&&!Targets(d,i).Contains(At(i))))return CharacterDescriptionText.Get("equip.error.target");
            if(chosen.Select(i=>PhysicalKey(d,At(i))).Distinct().Count()!=chosen.Length)return CharacterDescriptionText.Get("equip.error.duplicate");
            int extra=CharacterArtificerRules.Is(d,"armorer")&&d.level>=9?CharacterArtificerRules.Revised(d)?1:2:0;
            int regular=CharacterArtificerRules.ItemLimit(d)-extra;
            if(chosen.Length>CharacterArtificerRules.ItemLimit(d)||chosen.Count(i=>!ArmorExtra(d,At(i)))>regular)return CharacterDescriptionText.Get("equip.error.limit");
            if(chosen.Count(i=>RequiresAttunement(d.artificerPlans[i]))>CharacterArtificerRules.AttunementLimit(d))return CharacterDescriptionText.Get("equip.error.attunement");
            return null;
        }
        public static string[] AvailableTargets(CharacterCreationDraft d,int slot)=>Targets(d,slot).Where(target=>target==Target(d,slot)||Check(d,slot,target)==null).ToArray();
        public static void Repair(CharacterCreationDraft d)
        {
            d.artificerEquippedTargets??=new List<string>();
            int count=CharacterArtificerRules.Active(d)?d.artificerPlans.Count:0;
            while(d.artificerEquippedTargets.Count<count)d.artificerEquippedTargets.Add("");
            if(d.artificerEquippedTargets.Count>count)d.artificerEquippedTargets.RemoveRange(count,d.artificerEquippedTargets.Count-count);
            for(int i=0;i<count;i++)if(Target(d,i).Length>0&&!Targets(d,i).Contains(Target(d,i)))d.artificerEquippedTargets[i]="";
        }
        public static int Bonus(CharacterCreationDraft d,string target)
        {
            if(!CharacterArtificerRules.Active(d)||ChoiceError(d)!=null)return 0;
            int slot=Selected(d).Where(i=>Target(d,i)==target).DefaultIfEmpty(-1).First();
            if(slot<0)return 0;
            string plan=d.artificerPlans[slot];
            return plan.StartsWith("+2 ",StringComparison.Ordinal)||d.level>=10&&(plan=="Enhanced Defense"||plan=="Enhanced Weapon")?2:1;
        }
        private static IEnumerable<string> Worn(CharacterCreationDraft d)=>!CharacterArtificerRules.Active(d)||ChoiceError(d)!=null?Enumerable.Empty<string>():Selected(d).Where(i=>Target(d,i).StartsWith("worn.",StringComparison.Ordinal)).Select(i=>Item(d.artificerPlans[i]));
        internal static void ApplyScores(CharacterCreationDraft d,int[] scores)
        {
            foreach(string item in Worn(d))
            {
                int ability=item=="headband of intellect"?3:item=="amulet of health"?2:item=="gauntlets of ogre power"||item=="belt of hill giant strength"?0:-1;
                if(ability>=0)scores[ability]=Math.Max(scores[ability],item=="belt of hill giant strength"?21:19);
            }
        }
        public static int ProtectionBonus(CharacterCreationDraft d)=>Worn(d).Count(item=>item=="cloak of protection"||item=="ring of protection");
        internal static int WornArmorBonus(CharacterCreationDraft d) => Bonus(d, "armor") + Bonus(d, ScaleArmor) + Bonus(d, HalfPlate);
        internal static int ArmorBonus(CharacterCreationDraft d)=>WornArmorBonus(d)+(Bonus(d,"shield")>0?2+Bonus(d,"shield"):0)+ProtectionBonus(d);
    }
}
