﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using Simple.Data.Ado;
using Simple.Data.Ado.Schema;

namespace Simple.Data.SqlCe35
{
    public class SqlCeSchemaProvider : ISchemaProvider
    {
        private readonly IConnectionProvider _connectionProvider;

        public SqlCeSchemaProvider(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public IEnumerable<Table> GetTables()
        {
            foreach (var row in _GetTables().AsEnumerable())
            {
                yield return new Table(row["TABLE_NAME"].ToString(), null,
                    row["TABLE_TYPE"].ToString() == "BASE TABLE" ? TableType.Table : TableType.View);
            }
        }

        public IEnumerable<Column> GetColumns(Table table)
        {
            foreach (var row in _GetColumns(table).AsEnumerable())
            {
                yield return new Column(row["COLUMN_NAME"].ToString(), table);
            }
        }

        public Key GetPrimaryKey(Table table)
        {
            return new Key(GetPrimaryKeys(table.ActualName).AsEnumerable()
                .Where(row => row["TABLE_NAME"].ToString() == table.ActualName)
                    .OrderBy(row => (int)row["ORDINAL_POSITION"])
                    .Select(row => row["COLUMN_NAME"].ToString()));
        }

        public IEnumerable<ForeignKey> GetForeignKeys(Table table)
        {
            var groups = GetForeignKeys(table.ActualName).AsEnumerable()
                .Where(row => row["TABLE_NAME"].ToString() == table.ActualName)
                .GroupBy(row => row["CONSTRAINT_NAME"].ToString())
                .ToList();

            foreach (var group in groups)
            {
                yield return new ForeignKey(group.First()["TABLE_NAME"].ToString(),
                    group.Select(row => row["COLUMN_NAME"].ToString()),
                    group.First()["UNIQUE_TABLE_NAME"].ToString(),
                    group.Select(row => row["UNIQUE_COLUMN_NAME"].ToString()));
            }
        }

        private DataTable _GetTables()
        {
            return SelectToDataTable("SELECT TABLE_NAME, TABLE_TYPE FROM INFORMATION_SCHEMA.TABLES");
        }

        private DataTable _GetColumns(Table table)
        {
            return SelectToDataTable("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + table.ActualName + "'");
        }

        private DataTable GetPrimaryKeys()
        {
            return SelectToDataTable(Properties.Resources.PrimaryKeySql);
        }

        private DataTable GetForeignKeys()
        {
            return SelectToDataTable(Properties.Resources.ForeignKeysSql);
        }

        private DataTable GetPrimaryKeys(string tableName)
        {
            return GetPrimaryKeys().AsEnumerable()
                .Where(
                    row => row["TABLE_NAME"].ToString().Equals(tableName, StringComparison.InvariantCultureIgnoreCase))
                .CopyToDataTable();
        }

        private DataTable GetForeignKeys(string tableName)
        {
            return GetForeignKeys().AsEnumerable()
                .Where(
                    row => row["TABLE_NAME"].ToString().Equals(tableName, StringComparison.InvariantCultureIgnoreCase))
                .CopyToDataTable();
        }

        private DataTable SelectToDataTable(string sql)
        {
            var dataTable = new DataTable();
            using (var cn = _connectionProvider.CreateConnection() as SqlCeConnection)
            {
                using (var adapter = new SqlCeDataAdapter(sql, cn))
                {
                    adapter.Fill(dataTable);
                }

            }

            return dataTable;
        }
    }
}