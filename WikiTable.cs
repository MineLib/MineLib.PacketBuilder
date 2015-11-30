using System.Collections.Generic;
using System.Linq;

namespace MineLib.PacketBuilder
{
    public class Cell
    {
        public int Collumn;
        public int CollumnLength;
        public int Row;

        public string Value;

        public Cell(int collumn, int row, int collumnLength, string value)
        {
            Collumn = collumn;
            CollumnLength = collumnLength;
            Row = row;

            Value = value;
        }

        public override string ToString() { return string.Format("{0}:{1}", Collumn, Row); }
    }
    public class WikiTable
    {
        struct RepeatCell
        {
            public int Collumn;
            public int Row;
            public int Count;

            public RepeatCell(int collumn, int row, int count)
            {
                Collumn = collumn;
                Row = row;
                Count = count;
            }
        }

        public string Name;

        public int Width => Headers.Count - 1;
        public int Height { get; set; }

        int CellRow = 0;

        public List<string> Headers = new List<string>();
        public List<Cell> Cells = new List<Cell>();

        List<RepeatCell> RepeatX = new List<RepeatCell>();
        List<RepeatCell> RepeatY = new List<RepeatCell>();

        public void AddCell(int CellCollumn, string value, bool hasRowspan)
        {
            int CellLength = 1;

            if (!hasRowspan)
            {
                foreach (var repeat in RepeatY)
                {
                    if (repeat.Row != CellRow)
                        continue;

                    if (Enumerable.Range(repeat.Collumn, repeat.Count).Contains(CellCollumn))
                        CellRow++;
                    else
                        continue;
                }

                foreach (var repeat in RepeatX)
                {
                    if (repeat.Row != CellRow)
                        continue;

                    if (Enumerable.Range(repeat.Collumn, repeat.Count).Contains(CellCollumn))
                        CellLength = repeat.Count;
                    else
                        continue;
                }
            }

            Cells.Add(new Cell(CellCollumn, CellRow, CellLength, value));


            CellRow += CellLength;
            if (CellRow > Width)
                CellRow = 0;
        }

        public void RepeatAtY(int collumn, int row, int count) { RepeatY.Add(new RepeatCell(collumn, row, count)); }
        public void RepeatAtX(int collumn, int row, int count) { RepeatX.Add(new RepeatCell(collumn, row, count)); }

        public string GetAt(int collumn, int row)
        {
            foreach (Cell cell in Cells)
                if (cell.Collumn == collumn && cell.Row == row)
                    return cell.Value;

            return null;
        }
    }
}
