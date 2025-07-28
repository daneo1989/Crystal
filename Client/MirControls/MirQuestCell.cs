using Client.MirControls;
using Client.MirGraphics;
using Client.MirScenes;
using Client.MirSounds;
using System;
using System.Drawing; // for TextFormatFlags
using System.Windows.Forms;
using SystemDrawingColor = System.Drawing.Color;
using SystemDrawingFont = System.Drawing.Font;
using SystemDrawingPoint = System.Drawing.Point;
using SystemDrawingSize = System.Drawing.Size;

namespace Client.MirControls
{
    public sealed class MirQuestCell : MirImageControl
    {
        public MirLabel NameLabel, StatusLabel, GoldLabel, ExpLabel, CompletionPercentLabel, ShowLabel;
        public ClientQuestProgress Quest;
        public bool Selected;

        public event EventHandler Clicked;

        public MirImageControl QuestTypeIcon;

        public MirCheckBox ShowCheckBox;

        public MirQuestCell(ClientQuestProgress quest)
        {
            Quest = quest;
            Size = new SystemDrawingSize(125, 146);
            Index = 750; // Background index for cell
            Library = Libraries.Title;

            QuestTypeIcon = new MirImageControl
            {
                Parent = this,
                Location = new SystemDrawingPoint(15, 40),
                Size = new SystemDrawingSize(32, 32),
                Library = Libraries.Prguse,
                Index = GetQuestTypeIconIndex(quest),
                NotControl = true,
            };

            NameLabel = new MirLabel
            {
                Parent = this,
                Location = new SystemDrawingPoint(0, 13),
                Size = new SystemDrawingSize(125, 18),
                Font = new SystemDrawingFont("Segoe UI Semibold", 9F),
                ForeColour = SystemDrawingColor.FromArgb(240, 240, 240), // soft off-white
                Text = quest.QuestInfo.Name,
                DrawFormat = TextFormatFlags.HorizontalCenter,
                NotControl = true,
            };

            StatusLabel = new MirLabel
            {
                Parent = this,
                Location = new SystemDrawingPoint(53, 37),
                Size = new SystemDrawingSize(60, 20),
                Font = new SystemDrawingFont("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                ForeColour = quest.Completed
                    ? SystemDrawingColor.FromArgb(139, 195, 74)   // pastel green
                    : SystemDrawingColor.FromArgb(255, 193, 7),  // warm amber
                Text = quest.Completed ? "Completed" : "Incomplete",
                DrawFormat = TextFormatFlags.HorizontalCenter,
                NotControl = true,
            };

            GoldLabel = new MirLabel
            {
                Parent = this,
                Location = new SystemDrawingPoint(2, 102),
                Size = new SystemDrawingSize(95, 20),
                Font = new SystemDrawingFont("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                ForeColour = SystemDrawingColor.FromArgb(255, 215, 0), // classic gold
                Text = $"{quest.QuestInfo.RewardGold} Gold",
                DrawFormat = TextFormatFlags.RightToLeft | TextFormatFlags.Right,
                NotControl = true,
            };

            ExpLabel = new MirLabel
            {
                Parent = this,
                Location = new SystemDrawingPoint(2, 81),
                Size = new SystemDrawingSize(95, 20),
                Font = new SystemDrawingFont("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                ForeColour = SystemDrawingColor.FromArgb(64, 196, 255), // bright cyan
                Text = $"{quest.QuestInfo.RewardExp} EXP",
                DrawFormat = TextFormatFlags.RightToLeft | TextFormatFlags.Right,
                NotControl = true,
            };

            CompletionPercentLabel = new MirLabel
            {
                Parent = this,
                Location = new SystemDrawingPoint(20, 56),
                Size = new SystemDrawingSize(110, 20),
                Font = new SystemDrawingFont("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                ForeColour = SystemDrawingColor.White,
                Text = GetCompletionText(quest),
                DrawFormat = TextFormatFlags.HorizontalCenter,
                NotControl = true,
            };

            Click += (s, e) => Clicked?.Invoke(this, EventArgs.Empty);

            ShowLabel = new MirLabel
            {
                Parent = this,
                Location = new SystemDrawingPoint(10, 122),
                Size = new SystemDrawingSize(70, 15),
                Font = new SystemDrawingFont("Segoe UI", 8F),
                ForeColour = SystemDrawingColor.White,
                Text = "Track Q:",
                NotControl = true
            };

            ShowCheckBox = new MirCheckBox
            {
                Location = new SystemDrawingPoint(60, 125),
                Size = new SystemDrawingSize(12, 11),    
                Parent = this,
                Sound = SoundList.ButtonA,
                Library = Libraries.Prguse,
                TickedIndex = 2087,       
                Border = true,
                BorderColour = SystemDrawingColor.FromArgb(64, 64, 64),
                Checked = GameScene.Scene.QuestTrackingDialog.TrackedQuestsIds.Contains(quest.Id),
            };
            ShowCheckBox.Click += (o, e) =>
            {
                if (quest == null) return;

                if (ShowCheckBox.Checked)
                    GameScene.Scene.QuestTrackingDialog.AddQuest(quest);
                else
                    GameScene.Scene.QuestTrackingDialog.RemoveQuest(quest);
            };

        }

        private string GetCompletionText(ClientQuestProgress quest)
        {
            return quest.Completed ? "C: 100%" : "C: 0%";
        }

        public override void Draw()
        {
            base.Draw();

            if (Selected)
                Libraries.Prguse.Draw(193, DisplayLocation.X, DisplayLocation.Y); // Highlight overlay
        }

        private int GetQuestTypeIconIndex(ClientQuestProgress quest)
        {
            if (quest.Completed)
            {
                return quest.QuestInfo.Type switch
                {
                    QuestType.General => 988,    // Completed General icon
                    QuestType.Daily => 994,      // Completed Daily icon
                    QuestType.Repeatable => 988, // Completed Repeatable icon
                    QuestType.Story => 1088,     // Completed Story icon
                    _ => 988,
                };
            }
            else
            {
                return quest.QuestInfo.Type switch
                {
                    QuestType.General => 985,
                    QuestType.Daily => 991,
                    QuestType.Repeatable => 985,
                    QuestType.Story => 1085,
                    _ => 986,
                };
            }
        }
    }
}