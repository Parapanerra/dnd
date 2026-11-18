using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Taruck.CharacterCreation
{
    // Text fields retain whole named abilities, with explicit finite capacity.
    internal static class CreationDescriptionLayout
    {
        internal const int FieldCapacity = 2800;
        internal static bool Tracks(int index) => index>=54&&index<=56 || index>=60&&index<=65 || index==76 || index==77 || index==82 || index>=78&&index<=81 || index>=102&&index<=105;
        internal static string Fingerprint(string text)
        {
            ulong hash=14695981039346656037UL;
            unchecked{foreach(char value in text??""){hash^=value;hash*=1099511628211UL;}}
            return hash.ToString("X16");
        }

        internal static string[] Paragraphs(string value) => Regex.Split(value ?? "",
                @"\r?\n+|(?<=[.!?]) +(?=(?!(?:Використань|Використання|Uses|Использований|Использования):)[\p{Lu}][\p{L}’' –—-]{2,45}:)")
            .Select(p=>p.Trim()).Where(p=>p.Length>0).Distinct(StringComparer.Ordinal).ToArray();

        internal static string Primary(string[] paragraphs, out string[] remainder)
        {
            int take=0;string text="";
            while(take<paragraphs.Length&&take<3)
            {
                string next=text.Length==0?paragraphs[take]:text+"\n\n"+paragraphs[take];
                if(next.Length>FieldCapacity)break;
                text=next;take++;
            }
            remainder=paragraphs.Skip(take).ToArray();
            return text;
        }

        internal static string[] Pack(IEnumerable<string> source, int count=8)
        {
            var paragraphs=source.Where(p=>!string.IsNullOrWhiteSpace(p)).SelectMany(SplitLong).ToArray();
            var notes=new string[count];
            for(int i=0;i<count;i++)notes[i]="";
            // Distribute by real character capacity rather than overflowing the last note.
            int total=paragraphs.Sum(p=>p.Length+2);
            int target=Math.Min(FieldCapacity,Math.Max(900,(total+count-1)/count));
            if(!TryPack(paragraphs,notes,target))
            {
                Array.Fill(notes,"");
                if(!TryPack(paragraphs,notes,FieldCapacity))
                    throw new InvalidOperationException("Generated ability descriptions exceed the available note fields.");
            }
            return notes;
        }

        private static bool TryPack(string[] paragraphs,string[] notes,int target)
        {
            int index=0;
            foreach(string paragraph in paragraphs)
            {
                string next=notes[index].Length==0?paragraph:notes[index]+"\n\n"+paragraph;
                if(notes[index].Length>0&&next.Length>target&&index<notes.Length-1){index++;next=paragraph;}
                if(next.Length>FieldCapacity)return false;
                notes[index]=next;
            }
            return true;
        }

        private static IEnumerable<string> SplitLong(string paragraph)
        {
            string remaining=paragraph.Trim();
            while(remaining.Length>FieldCapacity)
            {
                int split=remaining.LastIndexOf(' ',FieldCapacity);
                if(split<1)split=FieldCapacity;
                yield return remaining.Substring(0,split).TrimEnd();
                remaining=remaining.Substring(split).TrimStart();
            }
            if(remaining.Length>0)yield return remaining;
        }
    }
}
