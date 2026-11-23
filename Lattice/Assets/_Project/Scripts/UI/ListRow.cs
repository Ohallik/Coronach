using System;
using System.Linq;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>A list row laid out in columns (MenuList.Row): a cell of words for each column, and the words the row is found by. A row used to be one centred string, and the replay
    /// driver and the checks read that string; they read this now. The key is what the string started with: the thing's name, with whoever wears a part in front of it
    /// ("Sela · Refined Edge"), so a route that walks down to a spare "Scrap Edge" doesn't stop on the one Taren has on.</summary>
    public sealed class ListRow:MonoBehaviour
    {
        public string key;
        public string[] columns;
        public TMP_Text[] cells;
        /// <summary>The cell under this column's name, or null.</summary>
        public TMP_Text Cell(string column){int at=columns==null?-1:Array.IndexOf(columns,column);return at>=0&&at<cells.Length?cells[at]:null;}
        /// <summary>What a cell of this row says, or "" for a row that hasn't that column.</summary>
        public static string Cell(Component row,string column){var list=row!=null?row.GetComponent<ListRow>():null;var cell=list!=null?list.Cell(column):null;return cell!=null?cell.text:"";}
        /// <summary>Everything the row says, cell by cell in the order they stand, "  ·  " between: a column row's facts in one line. A row of one label is that label.
        /// A cell's colour is how it says better or worse, short or locked; the words are read without it ("+22", not "&lt;color=#9BE8A6&gt;+22&lt;/color&gt;"). Button sprites stay.</summary>
        public static string Words(Component row)
        {
            if(row==null)return "";
            var list=row.GetComponent<ListRow>();
            if(list==null){var label=row.GetComponentInChildren<TMP_Text>();return label!=null?Plain(label.text):"";}
            return string.Join("  ·  ",list.cells.Where(c=>c!=null&&c.gameObject.activeSelf&&!string.IsNullOrEmpty(c.text)).Select(c=>Plain(c.text)));
        }
        static string Plain(string text)=>System.Text.RegularExpressions.Regex.Replace(text??"","</?color[^>]*>","");
        /// <summary>The words a row is found by: a column row's key, or a plain row's label.</summary>
        public static string Key(GameObject row)
        {
            if(row==null)return "";
            var list=row.GetComponent<ListRow>();if(list!=null)return list.key??"";
            var label=row.GetComponentInChildren<TMP_Text>();return label!=null?label.text:"";
        }
        /// <summary>The replay driver's pickRow: this row by its object's name (a save's slot, "Pager"), or by the words it's found by starting so.</summary>
        public static bool Found(GameObject row,string want)=>row!=null&&(row.name==want||Key(row).StartsWith(want,StringComparison.Ordinal));
    }
}
