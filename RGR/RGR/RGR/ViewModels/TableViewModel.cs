using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Data;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using RGR.Models;
using System.Reactive;

namespace RGR.ViewModels
{
    public class TableViewModel : ViewModelBase
    {
        string dbName = "DogRunners.db";
        private SQLiteConnection sql_con;
        private DataSet tables;

        List<RequestData> requests;
        int selectRow;

        public int currentTableIndex;

        public List<RequestData> Requests
        {
            get => requests;
            set => this.RaiseAndSetIfChanged(ref requests, value);
        }
        public int SelectRow
        {
            get => selectRow;
            private set => this.RaiseAndSetIfChanged(ref selectRow, value);
        }

        public DataSet Tables
        {
            get => tables;
            private set => this.RaiseAndSetIfChanged(ref tables, value);
        }

        public ReactiveCommand<Unit, DataSet> SendTables { get; }

        public TableViewModel()
        {

            SendTables = ReactiveCommand.Create(() => Tables);

            string sql = "SELECT name FROM sqlite_master WHERE type=\"table\" ORDER BY 1";
            string connectionStr = "Data Source=" + dbName + ";Mode=ReadWrite";

            using (sql_con = new SQLiteConnection(connectionStr))
            {
                sql_con.Open();
                SQLiteCommand command = new SQLiteCommand(sql, sql_con);
                DataTable tablesNames = new DataTable();
                using (SQLiteDataReader reader = command.ExecuteReader())
                {

                    tablesNames.Load(reader);
                    tables = new DataSet();
                    string subsql = "SELECT * FROM ";
                    foreach (DataRow row in tablesNames.Rows)
                    {
                        string name = row.ItemArray[0].ToString();
                        if (name == "sqlite_sequence") continue;
                        /*                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(subsql + name, sql_con);
                                                adapter.Fill(tables, name);*/
                        SQLiteCommand sqlTab = new SQLiteCommand(subsql + name, sql_con);
                        DataTable table = new DataTable();
                        table.Load(sqlTab.ExecuteReader());
                        table.TableName = name;
                        tables.Tables.Add(table);
                    }
                }
            }
        }

        public void AddRow()
        {
            DataRow row = tables.Tables[currentTableIndex].NewRow();
            int value = tables.Tables[currentTableIndex].Rows.Count + 1;
            row.BeginEdit();
            for (int i = 0; i < row.ItemArray.Length; i++)
            {
                if (Convert.ToString(row[i]) == "")
                    row[i] = Convert.ToString(value);
                
            }
            row.EndEdit();

            tables.Tables[currentTableIndex].Rows.Add(row);
        }

        public void DeleteRow()
        {
            tables.Tables[currentTableIndex].Rows[selectRow].Delete();
            /*tables.Tables[currentTableIndex].Rows.RemoveAt(selectRow);*/

        }

        public void OnClick()
        {
            string connectionStr = "Data Source=" + dbName + ";Mode=ReadWrite";
            string sql = "SELECT * FROM ";

            using (sql_con = new SQLiteConnection(connectionStr))
            {
                for (int i = 0; i < tables.Tables.Count; i++)
                {

                    try
                    {
                        SQLiteDataAdapter adapter = new SQLiteDataAdapter(sql + tables.Tables[i].TableName, sql_con);
                        SQLiteCommandBuilder commandBuilder = new SQLiteCommandBuilder(adapter);
                        commandBuilder.RefreshSchema();
                        int count = adapter.Update(tables, tables.Tables[i].TableName);
                        
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }

                }
                tables.AcceptChanges();
            }
        }

        public void Execute ()
        {
            string connectionStr = "Data Source=" + dbName + ";Mode=ReadWrite";
            using (sql_con = new SQLiteConnection(connectionStr))
            {
                sql_con.Open();
                SQLiteCommand command;
                for (int i = 0; i < Requests.Count; i++)
                {
                    command = new SQLiteCommand(Requests[i].Text, sql_con);
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                    adapter.FillSchema(Tables, SchemaType.Source);
                    adapter.Fill(tables, Requests[i].Name);
                }
                
            }
        }

        ~TableViewModel()
        {
            sql_con.Close();
        }

    }
}
