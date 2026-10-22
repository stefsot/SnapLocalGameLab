using System;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SnapLocalGameLab.Core
{
    internal sealed class LabForm : Form
    {
        private readonly TabControl _tabs = new TabControl { Dock = DockStyle.Fill };
        private readonly FlowLayoutPanel _buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        private readonly TextBox _log = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
        private readonly Label _catalogStatus = Note("Loading the client catalog…");
        private readonly ComboBox _league = Choice("VsAi", "VsBalanceAi", "AutoDoubleVsAI", "BattleVsAi", "VsAiBestLose", "VsAiWorstWin");
        private readonly ComboBox _aiPreset = Choice("League default", "DoNothing", "Random", "RandomAfterOpponent",
            "HiddenBeginner", "HiddenIntermediate", "HiddenExpert", "HiddenWorstWin", "HiddenBestLose");
        private readonly DeckInput _player;
        private readonly DeckInput _aiDeck;
        private readonly CheckBox _overrideLocations = Check("Override starting locations");
        private readonly ComboBox[] _locations = { Picker(), Picker(), Picker() };
        private readonly ComboBox _seedMode = Choice("Random", "Integer seed (legacy)", "16-byte key");
        private readonly TextBox _seed = new TextBox();
        private readonly Label _seedHint = Note("");
        private readonly CheckBox _skip = Check("Skip new-card prompts", true);
        private readonly CheckBox _snapping = Check("Enable snapping", true);
        private readonly NumericUpDown _startingCubes = Number(1, 1000000, 1);
        private readonly NumericUpDown _maximumCubes = Number(1, 1000000, 8);
        private readonly NumericUpDown _snapsPerPlayer = Number(1, 4, 1);
        private readonly CheckBox _doubleFinalTurn = Check("Double stakes on the final turn", true);
        private readonly NumericUpDown _speed = Number(0, 5, 1, 2, 0.25m);
        private readonly NumericUpDown _hold = Number(0.05m, 30, 0.3m, 2, 0.05m);
        private readonly ComboBox _drawTarget = Choice("Me", "Opponent", "Both");
        private readonly ComboBox _energyTarget = Choice("Me", "Opponent", "Both");
        private readonly NumericUpDown _energy = Number(-99, 99, 2);
        private readonly ComboBox _drawCard = Picker();
        private readonly ComboBox _spawnTarget = Choice("Me", "Opponent", "Both");
        private readonly ComboBox _spawnCard = Picker();
        private readonly ComboBox _spawnDestination = Choice("Hand", "Deck", "Location");
        private readonly ComboBox _spawnLocation = Choice("Left (1)", "Middle (2)", "Right (3)");
        private readonly ComboBox _cleanupTarget = Choice("Both", "Me", "Opponent");
        private readonly ComboBox _matchCard = Picker();
        private readonly ComboBox _augment = Picker();
        private readonly CheckBox _playerAppearance = Check("Override my appearance");
        private readonly CheckBox _opponentAppearance = Check("Override opponent appearance");
        private readonly TextBox _playerName = new TextBox { MaxLength = 64 };
        private readonly TextBox _opponentName = new TextBox { MaxLength = 64 };
        private readonly ComboBox _playerAvatar = Picker();
        private readonly ComboBox _opponentAvatar = Picker();
        private readonly Timer _messages = new Timer { Interval = 1000 };
        private GameCatalog _catalog;
        private bool _busy;
        private bool _closing;
        private bool _allowClose;

        public LabForm()
        {
            Text = "Snap Local Game Lab";
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9);
            ClientSize = new Size(800, 790);
            MinimumSize = new Size(710, 600);
            _player = new DeckInput(input => Busy(() => EditDeck(input)));
            _aiDeck = new DeckInput(input => Busy(() => EditDeck(input)));
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8) };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            root.Controls.Add(_tabs, 0, 0);
            root.Controls.Add(_buttons, 0, 1);
            root.Controls.Add(_log, 0, 2);
            Controls.Add(root);
            BuildSetup();
            BuildMatch();
            BuildCards();
            BuildPresentation();
            Button(_buttons, "Start local game", StartGame);
            Button(_buttons, "Return to main screen", () => Run(LocalGameService.ReturnToMainScreen));
            Button(_buttons, "Reload client lists", LoadCatalog);
            Shown += async (sender, args) => await Busy(LoadCatalog);
            _messages.Tick += (sender, args) =>
            {
                while (LocalGameInitialization.TryGetMessage(out var message)) Log(message);
            };
            _messages.Start();
        }

        private void BuildSetup()
        {
            var layout = Page("Setup");
            var match = Section(layout, "Local match");
            _league.SelectedItem = "VsBalanceAi";
            Row(match, "Local league", _league);
            Row(match, "AI preset", _aiPreset);
            Row(match, "", _skip);
            var decks = Section(layout, "Decks");
            Row(decks, "My deck", _player);
            Row(decks, "Opponent deck", _aiDeck);
            var locationSection = Section(layout, "Starting locations");
            Row(locationSection, "", _overrideLocations);
            var locations = Table();
            for (var i = 0; i < _locations.Length; i++) Row(locations, "Location " + (i + 1), _locations[i]);
            Row(locationSection, "", locations);
            Toggle(_overrideLocations, locations);
            var randomness = Section(layout, "Randomness");
            Row(randomness, "Seed mode", _seedMode);
            var seedPanel = Table();
            Row(seedPanel, "Seed / key", _seed);
            Row(seedPanel, "", _seedHint);
            var generate = new FlowLayoutPanel { AutoSize = true };
            Button(generate, "Generate key", () =>
            {
                var key = new byte[16];
                using (var random = RandomNumberGenerator.Create()) random.GetBytes(key);
                _seed.Text = BitConverter.ToString(key).Replace("-", "");
                return Task.CompletedTask;
            });
            Row(seedPanel, "", generate);
            Row(randomness, "", seedPanel);
            _seedMode.SelectedIndexChanged += (sender, args) =>
            {
                seedPanel.Visible = _seedMode.SelectedIndex != 0;
                generate.Visible = _seedMode.SelectedIndex == 2;
                _seedHint.Text = _seedMode.SelectedIndex == 1 ? "Signed 32-bit integer; uses the client's legacy generator."
                    : "32 hexadecimal digits (16 bytes). Keep the same decks, locations and key to repeat a setup.";
                _seed.Clear();
            };
            seedPanel.Visible = false;
            var snapSection = Section(layout, "Snapping");
            var advanced = Check("Advanced snap rules");
            Row(snapSection, "", advanced);
            var rules = Table();
            Row(rules, "", _snapping);
            Row(rules, "Starting cubes", _startingCubes);
            Row(rules, "Maximum cubes", _maximumCubes);
            Row(rules, "Snaps per player", _snapsPerPlayer);
            Row(rules, "", _doubleFinalTurn);
            _snapping.CheckedChanged += (sender, args) => _snapsPerPlayer.Enabled = _snapping.Checked;
            Row(snapSection, "", rules);
            Toggle(advanced, rules);
            Row(layout, "Client catalog", _catalogStatus);
        }

        private void BuildMatch()
        {
            var layout = Page("Match controls");
            Row(layout, "", Note("Start a local game and wait for the board before using these controls."));
            var timingSection = Section(layout, "Speed and input");
            Row(timingSection, "Game speed (×)", _speed);
            Row(timingSection, "Tap-and-hold (seconds)", _hold);
            var timing = new FlowLayoutPanel { AutoSize = true };
            Button(timing, "Apply timing", () =>
            {
                var speed = (float)_speed.Value;
                var hold = (float)_hold.Value;
                return Run(() => LocalGameControls.SetTiming(speed, hold));
            });
            Button(timing, "Restore timing", () => Run(LocalGameControls.RestoreTiming));
            Row(timingSection, "", timing);
            Row(timingSection, "", Note("0× freezes Unity animations. Raise the hold time at higher speeds. Original timing is restored when returning home or closing the lab."));
            var timerSection = Section(layout, "Turn timer");
            var timer = new FlowLayoutPanel { AutoSize = true };
            Button(timer, "Pause this turn's timer", () => Run(LocalGameControls.PauseTimer));
            Row(timerSection, "", timer);
            var energySection = Section(layout, "Energy");
            Row(energySection, "Player", _energyTarget);
            Row(energySection, "Amount", _energy);
            var energy = new FlowLayoutPanel { AutoSize = true };
            Button(energy, "Add energy", () =>
            {
                var target = Target(_energyTarget);
                var amount = (int)_energy.Value;
                return Run(() => LocalGameControls.AddEnergy(target, amount));
            });
            Row(energySection, "", energy);
            var cleanupSection = Section(layout, "Clear cards");
            Row(cleanupSection, "Player", _cleanupTarget);
            var cleanup = new FlowLayoutPanel { AutoSize = true };
            Button(cleanup, "Destroy board cards", () => Cleanup(CleanupTarget.Board));
            Button(cleanup, "Discard hands", () => Cleanup(CleanupTarget.Hands));
            Button(cleanup, "Remove deck cards", () => Cleanup(CleanupTarget.Decks));
            Row(cleanupSection, "", cleanup);
            var slateSection = Section(layout, "Clean slate — both players");
            Row(slateSection, "", Note("Clears both hands, decks and boards, then changes all three locations to Ruins."));
            var slate = new FlowLayoutPanel { AutoSize = true };
            Button(slate, "Clean slate", () => Run(() =>
                LocalGameControls.Cleanup(CleanupTarget.Everything, PlayerTarget.Both)));
            Row(slateSection, "", slate);
        }

        private void BuildCards()
        {
            var layout = Page("Card actions");
            Row(layout, "", Note("These actions apply to the current local match. Card and augment lists come from the running client."));
            var drawSection = Section(layout, "Draw from deck");
            Row(drawSection, "Player", _drawTarget);
            Row(drawSection, "Specific card", _drawCard);
            var draw = new FlowLayoutPanel { AutoSize = true };
            Button(draw, "Draw next card", () =>
            {
                var target = Target(_drawTarget);
                return Run(() => LocalGameControls.Draw(target));
            });
            Button(draw, "Draw selected card", () =>
            {
                var target = Target(_drawTarget);
                var card = Selected<CatalogItem>(_drawCard, "a card");
                return Run(() => LocalGameControls.Draw(target, card.Id));
            });
            Row(drawSection, "", draw);
            Row(drawSection, "", Note("Draw selected pulls that card from the player's remaining deck. Draw next doesn't need a card selection."));
            var spawnSection = Section(layout, "Spawn a card");
            Row(spawnSection, "Player", _spawnTarget);
            Row(spawnSection, "Card", _spawnCard);
            Row(spawnSection, "Destination", _spawnDestination);
            var location = Table();
            Row(location, "Location", _spawnLocation);
            Row(spawnSection, "", location);
            location.Visible = false;
            _spawnDestination.SelectedIndexChanged += (sender, args) => location.Visible = (string)_spawnDestination.SelectedItem == "Location";
            var spawn = new FlowLayoutPanel { AutoSize = true };
            Button(spawn, "Spawn card", () =>
            {
                var target = Target(_spawnTarget);
                var card = Selected<CatalogItem>(_spawnCard, "a card to spawn");
                var destination = (SpawnDestination)Enum.Parse(typeof(SpawnDestination), (string)_spawnDestination.SelectedItem);
                var index = _spawnLocation.SelectedIndex;
                return Run(() => LocalGameControls.Spawn(target, card.Id, destination, index));
            });
            Row(spawnSection, "", spawn);
            Row(spawnSection, "", Note("Creates one new copy for each selected player in their hand, deck, or side of a location."));
            var augmentPanel = Section(layout, "Apply an augment");
            var refresh = new FlowLayoutPanel { AutoSize = true };
            Button(refresh, "Refresh match cards", RefreshMatchCards);
            Row(augmentPanel, "", refresh);
            Row(augmentPanel, "Card in this match", _matchCard);
            Row(augmentPanel, "Augment", _augment);
            var apply = new FlowLayoutPanel { AutoSize = true };
            Button(apply, "Apply augment", async () =>
            {
                var card = Selected<MatchCard>(_matchCard, "a match card");
                var augment = Selected<CatalogItem>(_augment, "an augment");
                Log(await LocalGameControls.ApplyAugment(card, augment.Id));
            });
            Row(augmentPanel, "", apply);
            Row(augmentPanel, "", Note("Refresh match cards first, then choose a card and augment. The match's augment limits apply."));
        }

        private void BuildPresentation()
        {
            var layout = Page("Presentation");
            Row(layout, "", Note("Use these settings for the next local game, or apply them to the current board. Leave a name blank or choose Game default to keep the current appearance."));
            var playerSection = Section(layout, "My appearance");
            Row(playerSection, "", _playerAppearance);
            var player = Table();
            Row(player, "Name", _playerName);
            Row(player, "Avatar", _playerAvatar);
            Row(playerSection, "", player);
            Toggle(_playerAppearance, player);
            var opponentSection = Section(layout, "Opponent appearance");
            Row(opponentSection, "", _opponentAppearance);
            var opponent = Table();
            Row(opponent, "Name", _opponentName);
            Row(opponent, "Avatar", _opponentAvatar);
            Row(opponentSection, "", opponent);
            Toggle(_opponentAppearance, opponent);
            var apply = new FlowLayoutPanel { AutoSize = true };
            Button(apply, "Apply to current board", () =>
            {
                var options = ReadPresentation();
                return Run(() => LocalGameControls.SetPresentation(options));
            });
            Row(layout, "", apply);
        }

        private async Task LoadCatalog()
        {
            _catalogStatus.Text = "Loading the client catalog…";
            try
            {
                _catalog = await GameThread.Run(GameCatalog.Load);
                if (IsDisposed) return;
                foreach (var location in _locations) Fill(location, _catalog.Locations);
                Fill(_drawCard, _catalog.Cards);
                Fill(_spawnCard, _catalog.Cards);
                Fill(_augment, _catalog.Augments);
                Fill(_playerAvatar, new object[] { "Game default" }.Concat(_catalog.Avatars).ToArray());
                Fill(_opponentAvatar, new object[] { "Game default" }.Concat(_catalog.Avatars).ToArray());
                if (_playerAvatar.SelectedIndex < 0) _playerAvatar.SelectedIndex = 0;
                if (_opponentAvatar.SelectedIndex < 0) _opponentAvatar.SelectedIndex = 0;
                _catalogStatus.Text = $"{_catalog.Cards.Length} cards, {_catalog.Locations.Length} locations, {_catalog.Augments.Length} augments.";
                Log("Loaded card, location, augment and avatar lists from the running client.");
            }
            catch
            {
                _catalogStatus.Text = "Catalog unavailable. Wait for the home screen, then click Reload client lists.";
                throw;
            }
        }

        private async Task EditDeck(DeckInput input)
        {
            if (_catalog == null) await LoadCatalog();
            var entries = input.Entries;
            if (!string.IsNullOrWhiteSpace(input.EditorCode))
            {
                var code = input.EditorCode;
                entries = await GameThread.Run(() => LocalGameService.ReadDeck(code));
            }
            if (_closing || IsDisposed) return;
            using (var editor = new DeckEditorForm(_catalog, entries))
                if (editor.ShowDialog(this) == DialogResult.OK) input.SetCards(editor.Selection);
        }

        private async Task RefreshMatchCards()
        {
            var cards = await GameThread.Run(LocalGameControls.ReadCards);
            if (_closing || IsDisposed) return;
            Fill(_matchCard, cards);
            Log("Loaded " + cards.Length + " cards from the current board, hands and decks.");
        }

        private Task StartGame()
        {
            int? seed = null;
            byte[] key = null;
            if (_seedMode.SelectedIndex == 1)
            {
                if (!int.TryParse(_seed.Text, out var value)) throw new ArgumentException("Enter a signed 32-bit integer seed.");
                seed = value;
            }
            if (_seedMode.SelectedIndex == 2) key = LocalGameOptions.ParseRandomKey(_seed.Text);
            var options = new LocalGameOptions
            {
                League = (string)_league.SelectedItem, PlayerDeck = _player.Code, AiDeck = _aiDeck.Code,
                AiPreset = _aiPreset.SelectedIndex == 0 ? null : (string)_aiPreset.SelectedItem,
                PlayerCards = _player.ManualCards, AiCards = _aiDeck.ManualCards,
                Locations = _overrideLocations.Checked ? _locations.Select(c => Selected<CatalogItem>(c, "all three locations").Id).ToArray() : null,
                Seed = seed, RandomKey = key, SkipNewCardPrompts = _skip.Checked,
                EnableSnapping = _snapping.Checked, StartingCubes = (int)_startingCubes.Value,
                MaximumCubes = (int)_maximumCubes.Value, SnapsPerPlayer = (int)_snapsPerPlayer.Value,
                DoubleFinalTurn = _doubleFinalTurn.Checked,
                Presentation = ReadPresentation()
            };
            return Run(() => LocalGameService.Start(options));
        }

        private PresentationOptions ReadPresentation() => new PresentationOptions
        {
            PlayerName = _playerAppearance.Checked ? _playerName.Text : null,
            OpponentName = _opponentAppearance.Checked ? _opponentName.Text : null,
            PlayerAvatar = _playerAppearance.Checked ? Avatar(_playerAvatar) : null,
            OpponentAvatar = _opponentAppearance.Checked ? Avatar(_opponentAvatar) : null
        };

        private Task Cleanup(CleanupTarget cleanup)
        {
            var target = Target(_cleanupTarget);
            return Run(() => LocalGameControls.Cleanup(cleanup, target));
        }

        private static PlayerTarget Target(ComboBox combo) => (PlayerTarget)Enum.Parse(typeof(PlayerTarget), (string)combo.SelectedItem);

        private static AvatarChoice Avatar(ComboBox combo) => combo.Text == "Game default" || string.IsNullOrWhiteSpace(combo.Text)
            ? null : Selected<AvatarChoice>(combo, "an avatar");

        private static T Selected<T>(ComboBox combo, string description) where T : class
        {
            var selected = combo.Items.Cast<object>().OfType<T>().FirstOrDefault(item =>
                string.Equals(item.ToString(), combo.Text, StringComparison.OrdinalIgnoreCase)
                || item is CatalogItem entry && string.Equals(entry.Id, combo.Text, StringComparison.OrdinalIgnoreCase));
            if (selected == null) throw new ArgumentException("Choose " + description + " from the list.");
            combo.SelectedItem = selected;
            return selected;
        }

        private static void Fill(ComboBox combo, object[] items)
        {
            var previous = combo.SelectedItem?.ToString();
            combo.BeginUpdate();
            try
            {
                combo.Items.Clear();
                combo.Items.AddRange(items);
                combo.SelectedItem = items.FirstOrDefault(item => item.ToString() == previous);
                if (combo.SelectedIndex < 0) combo.Text = "";
            }
            finally { combo.EndUpdate(); }
        }

        private TableLayoutPanel Page(string title)
        {
            var page = new TabPage(title) { AutoScroll = true, Padding = new Padding(8) };
            var table = Table();
            _tabs.TabPages.Add(page);
            page.Controls.Add(table);
            return table;
        }

        private static TableLayoutPanel Table()
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Margin = Padding.Empty };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return table;
        }

        private static TableLayoutPanel Section(TableLayoutPanel parent, string title)
        {
            var group = new GroupBox
            {
                Text = title, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 3, 8, 5)
            };
            var table = Table();
            group.Controls.Add(table);
            Row(parent, "", group);
            group.Margin = new Padding(3, 4, 3, 8);
            return table;
        }

        private static void Row(TableLayoutPanel table, string label, Control control)
        {
            var row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Top;
            control.Margin = new Padding(3, 5, 3, 5);
            if (string.IsNullOrEmpty(label))
            {
                table.Controls.Add(control, 0, row);
                table.SetColumnSpan(control, 2);
            }
            else
            {
                table.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 8, 3, 5) }, 0, row);
                table.Controls.Add(control, 1, row);
            }
        }

        private void Button(FlowLayoutPanel panel, string text, Func<Task> action)
        {
            var button = new Button { Text = text, AutoSize = true, Margin = new Padding(3), Padding = new Padding(5, 1, 5, 1) };
            button.Click += async (sender, args) => await Busy(action);
            panel.Controls.Add(button);
        }

        private static ComboBox Choice(params object[] choices)
        {
            var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            combo.Items.AddRange(choices);
            combo.SelectedIndex = 0;
            return combo;
        }

        private static ComboBox Picker() => new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems, IntegralHeight = false, DropDownHeight = 300
        };

        private static CheckBox Check(string text, bool value = false) => new CheckBox { Text = text, Checked = value, AutoSize = true };
        private static Label Note(string text) => new Label { Text = text, AutoSize = true, MaximumSize = new Size(600, 0) };
        private static NumericUpDown Number(decimal min, decimal max, decimal value, int places = 0, decimal step = 1) =>
            new NumericUpDown { Minimum = min, Maximum = max, Value = value, DecimalPlaces = places, Increment = step, Width = 120 };

        private static void Toggle(CheckBox check, Control panel)
        {
            panel.Visible = check.Checked;
            check.CheckedChanged += (sender, args) => panel.Visible = check.Checked;
        }

        private async Task Busy(Func<Task> action)
        {
            if (_busy || _closing) return;
            _busy = true;
            _tabs.Enabled = false;
            _buttons.Enabled = false;
            try { await action(); }
            catch (Exception exception) { Log(exception.ToString()); }
            finally
            {
                _busy = false;
                if (!IsDisposed && !_closing) { _tabs.Enabled = true; _buttons.Enabled = true; }
            }
        }

        private async Task Run(Func<string> action) => Log(await GameThread.Run(action));

        private void Log(string message)
        {
            if (!IsDisposed) _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + " " + message + Environment.NewLine);
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_allowClose) return;
            e.Cancel = true;
            if (_closing) return;
            _closing = true;
            _tabs.Enabled = false;
            _buttons.Enabled = false;
            try { await GameThread.Run(LocalGameControls.RestoreTiming); }
            catch (Exception exception) { Log("Could not restore timing: " + exception.Message); }
            _allowClose = true;
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _messages.Dispose();
            base.OnFormClosed(e);
        }
    }
}
