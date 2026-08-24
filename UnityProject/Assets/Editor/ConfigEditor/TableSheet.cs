using System.Collections.Generic;

namespace WorkCard.Editor.Config
{
    public class TableSheet
    {
        public string Name;
        public string File;
        public readonly List<List<string>> Rows = new List<List<string>>();

        public int RowCount => Rows.Count;

        public int ColCount
        {
            get
            {
                var max = 0;
                foreach (var row in Rows)
                {
                    if (row.Count > max)
                    {
                        max = row.Count;
                    }
                }

                return max;
            }
        }

        public string Cell(int row, int col)
        {
            if (row < 0 || row >= Rows.Count || col < 0 || col >= Rows[row].Count)
            {
                return "";
            }

            return Rows[row][col] ?? "";
        }
    }
}
