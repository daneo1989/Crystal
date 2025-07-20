using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirSounds;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using C = ClientPackets;

namespace Client.MirScenes.Dialogs
{
    public class AdventurerJournalDialog : MirImageControl
    {
        public static AdventurerJournalDialog Instance;

        private MirButton CloseButton;
        private MirButton NoteBookButton, AchievementButton, QuestButton;
        private MirLabel NotebookDisplay;
        private MirTextBox NotebookInput;
        private MirButton EditButton, SaveButton;
        private MirButton UpButton, DownButton;
        private MirImageControl PositionBar;
        private MirTextBox SearchBox;
        private MirButton SearchButton;
        private MirLabel[] SearchResults;
        private MirLabel SearchCountLabel;

        private string NotebookContent = string.Empty;
        private int ScrollOffset;
        private const int VisibleLineCount = 25;
        private const int ScrollStep = 1;

        private readonly List<int> SearchIndices = new();
        private int SearchOffset;
        private string LastQuery = string.Empty;
        private const int MaxSearchResults = 20;

        public AdventurerJournalDialog()
        {
            Index = 0;
            Library = Libraries.Custom;
            Movable = true;
            Sort = true;
            Location = Center;
            Visible = false;

            InitializeTabs();
            InitializeNotebookControls();
            InitializeSearchControls();
            InitializeScrollControls();
            InitializeCloseButton();
            InitializeMouseWheel();

            Instance = this;
        }

        private void InitializeTabs()
        {
            NoteBookButton = CreateTabButton(2, 3, new Point(138, 68), ShowNotebookTab);
            AchievementButton = CreateTabButton(4, 5, new Point(209, 68), ShowAchievements);
            QuestButton = CreateTabButton(6, 7, new Point(280, 68), ShowQuests);
        }

        private void InitializeNotebookControls()
        {
            NotebookDisplay = new MirLabel
            {
                Parent = this,
                Location = new Point(142, 103),
                Size = new Size(526, 330),
                Font = new Font("Segoe UI", 8F),
                BackColour = Color.Transparent,
                ForeColour = Color.WhiteSmoke,
                NotControl = true,
                AutoSize = false
            };

            NotebookInput = new MirTextBox
            {
                Parent = this,
                Location = new Point(142, 103),
                Size = new Size(526, 330),
                Font = new Font("Segoe UI", 8F),
                MaxLength = 500000,
                CanLoseFocus = true,
                Visible = false,
                BackColour = Color.FromArgb(25, 25, 25),
                Border = false
            };
            NotebookInput.MultiLine();
            NotebookInput.TextBox.ScrollBars = ScrollBars.Vertical;

            EditButton = CreateButton(560, 561, 562, new Point(658, 36), "Edit Notebook", EnterEditMode);
            SaveButton = CreateButton(554, 555, 556, new Point(658, 36), "Save Notebook", SaveAndExitEditMode);
            SaveButton.Visible = false;
        }

        private void InitializeSearchControls()
        {
            SearchBox = new MirTextBox
            {
                Parent = this,
                Location = new Point(540, 69),
                Size = new Size(50, 20),
                Font = new Font("Segoe UI", 8F),
                MaxLength = 100,
                BackColour = Color.Black,
                ForeColour = Color.Gray,
                Text = "Search",
                Border = false,
                Visible = true,
            };
            SearchBox.Click += (s, e) =>
            {
                if (SearchBox.ForeColour == Color.Gray)
                {
                    SearchBox.Text = string.Empty;
                    SearchBox.ForeColour = Color.WhiteSmoke;
                }
            };

            SearchButton = CreateButton(1340, 1341, 1342, new Point(539, 34), "Search", DoSearch);
            SearchButton.Library = Libraries.Prguse2;
            SearchButton.BringToFront();

            SearchResults = new MirLabel[MaxSearchResults];
            for (int i = 0; i < MaxSearchResults; i++)
            {
                SearchResults[i] = new MirLabel
                {
                    Parent = this,
                    Location = new Point(15, 103 + i * 15),
                    Size = new Size(240, 14),
                    Font = new Font("Segoe UI", 8F),
                    ForeColour = Color.WhiteSmoke,
                    BackColour = Color.Transparent,
                    NotControl = false,
                    Visible = false,
                    Text = string.Empty
                };
                SearchResults[i].Click += OnSearchResult;
                SearchResults[i].MouseWheel += OnSearchScroll;
            }

            SearchCountLabel = new MirLabel
            {
                Parent = this,
                Location = new Point(12, 103 + MaxSearchResults * 15 + 4),
                Size = new Size(240, 32),
                Font = new Font("Segoe UI", 8F),
                ForeColour = Color.Gray,
                BackColour = Color.Transparent,
                NotControl = true,
                Visible = true,
                AutoSize = false,
                Text = "Awaiting search"
            };
        }

        private void InitializeScrollControls()
        {
            UpButton = new MirButton
            {
                Index = 197,
                HoverIndex = 198,
                PressedIndex = 199,
                Library = Libraries.Prguse2,
                Location = new Point(121, 103),
                Size = new Size(16, 14),
                Parent = this,
                Sound = SoundList.ButtonA
            };
            UpButton.Click += UpButton_Click;

            DownButton = new MirButton
            {
                Index = 207,
                HoverIndex = 208,
                PressedIndex = 209,
                Library = Libraries.Prguse2,
                Location = new Point(121, 421),
                Size = new Size(16, 14),
                Parent = this,
                Sound = SoundList.ButtonA
            };
            DownButton.Click += DownButton_Click;

            PositionBar = new MirImageControl
            {
                Index = 205,
                Library = Libraries.Prguse2,
                Location = new Point(121, 117),
                Size = new Size(16, 30),
                Parent = this,
                Movable = false
            };
        }

        private void InitializeCloseButton()
        {
            CloseButton = new MirButton
            {
                Index = 360,
                HoverIndex = 361,
                PressedIndex = 362,
                Location = new Point(671, 4),
                Library = Libraries.Prguse2,
                Parent = this,
                Sound = SoundList.ButtonA
            };
            CloseButton.Click += (s, e) => Hide();
        }

        private void InitializeMouseWheel()
        {
            MouseWheel += (s, e) =>
            {
                if (NotebookInput.Visible && MirControl.ActiveControl == NotebookInput)
                    return;

                if (NotebookDisplay.Visible)
                {
                    string[] lines = GetNotebookLines();
                    int total = lines.Length + 2;
                    int maxOffset = Math.Max(0, total - VisibleLineCount);

                    ScrollOffset += e.Delta > 0 ? -ScrollStep : ScrollStep;
                    ScrollOffset = Math.Clamp(ScrollOffset, 0, maxOffset);

                    UpdateNotebookDisplay();
                }
                else if (!string.IsNullOrEmpty(LastQuery))
                {
                    OnSearchScroll(this, e);
                }
            };
        }

        private MirButton CreateTabButton(int idx, int pidx, Point loc, EventHandler onClick)
        {
            var btn = new MirButton
            {
                Index = idx,
                PressedIndex = pidx,
                Library = Libraries.Custom,
                Location = loc,
                Parent = this,
                Sound = SoundList.ButtonA
            };
            btn.Click += onClick;
            return btn;
        }

        private MirButton CreateButton(int idx, int hidx, int pidx, Point loc, string hint, EventHandler onClick)
        {
            var btn = new MirButton
            {
                Index = idx,
                HoverIndex = hidx,
                PressedIndex = pidx,
                Library = Libraries.Prguse,
                Location = loc,
                Parent = this,
                Sound = SoundList.ButtonA,
                Hint = hint
            };
            btn.Click += onClick;
            return btn;
        }

        public void ShowNotebook()
        {
            ShowNotebookTab(this, EventArgs.Empty);
        }

        public void SetNotebookText(string text)
        {
            NotebookContent = text;
            NotebookInput.Text = text;
            ScrollOffset = 0;
            UpdateNotebookDisplay();
        }

        private void ShowNotebookTab(object sender, EventArgs e)
        {
            UpdateNotebookDisplay();

            NotebookDisplay.Visible = true;
            NotebookInput.Visible = false;
            EditButton.Visible = true;
            SaveButton.Visible = false;
            UpButton.Visible = DownButton.Visible = PositionBar.Visible = true;
            SearchBox.Visible = SearchButton.Visible = true;

            foreach (var lbl in SearchResults)
            {
                lbl.Text = string.Empty;
                lbl.Visible = false;
            }

            SearchCountLabel.Text = "Awaiting search";
            SearchCountLabel.Visible = true;
        }

        private void ShowAchievements(object sender, EventArgs e)
        {
            HideAll();
            GameScene.Scene.ChatDialog.ReceiveChat("Achievements coming soon.", ChatType.Hint);
        }

        private void ShowQuests(object sender, EventArgs e)
        {
            HideAll();
            GameScene.Scene.ChatDialog.ReceiveChat("Quests coming soon.", ChatType.Hint);
        }

        private void HideAll()
        {
            NotebookDisplay.Visible = false;
            NotebookInput.Visible = false;
            EditButton.Visible = false;
            SaveButton.Visible = false;
            UpButton.Visible = DownButton.Visible = PositionBar.Visible = false;
            SearchBox.Visible = false;
            SearchButton.Visible = false;

            foreach (var lbl in SearchResults)
                lbl.Visible = false;

            SearchCountLabel.Visible = false;
        }

        private void EnterEditMode(object sender, EventArgs e)
        {
            NotebookInput.Text = NotebookContent;
            NotebookInput.Visible = true;
            NotebookDisplay.Visible = false;
            EditButton.Visible = false;
            SaveButton.Visible = true;

            SearchBox.Enabled = false;
            SearchButton.Enabled = false;

            foreach (var lbl in SearchResults)
                lbl.Visible = false;

            SearchCountLabel.Visible = false;
        }

        private void SaveAndExitEditMode(object sender, EventArgs e)
        {
            NotebookContent = NotebookInput.Text;

            ScrollOffset = 0;
            UpdateNotebookDisplay();
            Network.Enqueue(new C.SaveNotebook { Content = NotebookContent });
            ShowNotebook();
            GameScene.Scene.ChatDialog.ReceiveChat("Notebook saved.", ChatType.Hint);

            SearchBox.Enabled = true;
            SearchButton.Enabled = true;
        }

        private void UpdateNotebookDisplay()
        {
            string displayContent = NotebookContent.TrimEnd('\r', '\n') + "\n\n"; // Append 2 blank lines for smooth scroll
            string[] lines = displayContent.Split(new[] { "\n" }, StringSplitOptions.None);

            int total = lines.Length;
            int maxOffset = Math.Max(0, total - VisibleLineCount);

            ScrollOffset = Math.Clamp(ScrollOffset, 0, maxOffset);

            int count = VisibleLineCount;
            if (ScrollOffset + count > total)
                count = total - ScrollOffset;

            NotebookDisplay.Text = string.Join("\n", lines, ScrollOffset, count);
            UpdatePositionBar();
        }

        private string[] GetNotebookLines()
        {
            string normalized = NotebookContent.Replace("\r\n", "\n");
            var lines = normalized.Split(new[] { '\n' }, StringSplitOptions.None).ToList();

            if (normalized.EndsWith("\n"))
            {
                int trailingNewlines = normalized.Length - normalized.TrimEnd('\n').Length;
                while (lines.Count < trailingNewlines + (lines.Count > 0 ? 1 : 0))
                    lines.Add(string.Empty);
            }

            return lines.ToArray();
        }

        private void UpdatePositionBar()
        {
            int total = GetLineCount() + 2; // Account for the 2 appended blank lines
            int maxOffset = Math.Max(0, total - VisibleLineCount);

            ScrollOffset = Math.Clamp(ScrollOffset, 0, maxOffset);

            PositionBar.Visible = true;

            int minY = 117;
            int maxY = 420 - 1 - PositionBar.Size.Height;

            if (maxOffset == 0)
            {
                // If no scroll needed, position bar at top
                PositionBar.Location = new Point(121, minY);
                return;
            }

            float scrollRatio = (float)ScrollOffset / maxOffset;
            int posY = minY + (int)((maxY - minY) * scrollRatio);
            posY = Math.Clamp(posY, minY, maxY);

            PositionBar.Location = new Point(121, posY);
        }

        private int GetLineCount()
        {
            if (string.IsNullOrEmpty(NotebookContent))
                return 0;

            return NotebookContent.Split(new[] { "\n" }, StringSplitOptions.None).Length;
        }

        private void UpButton_Click(object sender, EventArgs e)
        {
            ScrollOffset = Math.Max(0, ScrollOffset - ScrollStep);
            UpdateNotebookDisplay();
        }

        private void DownButton_Click(object sender, EventArgs e)
        {
            string[] lines = NotebookContent.Split(new[] { "\n" }, StringSplitOptions.None);
            int total = lines.Length + 2; // account for appended lines
            int maxOffset = Math.Max(0, total - VisibleLineCount);

            ScrollOffset = Math.Min(maxOffset, ScrollOffset + ScrollStep);
            UpdateNotebookDisplay();
        }

        private void DoSearch(object sender, EventArgs e)
        {
            LastQuery = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(LastQuery))
            {
                SearchIndices.Clear();
                SearchOffset = 0;

                foreach (var lbl in SearchResults)
                {
                    lbl.Text = string.Empty;
                    lbl.Visible = false;
                }

                SearchCountLabel.Text = string.Empty;
                SearchCountLabel.Visible = false;
                return;
            }

            SearchIndices.Clear();
            SearchOffset = 0;

            string[] lines = NotebookContent.Split(new[] { "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(LastQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    SearchIndices.Add(i);
            }

            RefreshResults();
        }

        private void RefreshResults()
        {
            string[] lines = NotebookContent.Split(new[] { "\n" }, StringSplitOptions.None);
            string query = LastQuery.ToLower();

            for (int i = 0; i < SearchResults.Length; i++)
            {
                SearchResults[i].Text = string.Empty;
                SearchResults[i].Visible = false;
            }

            int shownCount = 0;

            for (int i = 0; i < MaxSearchResults; i++)
            {
                int idx = SearchOffset + i;
                if (idx < SearchIndices.Count)
                {
                    int lineNumber = SearchIndices[idx] + 1;
                    string line = lines[SearchIndices[idx]];
                    int foundIndex = line.ToLower().IndexOf(query);
                    if (foundIndex >= 0)
                    {
                        string matchedWord = line.Substring(foundIndex, query.Length);
                        SearchResults[i].Text = $"{lineNumber}: {matchedWord}";
                        SearchResults[i].Visible = true;
                        shownCount++;
                    }
                }
            }

            SearchCountLabel.Text = shownCount == 0
                ? "No results found"
                : $"{SearchIndices.Count} \"{LastQuery}\" found";

            SearchCountLabel.Visible = true;
        }

        private void OnSearchResult(object sender, EventArgs e)
        {
            var lbl = sender as MirLabel;
            int idx = Array.IndexOf(SearchResults, lbl);
            int actual = SearchOffset + idx;

            if (actual < SearchIndices.Count)
            {
                ScrollOffset = SearchIndices[actual];
                UpdateNotebookDisplay();
            }
        }

        private void OnSearchScroll(object sender, MouseEventArgs e)
        {
            if (string.IsNullOrEmpty(LastQuery)) return;

            int maxOffset = Math.Max(0, SearchIndices.Count - MaxSearchResults);
            SearchOffset = e.Delta > 0
                ? Math.Max(0, SearchOffset - 1)
                : Math.Min(maxOffset, SearchOffset + 1);

            RefreshResults();
        }
    }
}
