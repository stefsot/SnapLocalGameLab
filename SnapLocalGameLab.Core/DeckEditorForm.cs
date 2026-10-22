using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SnapLocalGameLab.Core
{
    internal sealed class DeckInput : UserControl
    {
        private readonly ComboBox _mode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        private readonly TextBox _code = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 72, Dock = DockStyle.Top };
        private readonly Label _summary = new Label { AutoSize = true, Dock = DockStyle.Top };
        public DeckEntry[] Entries { get; private set; } = Array.Empty<DeckEntry>();
        public string Code => _mode.SelectedIndex == 1 ? _code.Text : "";
        public DeckEntry[] ManualCards => _mode.SelectedIndex == 2 ? Entries : null;
        public string EditorCode => _mode.SelectedIndex == 1 ? _code.Text : "";

        public DeckInput(Func<DeckInput, Task> edit)
        {
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            var layout = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1 };
            var bar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Margin = Padding.Empty };
            _mode.Items.AddRange(new object[] { "Game default", "Deck code", "Manual selection" });
            var advanced = new Button { Text = "Advanced…", AutoSize = true };
            advanced.Click += async (sender, args) => await edit(this);
            bar.Controls.Add(_mode);
            bar.Controls.Add(advanced);
            layout.Controls.Add(bar);
            layout.Controls.Add(_code);
            layout.Controls.Add(_summary);
            Controls.Add(layout);
            _mode.SelectedIndexChanged += (sender, args) => RefreshMode();
            _mode.SelectedIndex = 0;
        }

        public void SetCards(DeckEntry[] entries)
        {
            Entries = entries;
            _mode.SelectedIndex = 2;
            RefreshMode();
        }

        private void RefreshMode()
        {
            _code.Visible = _mode.SelectedIndex == 1;
            _summary.Visible = _mode.SelectedIndex == 2;
            _summary.Text = Entries.Length == 0 ? "Click Advanced to choose cards and counts."
                : Entries.Sum(c => c.Count) + " cards, " + Entries.Length + " selections";
        }
    }

    internal sealed class DeckEditorForm : Form
    {
        private readonly DataGridView _cards = new DataGridView
        {
            Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, RowHeadersVisible = false, MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect, EditMode = DataGridViewEditMode.EditOnEnter,
            BackgroundColor = SystemColors.Window,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        };
        private readonly TextBox _search = new TextBox { Dock = DockStyle.Fill };
        private readonly CheckBox _selectedOnly = new CheckBox { Text = "Selected only", AutoSize = true };
        private readonly Label _total = new Label { AutoSize = true, Margin = new Padding(8) };
        public DeckEntry[] Selection { get; private set; }

        public DeckEditorForm(GameCatalog catalog, DeckEntry[] initial)
        {
            Text = "Manual deck — cards from the game client";
            ClientSize = new Size(740, 560);
            MinimumSize = new Size(580, 400);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var filter = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
            filter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filter.Controls.Add(new Label { Text = "Search", AutoSize = true, Margin = new Padding(3, 6, 6, 6) });
            filter.Controls.Add(_search);
            filter.Controls.Add(_selectedOnly);
            layout.Controls.Add(filter, 0, 0);
            layout.Controls.Add(_cards, 0, 1);
            var footer = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            var save = new Button { Text = "Use this deck", AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true, CausesValidation = false };
            footer.Controls.Add(save);
            footer.Controls.Add(cancel);
            footer.Controls.Add(_total);
            layout.Controls.Add(footer, 0, 2);
            Controls.Add(layout);
            CancelButton = cancel;
            _cards.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Use", HeaderText = "Use", Width = 45 });
            _cards.Columns.Add(new DataGridViewTextBoxColumn { Name = "Card", HeaderText = "Card", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _cards.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "Card ID", ReadOnly = true, Width = 220 });
            _cards.Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "Count", ReadOnly = false, Width = 65, ValueType = typeof(int) });
            var selected = initial.ToDictionary(c => c.CardId, c => c.Count, StringComparer.OrdinalIgnoreCase);
            var available = catalog.Cards.Select(c => c.Id).ToArray();
            var missing = selected.Keys.Where(id => !available.Contains(id, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (missing.Length != 0) throw new ArgumentException("Cards missing from this client: " + string.Join(", ", missing));
            foreach (var card in catalog.Cards)
            {
                var chosen = selected.TryGetValue(card.Id, out var count);
                _cards.Rows.Add(chosen, card.Name, card.Id, chosen ? count : 1);
            }
            _cards.CurrentCellDirtyStateChanged += (sender, args) =>
            {
                if (_cards.IsCurrentCellDirty && _cards.CurrentCell is DataGridViewCheckBoxCell)
                    _cards.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _cards.CellValueChanged += (sender, args) =>
            {
                if (args.RowIndex < 0) return;
                UpdateTotal();
            };
            _cards.CellValidating += (sender, args) =>
            {
                if (args.ColumnIndex != 3) return;
                if (!int.TryParse(Convert.ToString(args.FormattedValue), out var count) || count < 1 || count > 99)
                {
                    args.Cancel = true;
                    _total.Text = "Count must be 1–99.";
                }
            };
            _cards.DataError += (sender, args) => { args.ThrowException = false; _total.Text = "Enter a whole number for the count."; };
            _search.TextChanged += (sender, args) => Filter();
            _selectedOnly.CheckedChanged += (sender, args) => Filter();
            save.Click += (sender, args) =>
            {
                if (!_cards.EndEdit() || !ValidateChildren()) return;
                var entries = ReadSelection();
                try { LocalGameOptions.ValidateDeck(entries); }
                catch (ArgumentException exception) { _total.Text = exception.Message; return; }
                Selection = entries;
                DialogResult = DialogResult.OK;
            };
            UpdateTotal();
        }

        private DeckEntry[] ReadSelection() => _cards.Rows.Cast<DataGridViewRow>()
            .Where(row => Convert.ToBoolean(row.Cells[0].Value))
            .Select(row => new DeckEntry { CardId = (string)row.Cells[2].Value, Count = Convert.ToInt32(row.Cells[3].Value) }).ToArray();

        private void UpdateTotal() => _total.Text = ReadSelection().Sum(c => c.Count) + " cards selected (normally 12; maximum 99).";

        private void Filter()
        {
            if (!_cards.EndEdit()) return;
            _cards.CurrentCell = null;
            var query = _search.Text.Trim();
            foreach (DataGridViewRow row in _cards.Rows)
                row.Visible = (!_selectedOnly.Checked || Convert.ToBoolean(row.Cells[0].Value))
                    && (Convert.ToString(row.Cells[1].Value).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                        || Convert.ToString(row.Cells[2].Value).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
