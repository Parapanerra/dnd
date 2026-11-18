using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    // Localize generated overflow attacks without touching a player's edited attack notes.
    internal static class CreationAttackOverflow
    {
        private const string Key="CreationAttackOverflow_v1";
        internal sealed class Baseline { internal string text;internal string[] rows; }
        [Serializable] private sealed class Snapshot { public string[] suffixes;public string[] hashes; }
        private static string Read(CharacterData c,CreationSheetBindings b,int index)
        {
            var e=b.Entries.First(entry=>entry.key=="input."+index);var data=c.GetSceneData(e.scene);
            return data.GetString("StableField_v1_"+e.stableId,index<data.inputData.Count?data.inputData[index]:"");
        }
        internal static Baseline Capture(CharacterData c,CreationSheetBindings b)=>new Baseline{text=Read(c,b,75),rows=Enumerable.Range(66,9).Select(i=>Read(c,b,i)).ToArray()};
        private static string Label(string source,Func<string,string> t)
        {
            var match=Regex.Match(source,@"^(.*?)( \+\d+)$");
            return match.Success?t(match.Groups[1].Value)+match.Groups[2].Value:t(source);
        }
        internal static void Record(CharacterData c,CreationSheetBindings b,CharacterCreationDraft d,CreatedCharacterStats stats,string originalBase,Baseline before)
        {
            if(!before.text.StartsWith(originalBase,StringComparison.Ordinal))return;
            string priorSuffix=before.text.Substring(originalBase.Length);var snapshot=new Snapshot{suffixes=new string[3],hashes=new string[3]};var catalog=new TranslationCatalog();
            foreach(AppLanguage language in Enum.GetValues(typeof(AppLanguage)))
            {
                Func<string,string> t=s=>catalog.Translate(s,language);
                string baseText=CharacterCreationPresentation.GeneratedSheetFields(d,stats,t)[75];
                string translatedPrior=string.Join("\n",priorSuffix.Split('\n').Select(line=>{int colon=line.IndexOf(':');return colon<0?line:Label(line.Substring(0,colon),t)+line.Substring(colon);}));
                var candidate=new CharacterData(Guid.NewGuid().ToString("N"));b.Text(candidate,"input.75",baseText+translatedPrior);
                for(int i=0;i<before.rows.Length;i++)b.Text(candidate,"input."+(66+i),i%3==1?Label(before.rows[i],t):before.rows[i]);
                CreationStartingWeapons.Write(candidate,b,d,stats,t);CreationExpandedSpecies.WriteAttack(candidate,b,d,stats,t);
                string full=Read(candidate,b,75);if(!full.StartsWith(baseText,StringComparison.Ordinal))return;
                snapshot.suffixes[(int)language]=full.Substring(baseText.Length);snapshot.hashes[(int)language]=CreationDescriptionLayout.Fingerprint(full);
            }
            if(!snapshot.hashes.Contains(CreationDescriptionLayout.Fingerprint(Read(c,b,75))))return;
            if(snapshot.suffixes.All(string.IsNullOrEmpty))
            {
                c.DeleteSharedString(Key);
                return;
            }
            c.SetSharedString(Key,JsonUtility.ToJson(snapshot));
        }
        internal static bool Has(CharacterData c)=>!string.IsNullOrEmpty(c.GetSharedString(Key));
        internal static bool Refresh(CharacterData c,string current,IReadOnlyDictionary<AppLanguage,Dictionary<int,string>> variants,AppLanguage language,out string text)
        {
            text=current;Snapshot snapshot;
            if(current==null)return false;
            try{snapshot=JsonUtility.FromJson<Snapshot>(c.GetSharedString(Key));}catch(Exception){return false;}
            if(snapshot?.hashes==null||snapshot.suffixes==null||snapshot.hashes.Length!=3||snapshot.suffixes.Length!=3||!snapshot.hashes.Contains(CreationDescriptionLayout.Fingerprint(current)))return false;
            text=variants[language][75]+snapshot.suffixes[(int)language];
            foreach(var variant in variants)snapshot.hashes[(int)variant.Key]=CreationDescriptionLayout.Fingerprint(variant.Value[75]+snapshot.suffixes[(int)variant.Key]);
            c.SetSharedString(Key,JsonUtility.ToJson(snapshot));return true;
        }
    }
}
