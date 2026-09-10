using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SnapLocalGameLab.Core
{
    internal sealed class LabForm : Form
    {
        private readonly TextBox _player = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 95 };
        private readonly TextBox _aiDeck = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 95 };
        private readonly TextBox _locations = new TextBox();
        private readonly TextBox _seed = new TextBox();
        private readonly ComboBox _league = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _strength = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly CheckBox _skip = new CheckBox { Text = "Skip new-card prompts", Checked = true, AutoSize = true };
        private readonly TextBox _log = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Height = 130 };
        private readonly FlowLayoutPanel _buttons = new FlowLayoutPanel { AutoSize = true };

        public LabForm()
        {
            Text = "Snap Local Game Lab";
            ClientSize = new Size(710, 720);
            MinimumSize = new Size(650, 650);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, ColumnCount = 2, Padding = new Padding(12) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(layout);
            _league.Items.AddRange(new object[] { "VsAi", "VsBalanceAi", "AutoDoubleVsAI", "BattleVsAi", "VsAiBestLose", "VsAiWorstWin" });
            _league.SelectedItem = "VsBalanceAi";
            _strength.Items.AddRange(new object[] { "HiddenBeginner", "HiddenIntermediate", "HiddenExpert", "Random", "DoNothing" });
            _strength.SelectedItem = "HiddenExpert";
            AddRow(layout, "Local league", _league);
            AddRow(layout, "Player deck code", _player);
            AddRow(layout, "AI deck code", _aiDeck);
            AddRow(layout, "Locations (optional)", _locations);
            AddRow(layout, "Seed (blank = random)", _seed);
            AddRow(layout, "", _skip);
            AddRow(layout, "AI preset (not applied)", _strength);
            AddRow(layout, "", new Label { Text = "Blank decks use game defaults. Blank locations = random; otherwise enter three comma-separated internal IDs.\r\nStart from the home screen.", AutoSize = true });
            AddButton("Start local game", StartGame);
            AddButton("Draw card", () => Run(() => LocalGameControls.Send("DrawCardCheatRequest")));
            AddButton("+2 energy", () => Run(() => LocalGameControls.Send("AddEnergyCheatRequest", amount: 2)));
            AddRow(layout, "", _buttons);
            AddRow(layout, "Status", _log);
        }

        private static void AddRow(TableLayoutPanel layout, string label, Control control)
        {
            var row = layout.RowCount++;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 7, 3, 7) }, 0, row);
            control.Dock = DockStyle.Top;
            control.Margin = new Padding(3, 5, 3, 5);
            layout.Controls.Add(control, 1, row);
        }

        private void AddButton(string text, Func<Task> action)
        {
            var button = new Button { Text = text, AutoSize = true };
            button.Click += async (sender, args) =>
            {
                _buttons.Enabled = false;
                try { await action(); }
                catch (Exception e) { Log(e.ToString()); }
                finally { if (!IsDisposed) _buttons.Enabled = true; }
            };
            _buttons.Controls.Add(button);
        }

        private Task StartGame()
        {
            int? seed = null;
            if (!string.IsNullOrWhiteSpace(_seed.Text))
            {
                if (!int.TryParse(_seed.Text, out var number)) throw new ArgumentException("Seed must be a signed 32-bit integer.");
                seed = number;
            }
            var options = new LocalGameOptions
            {
                League = (string)_league.SelectedItem, PlayerDeck = _player.Text, AiDeck = _aiDeck.Text,
                Locations = _locations.Text, Seed = seed, SkipNewCardPrompts = _skip.Checked
            };
            return Run(() => LocalGameService.Start(options));
        }

        private async Task Run(Func<string> action) => Log(await GameThread.Run(action));
        private void Log(string message)
        {
            if (!IsDisposed) _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + message + Environment.NewLine);
        }
    }
}
