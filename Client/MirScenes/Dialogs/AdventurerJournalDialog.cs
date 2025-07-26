using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System;
using C = ClientPackets;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirSounds;

using Client.MirControls;

namespace Client.MirScenes.Dialogs
{
    public class AdventurerJournalDialog : MirImageControl
    {
        public static AdventurerJournalDialog Instance;

        private MirButton CloseButton, NoteBookButton, AchievementButton, QuestButton;
        private MirLabel totalGold, totalCredits;

        // Notebook
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
        private const int VisibleLineCount = 25, ScrollStep = 1, MaxSearchResults = 20;
        private List<int> SearchIndices = new List<int>();
        private int SearchOffset;
        private string LastQuery = "";

        // Quest UI
        public List<ClientQuestProgress> Quests = new List<ClientQuestProgress>();
        private List<ClientQuestProgress> AllQuests = new List<ClientQuestProgress>();
        private MirLabel _takenQuestsLabel, PageNumberLabel;
        private MirControl QuestCellsContainer;
        private List<MirQuestCell> QuestCells = new List<MirQuestCell>();
        private int currentPage = 0;
        private const int QuestsPerPage = 8;
        private MirButton PreviousButton, NextButton;

        private readonly int cellWidth = 125, cellHeight = 146, columns = 4, horizontalSpacing = 6, verticalSpacing = 15, offsetX = 5, offsetY = 5;

        private enum FilterType { Category, Difficulty, Region, MinLevel, MaxLevel, Gold, Exp }
        private class FilterOption
        {
            public string Name;
            public FilterType Type;
            public string Parent;
            public bool Expanded;
            public bool IsHeader => Parent == null;
        }

        private List<FilterOption> ActiveFilters = new();
        private List<MirLabel> FilterLabels = new();
        private List<MirButton> ClassButtons = new();
        private MirButton FilterScrollUp, FilterScrollDown;
        private MirImageControl FilterScrollBar;
        private MirControl FilterContainer;
        private int FilterScrollOffset = 0;
        private const int MaxVisibleFilters = 16;

        private HashSet<string> SelectedCategories = new();
        private HashSet<string> SelectedRegions = new();
        private HashSet<string> SelectedDifficulties = new();

        private string SelectedMinLevel = null;
        private string SelectedMaxLevel = null;
        private string SelectedGold = null;
        private string SelectedExp = null;
        private readonly List<FilterOption> VisibleFilterOptions = new();

        private MirTextBox MinLevelBox, MaxLevelBox, GoldBox, ExpBox;
        private MirLabel MinLevelLabel, MaxLevelLabel, GoldLabel, ExpLabel;
        private Dictionary<RequiredClass, MirButton> ClassFilterButtons = new();
        private HashSet<RequiredClass> SelectedClasses = new();

        public AdventurerJournalDialog()
        {
            Index = 0;
            Library = Libraries.Custom;
            Movable = true;
            Sort = true;
            Location = Center;
            Visible = false;
            Instance = this;

            InitializeTabs();
            InitializeCloseButton();
            InitializeGoldCredits();
            InitializeNotebookControls();
            InitializeSearchControls();
            InitializeScrollControls();
            InitializeQuestControls();
            InitializeMouseWheel();
            InitializeFilterUI();

            ShowNotebookTab(this, EventArgs.Empty);
        }

        #region Initialization
        private void InitializeTabs()
        {
            NoteBookButton = new MirButton
            {
                Index = 2,
                PressedIndex = 3,
                Library = Libraries.Custom,
                Location = new Point(138, 68),
                Parent = this,
                Sound = SoundList.ButtonA
            };
            NoteBookButton.Click += ShowNotebookTab;

            AchievementButton = new MirButton
            {
                Index = 4,
                PressedIndex = 5,
                Library = Libraries.Custom,
                Location = new Point(209, 68),
                Parent = this,
                Sound = SoundList.ButtonA
            };
            AchievementButton.Click += ShowAchievements;

            QuestButton = new MirButton
            {
                Index = 6,
                PressedIndex = 7,
                Library = Libraries.Custom,
                Location = new Point(280, 68),
                Parent = this,
                Sound = SoundList.ButtonA
            };
            QuestButton.Click += ShowQuests;
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
        private void InitializeGoldCredits()
        {
            totalGold = new MirLabel
            {
                Size = new Size(100, 20),
                DrawFormat = TextFormatFlags.RightToLeft | TextFormatFlags.Right,
                Location = new Point(123, 449),
                Parent = this,
                NotControl = true,
                Font = new Font(Settings.FontName, 8F)
            };
            totalCredits = new MirLabel
            {
                Size = new Size(100, 20),
                DrawFormat = TextFormatFlags.RightToLeft | TextFormatFlags.Right,
                Location = new Point(5, 449),
                Parent = this,
                NotControl = true,
                Font = new Font(Settings.FontName, 8F)
            };
            
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
            NotebookInput.MultiLine(); NotebookInput.TextBox.ScrollBars = ScrollBars.Vertical;
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
                Visible = true
            };
            SearchBox.Click += (s, e) => { if (SearchBox.ForeColour == Color.Gray) { SearchBox.Text = ""; SearchBox.ForeColour = Color.WhiteSmoke; } };
            SearchButton = CreateButton(1340, 1341, 1342, new Point(539, 34), "Search", DoSearch); SearchButton.Library = Libraries.Prguse2; SearchButton.BringToFront();
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
                    Text = ""
                };
                SearchResults[i].Click += OnSearchResult; SearchResults[i].MouseWheel += OnSearchScroll;
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
            }; UpButton.Click += UpButton_Click;

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
        private void InitializeQuestControls()
        {
            _takenQuestsLabel = new MirLabel
            {
                Font = new Font(Settings.FontName, 8F),
                Parent = this,
                AutoSize = true,
                Location = new Point(540, 69),
                Visible = false
            };
            QuestCellsContainer = new MirControl
            {
                Parent = this,
                Location = new Point(150, 110),
                Size = new Size(offsetX * 2 + (cellWidth * columns) + (horizontalSpacing * (columns - 1)), offsetY * 2 + (cellHeight * 2) + verticalSpacing),
                Visible = false
            };
            PageNumberLabel = new MirLabel
            {
                Text = "",
                Parent = this,
                Size = new Size(83, 17),
                Location = new Point(597, 446),
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                Font = new Font(Settings.FontName, 7F),
            };
            PreviousButton = new MirButton
            {
                Index = 240,
                HoverIndex = 241,
                PressedIndex = 242,
                Library = Libraries.Prguse2,
                Parent = this,
                Location = new Point(600, 448),
                Sound = SoundList.ButtonA,
                Visible = true,
            };
            PreviousButton.Click += (o, e) =>
            {
                if (currentPage > 0)
                {
                    currentPage--; ShowPage(currentPage);
                }
            };
            NextButton = new MirButton
            {
                Index = 243,
                HoverIndex = 244,
                PressedIndex = 245,
                Library = Libraries.Prguse2,
                Parent = this,
                Location = new Point(660, 448),
                Sound = SoundList.ButtonA,
            };
            NextButton.Click += (o, e) =>
            {
                if ((currentPage + 1) * QuestsPerPage < Quests.Count)
                {
                    currentPage++; ShowPage(currentPage);
                }
            };
            InitializeClassFilterButtons();
        }
        private void InitializeMouseWheel()
        {
            MouseWheel += (s, e) =>
            {
                if (NotebookInput.Visible && MirControl.ActiveControl == NotebookInput) return;
                if (NotebookDisplay.Visible)
                {
                    string[] lines = GetNotebookLines();
                    int total = lines.Length + 2;
                    int maxOffset = Math.Max(0, total - VisibleLineCount);
                    ScrollOffset += e.Delta > 0 ? -ScrollStep : ScrollStep;
                    ScrollOffset = Math.Clamp(ScrollOffset, 0, maxOffset);
                    UpdateNotebookDisplay();
                }

                else if (QuestCellsContainer.Visible)
                {
                    OnFilterMouseWheel(this, e);
                }
                else if (!string.IsNullOrEmpty(LastQuery))
                {
                    OnSearchScroll(this, e);
                }
            };
        }
        #endregion

        #region Tabs & Show/Hide
        public void ShowNotebookTab(object sender, EventArgs e)
        {
            ResetQuestUI();
            HideAllSections();
            ResetTabButtonStates();
            NoteBookButton.Index = 3;

            NotebookDisplay.Visible = true;
            NotebookInput.Visible = false;
            EditButton.Visible = true;
            SaveButton.Visible = false;
            UpButton.Visible = true;
            DownButton.Visible = true;
            PositionBar.Visible = true;

            SearchBox.Visible = true;
            SearchButton.Visible = true;

            foreach (var lbl in SearchResults) { lbl.Text = ""; lbl.Visible = false; }
            SearchCountLabel.Text = "Awaiting search";
            SearchCountLabel.Visible = true;

            totalGold.Visible = true;
            totalCredits.Visible = true;
            NoteBookButton.Visible = true;
            AchievementButton.Visible = true;
            QuestButton.Visible = true;
            FilterContainer.Visible = false;

            NoteBookButton.BringToFront();
            AchievementButton.BringToFront();
            QuestButton.BringToFront();

        }
        private void ShowAchievements(object sender, EventArgs e)
        {
            ResetQuestUI();
            HideAllSections();
            ResetTabButtonStates();
            AchievementButton.Index = 5;

            GameScene.Scene.ChatDialog.ReceiveChat("Achievements coming soon.", ChatType.Hint);

            totalGold.Visible = true;
            totalCredits.Visible = true;
            NoteBookButton.Visible = true;
            AchievementButton.Visible = true;
            QuestButton.Visible = true;
            FilterContainer.Visible = false;

            NoteBookButton.BringToFront();
            AchievementButton.BringToFront();
            QuestButton.BringToFront();

        }
        private void ShowQuests(object sender, EventArgs e)
        {
            HideAllSections();
            ResetSearch();
            ResetTabButtonStates();
            QuestButton.Index = 7;

            SelectedCategories.Clear();
            SelectedRegions.Clear();
            SelectedDifficulties.Clear();

            AllQuests = GameScene.User.CurrentQuests.ToList();
            BuildFilters();
            DisplayQuests();

            FilterContainer.Visible = true;

            foreach (var b in ClassButtons)
                b.Visible = true;

            FilterScrollUp.Visible = true;
            FilterScrollDown.Visible = true;
            FilterScrollBar.Visible = true;
        }

        private void HideAllSections()
        {
            NotebookDisplay.Visible = false;
            NotebookInput.Visible = false;
            EditButton.Visible = false;
            SaveButton.Visible = false;
            UpButton.Visible = false;
            DownButton.Visible = false;
            PositionBar.Visible = false;
            SearchBox.Visible = false;
            SearchButton.Visible = false;
            foreach (var lbl in SearchResults) lbl.Visible = false;
            SearchCountLabel.Visible = false;
            _takenQuestsLabel.Visible = false;
            QuestCellsContainer.Visible = false;
            PreviousButton.Visible = false;
            NextButton.Visible = false;
            PageNumberLabel.Visible = false;

            foreach (var b in ClassButtons)
                b.Visible = false;

            FilterScrollUp.Visible = false;
            FilterScrollDown.Visible = false;
            FilterScrollBar.Visible = false;
        }
        private void ResetTabButtonStates()
        {
            NoteBookButton.Index = 2;
            AchievementButton.Index = 4;
            QuestButton.Index = 6;
        }

        #endregion

        #region Quest Filtering/Display
        public void DisplayQuests()
        {
            var filtered = AllQuests.AsEnumerable();

            if (SelectedCategories.Count > 0)
                filtered = filtered.Where(q => SelectedCategories.Contains(q.QuestInfo.Type.ToString()));

            if (SelectedRegions.Count > 0)
                filtered = filtered.Where(q => !string.IsNullOrEmpty(q.QuestInfo.Group) &&
                                               SelectedRegions.Contains(q.QuestInfo.Group));

            if (SelectedDifficulties.Count > 0)
                filtered = filtered.Where(q => SelectedDifficulties.Contains(q.QuestInfo.Type.ToString()));

            if (SelectedClasses.Count > 0)
                filtered = filtered.Where(q => SelectedClasses.Contains(q.QuestInfo.ClassNeeded));

            if (int.TryParse(MinLevelBox.Text, out int minLevel))
                filtered = filtered.Where(q => q.QuestInfo.MinLevelNeeded >= minLevel);

            if (int.TryParse(MaxLevelBox.Text, out int maxLevel))
                filtered = filtered.Where(q => q.QuestInfo.MaxLevelNeeded <= maxLevel);

            if (int.TryParse(GoldBox.Text, out int minGold))
                filtered = filtered.Where(q => q.QuestInfo.RewardGold >= minGold);

            if (int.TryParse(ExpBox.Text, out int minExp))
                filtered = filtered.Where(q => q.QuestInfo.RewardExp >= minExp);

            Quests = filtered.ToList();

            _takenQuestsLabel.Text = $"Quests: {Quests.Count}/{Globals.MaxConcurrentQuests}";
            _takenQuestsLabel.Visible = true;

            currentPage = 0;
            ShowPage(currentPage);

            PreviousButton.Visible = true;
            NextButton.Visible = true;
            QuestCellsContainer.Visible = true;
            PageNumberLabel.Visible = true;
        }
        private void ResetQuestUI()
        {
            FilterScrollOffset = 0;
            SelectedCategories.Clear();
            SelectedRegions.Clear();
            SelectedDifficulties.Clear();
            SelectedClasses.Clear();

            SelectedMinLevel = null;
            SelectedMaxLevel = null;
            SelectedGold = null;
            SelectedExp = null;

            MinLevelBox.Text = "";
            MaxLevelBox.Text = "";
            GoldBox.Text = "";
            ExpBox.Text = "";

            foreach (var lbl in FilterLabels)
                lbl.Visible = false;

            FilterScrollUp.Visible = false;
            FilterScrollDown.Visible = false;
            FilterScrollBar.Visible = false;
            FilterContainer.Visible = false;

            foreach (var btn in ClassButtons)
                btn.Visible = false;
        }
        private void InitializeFilterUI()
        {
            FilterContainer = new MirControl
            {
                Parent = this,
                Location = new Point(11, 103),
                Size = new Size(110, 460),
                Visible = false
            };

            MirLabel resetLabel = new MirLabel
            {
                Parent = FilterContainer,
                Location = new Point(1, 255),
                Size = new Size(70, 14),
                Text = "Reset Filters",
                Font = new Font(Settings.FontName, 7F, FontStyle.Bold),
                ForeColour = Color.White,
                BackColour = Color.FromArgb(40, 40, 40),
                Border = true,
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                NotControl = false,
                Visible = true
            };
            resetLabel.MouseEnter += (s, e) => resetLabel.BackColour = Color.FromArgb(60, 60, 60);
            resetLabel.MouseLeave += (s, e) => resetLabel.BackColour = Color.FromArgb(40, 40, 40);
            resetLabel.Click += (o, e) => ResetAllFilters();

            FilterScrollUp = new MirButton
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
            FilterScrollUp.Click += (o, e) => { FilterScrollOffset = Math.Max(0, FilterScrollOffset - 1); ShowFilter(); };

            FilterScrollDown = new MirButton
            {
                Index = 207,
                HoverIndex = 208,
                PressedIndex = 209,
                Library = Libraries.Prguse2,
                Location = new Point(121, 422),
                Size = new Size(16, 14),
                Parent = this,
                Sound = SoundList.ButtonA
            };
            FilterScrollDown.Click += (o, e) => { FilterScrollOffset++; ShowFilter(); };

            FilterScrollBar = new MirImageControl
            {
                Index = 205,
                Library = Libraries.Prguse2,
                Location = new Point(121, 117),
                Size = new Size(16, 30),
                Parent = this,
                Movable = true,
                Visible = true
            };
            FilterScrollBar.OnMoving += (o, e) =>
            {
                int minY = FilterScrollUp.Location.Y + FilterScrollUp.Size.Height + 3;
                int maxY = FilterScrollDown.Location.Y - FilterScrollBar.Size.Height - 3;
                int newY = Math.Clamp(e.Location.Y, minY, maxY);
                float percent = (float)(newY - minY) / (maxY - minY);
                int maxOffset = Math.Max(0, VisibleFilterOptions.Count - MaxVisibleFilters);
                FilterScrollOffset = (int)(percent * maxOffset);
                ShowFilter();
            };

            for (int i = 0; i < MaxVisibleFilters; i++)
            {
                int optionIndex = i;
                var label = new MirLabel
                {
                    Parent = FilterContainer,
                    Location = new Point(0, 2 + i * 15),
                    Size = new Size(90, 15),
                    Font = new Font(Settings.FontName, 7F),
                    ForeColour = Color.White,
                    Visible = false
                };
                label.Click += (s, e) => OnFilterLabelClick(optionIndex);
                FilterLabels.Add(label);
            }

            FilterContainer.MouseWheel += OnFilterMouseWheel;

            MinLevelLabel = CreateFilterLabel("Min Lv:", new Point(3, 273));
            MinLevelBox = CreateFilterBox(new Point(50, 273));

            MaxLevelLabel = CreateFilterLabel("Max Lv:", new Point(3, 288));
            MaxLevelBox = CreateFilterBox(new Point(50, 288));

            GoldLabel = CreateFilterLabel("Gold:", new Point(3, 303));
            GoldBox = CreateFilterBox(new Point(50, 303));

            ExpLabel = CreateFilterLabel("EXP:", new Point(3, 318));
            ExpBox = CreateFilterBox(new Point(50, 318));

            FilterContainer.Controls.AddRange(new MirControl[]
            {
                MinLevelLabel,
                MinLevelBox,
                MaxLevelLabel,
                MaxLevelBox,
                GoldLabel,
                GoldBox,
                ExpLabel,
                ExpBox
            });

        }

        private MirLabel CreateFilterLabel(string text, Point loc)
        {
            return new MirLabel
            {
                Parent = FilterContainer,
                Location = loc,
                Size = new Size(80, 12),
                Font = new Font(Settings.FontName, 7F, FontStyle.Bold),
                ForeColour = Color.White,
                Text = text,
                NotControl = true
            };
        }

        private MirTextBox CreateFilterBox(Point loc)
        {
            var box = new MirTextBox
            {
                Parent = FilterContainer,
                Location = loc,
                Size = new Size(45, 16),
                Font = new Font(Settings.FontName, 7F),
                BackColour = Color.Black,
                Border = true,
                Text = "",
                MaxLength = 5
            };

            box.TextBox.TextChanged += (s, e) => ApplyManualFilters();
            box.TextBox.Leave += (s, e) => ApplyManualFilters();
            box.TextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ApplyManualFilters();
                    e.SuppressKeyPress = true;
                }
            };

            return box;
        }
        private void ApplyManualFilters()
        {
            DisplayQuests();
        }

        private void InitializeClassFilterButtons()
        {
            void AddClassButton(RequiredClass rClass, string name, Point location)
            {
                var button = new MirButton
                {
                    Library = Libraries.Title,
                    Index = GetStaticIndexForClass(rClass),
                    Location = location,
                    Size = new Size(20, 20),
                    Parent = this,
                    Visible = false,
                    Sound = SoundList.ButtonA,
                    Hint = name,
                    Border = false,
                    BackColour = Color.Transparent
                };

                button.Click += (o, e) =>
                {
                    if (rClass == RequiredClass.None)
                        SelectedClasses.Clear();
                    else
                    {
                        if (SelectedClasses.Contains(rClass))
                            SelectedClasses.Remove(rClass);
                        else
                            SelectedClasses.Add(rClass);
                    }

                    UpdateClassButtonStates();
                    DisplayQuests();
                };

                ClassFilterButtons[rClass] = button;
                ClassButtons.Add(button);
            }

            AddClassButton(RequiredClass.None, "All", new Point(539, 37));
            AddClassButton(RequiredClass.Warrior, "Warrior", new Point(568, 38));
            AddClassButton(RequiredClass.Assassin, "Assassin", new Point(591, 38));
            AddClassButton(RequiredClass.Taoist, "Taoist", new Point(614, 38));
            AddClassButton(RequiredClass.Wizard, "Wizard", new Point(637, 38));
            AddClassButton(RequiredClass.Archer, "Archer", new Point(660, 38));
        }
        private int GetStaticIndexForClass(RequiredClass rClass)
        {
            return rClass switch
            {
                RequiredClass.None => 751, 
                RequiredClass.Warrior => 754,
                RequiredClass.Assassin => 757,
                RequiredClass.Taoist => 760,
                RequiredClass.Wizard => 763,
                RequiredClass.Archer => 766,
                _ => 751
            };
        }

        private void BuildFilters()
        {
            ActiveFilters.Clear();

            // Category
            ActiveFilters.Add(new FilterOption { Name = "Category", Type = FilterType.Category, Expanded = false });
            ActiveFilters.Add(new FilterOption { Name = "Show All", Type = FilterType.Category, Parent = "Category" });
            ActiveFilters.Add(new FilterOption { Name = "General", Type = FilterType.Category, Parent = "Category" });
            ActiveFilters.Add(new FilterOption { Name = "Daily", Type = FilterType.Category, Parent = "Category" });
            ActiveFilters.Add(new FilterOption { Name = "Repeatable", Type = FilterType.Category, Parent = "Category" });
            ActiveFilters.Add(new FilterOption { Name = "Story", Type = FilterType.Category, Parent = "Category" });

            // Difficulty
            ActiveFilters.Add(new FilterOption { Name = "Difficulty", Type = FilterType.Difficulty, Expanded = false });
            ActiveFilters.Add(new FilterOption { Name = "Show All", Type = FilterType.Difficulty, Parent = "Difficulty" });
            ActiveFilters.Add(new FilterOption { Name = "Easy", Type = FilterType.Difficulty, Parent = "Difficulty" });
            ActiveFilters.Add(new FilterOption { Name = "Normal", Type = FilterType.Difficulty, Parent = "Difficulty" });
            ActiveFilters.Add(new FilterOption { Name = "Hard", Type = FilterType.Difficulty, Parent = "Difficulty" });

            // Region
            ActiveFilters.Add(new FilterOption { Name = "Region", Type = FilterType.Region, Expanded = false });
            ActiveFilters.Add(new FilterOption { Name = "Show All", Type = FilterType.Region, Parent = "Region" });
            var territories = AllQuests.Select(q => q.QuestInfo.Group).Distinct();
            foreach (var region in territories)
            {
                ActiveFilters.Add(new FilterOption { Name = region, Type = FilterType.Region, Parent = "Region" });
            }
            ShowFilter();
        }

        private void ShowFilter()
        {
            VisibleFilterOptions.Clear();
            foreach (var header in ActiveFilters.Where(f => f.IsHeader))
            {
                VisibleFilterOptions.Add(header);
                if (header.Expanded)
                {
                    VisibleFilterOptions.AddRange(ActiveFilters.Where(f => f.Parent == header.Name));
                }
            }

            int total = VisibleFilterOptions.Count;
            int maxOffset = Math.Max(0, total - MaxVisibleFilters);
            FilterScrollOffset = Math.Clamp(FilterScrollOffset, 0, maxOffset);

            for (int i = 0; i < MaxVisibleFilters; i++)
            {
                int index = i + FilterScrollOffset;
                if (index < total)
                {
                    var item = VisibleFilterOptions[index];
                    FilterLabels[i].Text = item.IsHeader ? $"> {item.Name}" : $"   {item.Name}";

                    bool isSelected = item.Type switch
                    {
                        FilterType.Category => SelectedCategories.Count == 0 && item.Name == "Show All" || SelectedCategories.Contains(item.Name),
                        FilterType.Difficulty => SelectedDifficulties.Count == 0 && item.Name == "Show All" || SelectedDifficulties.Contains(item.Name),
                        FilterType.Region => SelectedRegions.Count == 0 && item.Name == "Show All" || SelectedRegions.Contains(item.Name),
                        FilterType.MinLevel => item.Name == SelectedMinLevel,
                        FilterType.MaxLevel => item.Name == SelectedMaxLevel,
                        FilterType.Gold => item.Name == SelectedGold,
                        FilterType.Exp => item.Name == SelectedExp,
                        _ => false
                    };

                    FilterLabels[i].ForeColour = isSelected ? Color.Yellow : Color.White;

                    FilterLabels[i].Visible = true;
                }
                else
                {
                    FilterLabels[i].Visible = false;
                }
            }
            FilterScrollUp.Visible = true;
            FilterScrollDown.Visible = true;
            FilterScrollBar.Visible = true;

            int minY = FilterScrollUp.Location.Y + FilterScrollUp.Size.Height + 3;
            int maxY = FilterScrollDown.Location.Y - FilterScrollBar.Size.Height - 3;
            if (maxOffset > 0)
            {
                float percent = (float)FilterScrollOffset / maxOffset;
                int barY = minY + (int)(percent * (maxY - minY));
                FilterScrollBar.Location = new Point(121, barY);
            }
            else
            {
                FilterScrollBar.Location = new Point(121, minY);
            }
        }

        private void OnFilterLabelClick(int index)
        {
            int actualIndex = FilterScrollOffset + index;
            if (actualIndex < 0 || actualIndex >= VisibleFilterOptions.Count) return;

            var selected = VisibleFilterOptions[actualIndex];

            if (selected.IsHeader)
            {
                selected.Expanded = !selected.Expanded;
                ShowFilter();
                return;
            }

            void Toggle(HashSet<string> set, string name)
            {
                if (name == "Show All") set.Clear();
                else if (!set.Add(name)) set.Remove(name);
            }

            switch (selected.Type)
            {
                case FilterType.Category:
                    Toggle(SelectedCategories, selected.Name);
                    break;
                case FilterType.Difficulty:
                    Toggle(SelectedDifficulties, selected.Name);
                    break;
                case FilterType.Region:
                    Toggle(SelectedRegions, selected.Name);
                    break;
                case FilterType.MinLevel:
                    SelectedMinLevel = selected.Name;
                    break;
                case FilterType.MaxLevel:
                    SelectedMaxLevel = selected.Name;
                    break;
                case FilterType.Gold:
                    SelectedGold = selected.Name;
                    break;
                case FilterType.Exp:
                    SelectedExp = selected.Name;
                    break;
            }

            DisplayQuests();
            ShowFilter();
        }

        private void OnFilterMouseWheel(object sender, MouseEventArgs e)
        {
            if (!QuestCellsContainer.Visible) return;

            int maxOffset = Math.Max(0, VisibleFilterOptions.Count - MaxVisibleFilters);
            FilterScrollOffset += e.Delta > 0 ? -1 : 1;
            FilterScrollOffset = Math.Clamp(FilterScrollOffset, 0, maxOffset);
            ShowFilter();
        }

        private void ResetAllFilters()
        {
            SelectedCategories.Clear();
            SelectedRegions.Clear();
            SelectedDifficulties.Clear();
            SelectedClasses.Clear();

            SelectedMinLevel = null;
            SelectedMaxLevel = null;
            SelectedGold = null;
            SelectedExp = null;

            MinLevelBox.Text = "";
            MaxLevelBox.Text = "";
            GoldBox.Text = "";
            ExpBox.Text = "";

            UpdateClassButtonStates();
            DisplayQuests();
            ShowFilter();
        }
        #endregion

        #region Notebook/Search Logic
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
            btn.Click += onClick; return btn;
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
        private void EnterEditMode(object sender, EventArgs e)
        {
            NotebookInput.Text = NotebookContent;
            NotebookInput.Visible = true;
            NotebookDisplay.Visible = false;
            EditButton.Visible = false;
            SaveButton.Visible = true;
            SearchBox.Enabled = false;
            SearchButton.Enabled = false;
            foreach (var lbl in SearchResults) lbl.Visible = false;
            SearchCountLabel.Visible = false;
        }
        private void SaveAndExitEditMode(object sender, EventArgs e)
        {
            NotebookContent = NotebookInput.Text;
            ScrollOffset = 0;
            UpdateNotebookDisplay();
            Network.Enqueue(new C.SaveNotebook
            {
                Content = NotebookContent
            });
            ShowNotebook();
            GameScene.Scene.ChatDialog.ReceiveChat("Notebook saved.", ChatType.Hint); SearchBox.Enabled = true; SearchButton.Enabled = true;
        }
        private void UpdateNotebookDisplay()
        {
            string displayContent = NotebookContent.TrimEnd('\r', '\n') + "\n\n";
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
            int total = GetLineCount() + 2;
            int maxOffset = Math.Max(0, total - VisibleLineCount);
            ScrollOffset = Math.Clamp(ScrollOffset, 0, maxOffset);
            PositionBar.Visible = true;

            int minY = 117, maxY = 420 - 1 - PositionBar.Size.Height;
            if (maxOffset == 0) { PositionBar.Location = new Point(121, minY); return; }
            float scrollRatio = (float)ScrollOffset / maxOffset;
            int posY = minY + (int)((maxY - minY) * scrollRatio);
            posY = Math.Clamp(posY, minY, maxY);
            PositionBar.Location = new Point(121, posY);
        }
        private int GetLineCount()
        {
            if (string.IsNullOrEmpty(NotebookContent)) return 0;
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
            int total = lines.Length + 2;
            int maxOffset = Math.Max(0, total - VisibleLineCount);
            ScrollOffset = Math.Min(maxOffset, ScrollOffset + ScrollStep);
            UpdateNotebookDisplay();
        }
        private void DoSearch(object sender, EventArgs e)
        {
            LastQuery = SearchBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(LastQuery)) { SearchIndices.Clear(); SearchOffset = 0; foreach (var lbl in SearchResults) { lbl.Text = ""; lbl.Visible = false; } SearchCountLabel.Text = ""; SearchCountLabel.Visible = false; return; }
            SearchIndices.Clear(); SearchOffset = 0;
            string[] lines = NotebookContent.Split(new[] { "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) if (lines[i].IndexOf(LastQuery, StringComparison.OrdinalIgnoreCase) >= 0) SearchIndices.Add(i);
            RefreshResults();
        }
        private void RefreshResults()
        {
            string[] lines = NotebookContent.Split(new[] { "\n" }, StringSplitOptions.None); string query = LastQuery.ToLower();
            for (int i = 0; i < SearchResults.Length; i++) { SearchResults[i].Text = ""; SearchResults[i].Visible = false; }
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
            SearchCountLabel.Text = shownCount == 0 ? "No results found" : $"{SearchIndices.Count} \"{LastQuery}\" found";
            SearchCountLabel.Visible = true;
        }
        private void OnSearchResult(object sender, EventArgs e)
        {
            var lbl = sender as MirLabel; int idx = Array.IndexOf(SearchResults, lbl); int actual = SearchOffset + idx;
            if (actual < SearchIndices.Count) { ScrollOffset = SearchIndices[actual]; UpdateNotebookDisplay(); }
        }

        private void OnSearchScroll(object sender, MouseEventArgs e)
        {
            if (!NotebookDisplay.Visible || string.IsNullOrEmpty(LastQuery)) return;

            int maxOffset = Math.Max(0, SearchIndices.Count - MaxSearchResults);
            SearchOffset = e.Delta > 0 ? Math.Max(0, SearchOffset - 1) : Math.Min(maxOffset, SearchOffset + 1);
            RefreshResults();
        }
        public void ShowPage(int page)
        {
            ClearQuestCells();
            QuestCellsContainer.Controls.Clear();
            QuestCells.Clear();

            int start = page * QuestsPerPage;
            int end = Math.Min(start + QuestsPerPage, Quests.Count);

            for (int i = start; i < end; i++)
            {
                var quest = Quests[i];
                int col = (i - start) % columns;
                int row = (i - start) / columns;

                var cell = new MirQuestCell(quest)
                {
                    Parent = QuestCellsContainer,
                    Location = new Point(offsetX + col * (cellWidth + horizontalSpacing), offsetY + row * (cellHeight + verticalSpacing)),
                    Visible = true
                };

                cell.Clicked += (s, e) =>
                {
                    foreach (var c in QuestCells)
                        c.Selected = false;

                    var clickedCell = (MirQuestCell)s;
                    clickedCell.Selected = true;

                    GameScene.Scene.QuestDetailDialog.DisplayQuestDetails(clickedCell.Quest);
                    GameScene.Scene.QuestDetailDialog.Show();
                };

                QuestCells.Add(cell);
            }

            QuestCellsContainer.Visible = true;
            PreviousButton.Visible = true;
            NextButton.Visible = true;

            PreviousButton.Enabled = page > 0;
            NextButton.Enabled = end < Quests.Count;

            int totalPages = (int)Math.Ceiling((double)Quests.Count / QuestsPerPage);
            if (totalPages == 0) totalPages = 1;

            PageNumberLabel.Text = $"{page + 1} / {totalPages}";
            PageNumberLabel.Visible = true;
        }

        public void ClearQuestCells()
        {
            foreach (var cell in QuestCells)
                cell.Dispose();
            QuestCells.Clear();
        }

        private void UpdateClassButtonStates()
        {
            foreach (var (rClass, button) in ClassFilterButtons)
            {
                bool selected = rClass == RequiredClass.None
                    ? SelectedClasses.Count == 0
                    : SelectedClasses.Contains(rClass);

                button.Border = selected;
                button.BackColour = selected ? Color.DarkGoldenrod : Color.Transparent;
            }
        }

        private void ResetSearch()
        {
            LastQuery = string.Empty;
            SearchIndices.Clear();
            SearchOffset = 0;
            SearchBox.Text = "Search";
            SearchBox.ForeColour = Color.Gray;

            foreach (var lbl in SearchResults)
                lbl.Visible = false;

            SearchCountLabel.Visible = false;
        }
        #endregion

        public void Process()
        {
            if (!Visible) return;

            totalGold.Text = GameScene.Gold.ToString("###,###,##0");
            totalCredits.Text = GameScene.Credit.ToString("###,###,##0");
        }
    }
}