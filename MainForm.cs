using Npgsql;
using System.Data;

namespace PR4
{
    public partial class MainForm : Form
    {
        public static NpgsqlConnection conn;
        NpgsqlDataAdapter adapter;
        DataTable dataTable;
        string currentTable;
        Dictionary<string, Dictionary<int, string>> lookupData = new Dictionary<string, Dictionary<int, string>>();

        Dictionary<string, Dictionary<string, string>> foreignKeysMap = new Dictionary<string, Dictionary<string, string>>
        {
            ["список_продажи"] = new Dictionary<string, string> { ["ин_товара"] = "товары" },
            ["продажи"] = new Dictionary<string, string> { ["ин_списка_проданных_товаров"] = "список_продажи" },
            ["поставки"] = new Dictionary<string, string> { ["ин_списка_проданных_товаров"] = "список_поставки" }
        };
        public MainForm()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximumSize = this.Size;
            conn = new NpgsqlConnection("Host=localhost;Port=5432;Database=YP_DB;Username=postgres;Password=1219;");
            conn.Open();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentTable = comboBox1.Text;
            LoadLookupTables();

            adapter = new NpgsqlDataAdapter($"select * from {currentTable};", conn);
            dataTable = new DataTable();
            adapter.Fill(dataTable);

            dataGridView1.Columns.Clear();
            dataGridView1.DataSource = dataTable;
            dataGridView1.Columns["ин"].Visible = false;

            ReplaceForeignKeyColumns();

            dataGridView1.DefaultValuesNeeded -= dataGridView1_DefaultValuesNeeded;
            dataGridView1.DefaultValuesNeeded += dataGridView1_DefaultValuesNeeded;
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            try
            {
                NpgsqlCommandBuilder npgsqlCommandBuilder = new NpgsqlCommandBuilder(adapter);
                adapter.Update(dataTable);
                MessageBox.Show("Изменения сохранены.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении:\n" + ex.Message);
            }
        }

        private void LoadLookupTables()
        {
            lookupData.Clear();

            if (!foreignKeysMap.ContainsKey(currentTable))
                return;

            foreach (var fk in foreignKeysMap[currentTable])
            {
                string lookupTable = fk.Value;
                if (!lookupData.ContainsKey(lookupTable))
                {
                    string keyColumn = "ин";
                    string valueColumn = GetDisplayColumnForTable(lookupTable);
                    lookupData[lookupTable] = LoadLookup(lookupTable, keyColumn, valueColumn);
                }
            }
        }

        private string GetDisplayColumnForTable(string table)
        {
            switch (table)
            {
                case "товары": return "наименование_товара";
                case "сотрудники": return "ФИО";
                case "список_продажи": return "номер_списка";
                case "список_поставки": return "номер_списка";
                default: return "ин";
            }
        }

        private Dictionary<int, string> LoadLookup(string table, string key, string column)
        {
            var dict = new Dictionary<int, string>();
            using (var cmd = new NpgsqlCommand($"select {key}, {column} from {table};", conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dict.Add(reader.GetInt32(0), reader.IsDBNull(1) ? "NULL" : reader.GetString(1));
                    }
                }
            }
            return dict;
        }

        private void ReplaceForeignKeyColumns()
        {
            if (!foreignKeysMap.TryGetValue(currentTable, out var fkColumns))
                return;

            foreach (var fk in fkColumns)
            {
                string columnName = fk.Key;
                string lookupTable = fk.Value;

                if (!dataTable.Columns.Contains(columnName))
                    continue;

                var combo = new DataGridViewComboBoxColumn
                {
                    Name = columnName,
                    DataPropertyName = columnName,
                    DataSource = lookupData[lookupTable].ToList(),
                    DisplayMember = "Value",
                    ValueMember = "Key",
                    FlatStyle = FlatStyle.Flat
                };

                int index = dataGridView1.Columns[columnName].Index;
                dataGridView1.Columns.Remove(columnName);
                dataGridView1.Columns.Insert(index, combo);
            }
        }

        private void dataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            if (!foreignKeysMap.TryGetValue(currentTable, out var fkColumns))
                return;

            foreach (var fk in fkColumns)
            {
                string columnName = fk.Key;
                string lookupTable = fk.Value;

                if (lookupData.TryGetValue(lookupTable, out var lookup) && lookup.Count > 0)
                {
                    e.Row.Cells[columnName].Value = lookup.First().Key;
                }
            }

            // Установка значений по умолчанию для разных таблиц
            switch (currentTable)
            {
                case "продажи":
                    if (dataTable.Columns.Contains("дата_продажи"))
                        e.Row.Cells["дата_продажи"].Value = DateTime.Now;
                    if (dataTable.Columns.Contains("количество"))
                        e.Row.Cells["количество"].Value = 1;
                    break;

                case "поставки":
                    if (dataTable.Columns.Contains("дата_поставки"))
                        e.Row.Cells["дата_поставки"].Value = DateTime.Now;
                    break;
            }
        }

        private void goods_Click(object sender, EventArgs e)
        {
            Form form = new GoodsForm();
            form.ShowDialog();
        }

        private void basket_Click(object sender, EventArgs e)
        {
            Form form = new BasketForm();
            form.ShowDialog();
        }
    }
}