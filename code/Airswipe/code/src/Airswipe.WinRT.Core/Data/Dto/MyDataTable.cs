using System;
using System.Collections.Generic;
using System.Linq;

namespace Airswipe.WinRT.Core.Data.Dto
{
    public class MyDataTable
    {
        Dictionary<string, List<object>> dict = new Dictionary<string, List<object>>();

        public static readonly object NULL_VALUE = "0.0";

        int rowCount = 0;

        public IEnumerable<string> ColNames
        {
            get { return dict.Keys; }//table.Columns.Cast<DataColumn>();
        }

        public int GetColIndex(string colName)
        {
            int index = dict.Keys.ToList().IndexOf(colName);

            if (index < 0)
                throw new Exception("Col does not exist");

            return index;
        }

        public MyDataTable TrimByRowsColValue(string colName, object trimValue)
        {
            MyDataTable clone = Clone();

            for (int i = rowCount - 1; i >= 0; i--)
                if (!clone[colName][i].Equals(trimValue))
                {
                    clone.rowCount--;

                    foreach (var values in clone.dict.Values)
                    {
                        values.RemoveAt(i);
                    }
                }

            return clone;
        }

        public MyDataTable DuplicateRows(
            Dictionary<string, Func<object, bool>> colsFilter = null,
            Dictionary<string, Func<object, object>> colValueManipulate = null,
            Dictionary<string, string> colNameValueMapping = null
            )
        {
            return DuplicateRows(
                r =>
                {
                    if (colsFilter != null)
                        foreach (string key in colsFilter.Keys)
                        {
                            int colIndex = GetColIndex(key);
                            var colFilter = colsFilter[key];
                            bool passesFilter = colFilter(r[colIndex]);
                            if (!passesFilter)
                                return null;
                        }

                    if (colValueManipulate != null)
                        foreach (string key in colValueManipulate.Keys)
                        {
                            int index = GetColIndex(key);
                            var manipulator = colValueManipulate[key];
                            r[index] = manipulator(r[index]);
                        }

                    if (colNameValueMapping != null)
                        foreach (string key in colNameValueMapping.Keys)
                        {
                            int index = GetColIndex(key);
                            int swapIndex = GetColIndex(colNameValueMapping[key]);
                            r[index] = r[swapIndex];
                        }


                    return r;
                }
                );
        }

        //// overrides from another column
        //public MyDataTable DuplicateRows()
        //{
        //    return DuplicateRows(
        //        r =>
        //        {

        //            foreach (string key in colNameValueMapping.Keys)
        //            {
        //                int index = GetColIndex(key);
        //                int swapIndex = GetColIndex(colNameValueMapping[key]);
        //                r[index] = r[swapIndex];
        //            }

        //            return r;
        //        }
        //        );
        //}

        public MyDataTable DuplicateRows(Func<List<object>, List<object>> f)
        {
            var clone = Clone();

            for (int i = 0; i < rowCount; i++)
            {
                var newRow = f(GetRowAt(i));
                bool skip = (newRow == null);
                if (!skip)
                    clone.AddRow(newRow);
                //for (int ii = 0; ii < rowCount; ii++)
                //    clone[ii].Add(newRow[ii]);
            }

            return clone;
        }

        private IEnumerable<List<object>> Filter(Func<List<object>, List<object>> f)
        {
            for (int i = 0; i < rowCount; i++)
            {
                var row = f(GetRowAt(i));
                bool skip = (row == null);
                if (!skip)
                    yield return row;
                //for (int ii = 0; ii < rowCount; ii++)
                //    clone[ii].Add(newRow[ii]);
            }
        }

        //public MyDataTable Filter(Dictionary<string, object> colsValueFilter = null)
        //{
        //    var d = new Dictionary<string, List<object>>;
        //    fore

        //    return Filter()
        //}

        public MyDataTable Filter(Dictionary<string, List<string>> colsValueFilter = null)
        {
            var clone = Clone(false);

            var rows = Filter(r =>
            {
                if (colsValueFilter != null)
                    foreach (string key in colsValueFilter.Keys)
                    {
                        int colIndex = GetColIndex(key);
                        var allowedValues = colsValueFilter[key];
                        bool passesFilter = false;
                        foreach (var value in allowedValues)
                        {
                            string colValue = r[colIndex].ToString();
                            if (colValue.Equals(value))
                                passesFilter = true;
                        }
                        if (!passesFilter)
                            return null;
                    }

                return r;
            }
            );

            foreach (var row in rows)
                clone.AddRow(row);

            return clone;

        }

        //public MyDataTable GetSubset(Func<List<object>, List<object>> f)
        //{
        //    var clone = Clone();

        //    for (int i = 0; i < rowCount; i++)
        //    {
        //        var newRow = f(GetRowAt(i));
        //        bool skip = (newRow == null);
        //        if (!skip)
        //            clone.AddRow(newRow);
        //        //for (int ii = 0; ii < rowCount; ii++)
        //        //    clone[ii].Add(newRow[ii]);
        //    }

        //    return clone;
        //}


        private void AddRow(List<object> values)
        {
            for (int i = 0; i < values.Count; i++)
                this[i].Add(values[i]);

            rowCount++;
        }

        private MyDataTable Clone(bool isDataIncluded = true)
        {
            if (!isDataIncluded)
            {
                var noDataTable = new MyDataTable();

                foreach (var key in dict.Keys)
                    noDataTable[key] = new List<object>();

                noDataTable.SortIndex = SortIndex;

                return noDataTable;
            }

            var clone = new MyDataTable() { dict = new Dictionary<string, List<object>>(dict) };
            clone.rowCount = rowCount;
            clone.SortIndex = SortIndex;

            return clone;
        }

        public List<object> this[string name]
        {
            get { return dict[name]; }
            set { dict[name] = value; }
        }

        public List<object> this[int i]
        {
            get { return dict.Values.ElementAt(i); }
            //set { dict[name] = value; }
        }

        public int SortIndex { get; set; }

        public int AddRow()
        {
            foreach (string colName in ColNames)
                dict[colName].Add(NULL_VALUE);

            rowCount++;
            return rowCount;
        }

        public void CreateColIfNotExists(string colName)
        {
            CreateColIfNotExists(colName, NULL_VALUE);
        }

        public void CreateColIfNotExists(string colName, object defaultValue)
        {
            if (!dict.ContainsKey(colName))
                dict[colName] = Enumerable.Repeat(NULL_VALUE, rowCount).Cast<object>().ToList();
        }

        public void ReplaceColValues(string colName, object valueOut, object valueIn)
        {
            dict[colName] = dict[colName].Select(v => (v.Equals(valueOut)) ? valueIn : valueOut).ToList();
        }

        public void SetCurrentRowCol(string colName, object value, bool createColIfNotExists = false)
        {
            if (!dict.ContainsKey(colName))
                if (createColIfNotExists)
                    dict[colName] = Enumerable.Repeat(NULL_VALUE, rowCount).Cast<object>().ToList();
                else
                    throw new Exception("Column does not exist.");

            var list = dict[colName];
            var index = list.Count - 1;
            list[list.Count - 1] = value;
        }

        public IList<IList<object>> PopAllSortByIndex()
        {
            var rows = new List<IList<object>>();
            IList<object> row = PopFirst();
            while (row != null)
            {
                rows.Add(row);
                row = PopFirst();
            }

            return rows.Select(
                r => new { Index = r[SortIndex], Row = r }
                ).OrderBy(
                    ir => ir.Index
                    ).Select(oir => oir.Row).ToList();
        }

        public IList<object> PopFirst()
        {
            if (rowCount == 0)
                return null;
            int popIndex = 0;

            var row = GetRowAt(popIndex);

            RemoveRowAt(popIndex);

            return row;
        }

        private void RemoveRowAt(int popIndex)
        {
            foreach (string colName in ColNames)
                dict[colName].RemoveAt(popIndex);

            rowCount--;
        }

        private List<object> GetRowAt(int popIndex)
        {
            var result = new List<object>();
            foreach (string colName in ColNames)
            {
                var l = dict[colName];
                result.Add(l[popIndex]);
            }

            return result;
        }
    }
}
