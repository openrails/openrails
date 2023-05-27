// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D.Popups;
using Orts.Viewer3D.RollingStock.Subsystems.ETCS;
using ORTS.Scripting.Api.ETCS;
using static Orts.Viewer3D.RollingStock.Subsystems.ETCS.DriverMachineInterface;

namespace Orts.Viewer3D.RollingStock.SubSystems.ETCS
{
    public class Keyboard
    {
        public readonly IList<DMIButton> Keys = new List<DMIButton>();
        public readonly DMIButton MoreKey;
        public int CurrentKeyPage { get; private set; }
        public Keyboard(DataEntryField field)
        {
            MoreKey = new DMIIconButton("NA_23.bmp", "NA_23.bmp", "More", false, () =>
            {
                CurrentKeyPage++;
                if (CurrentKeyPage * 11 >= Keys.Count) CurrentKeyPage = 0;
                field.DataEntryWindow.PrepareLayout();
            }, 102, 50, field.DMI);
            MoreKey.Enabled = true;
        }
    }
    public class AlphanumericKeyboard : Keyboard
    {
        public class AlphanumericButton : DMIButton
        {
            string Number;
            string Letters;
            TextPrimitive NumberText;
            TextPrimitive LettersText;
            readonly int FontHeightNumber = 16;
            readonly int FontHeightLetters = 10;
            public AlphanumericButton(string num, string letters, DataEntryField field) : base(num + " " + letters, false, () => field.HandleKeyPress(num + letters), 102, 50, field.DMI, true)
            {
                Number = num + " ";
                Letters = letters;
                SetText();
            }
            public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
            {
                base.Draw(spriteBatch, drawPosition);
                {
                    int x = drawPosition.X + (int)Math.Round(NumberText.Position.X * Scale);
                    int y = drawPosition.Y + (int)Math.Round(NumberText.Position.Y * Scale);
                    NumberText.Draw(spriteBatch, new Point(x, y));
                }
                {
                    int x = drawPosition.X + (int)Math.Round(LettersText.Position.X * Scale);
                    int y = drawPosition.Y + (int)Math.Round(LettersText.Position.Y * Scale);
                    LettersText.Draw(spriteBatch, new Point(x, y));
                }
            }
            public override void ScaleChanged()
            {
                base.ScaleChanged();
                SetText();
            }
            void SetText()
            {
                var f1 = GetFont(FontHeightNumber);
                var f2 = GetFont(FontHeightLetters);
                var size1 = (int)(f1.MeasureString(Number) / Scale);
                var size2 = (int)(f2.MeasureString(Letters) / Scale);
                NumberText = new TextPrimitive(new Point((Width - size1 - size2) / 2, (Height - FontHeightNumber) / 2), ColorGrey, Number, f1);
                LettersText = new TextPrimitive(new Point((Width - size1 - size2) / 2 + size1, (Height + FontHeightNumber) / 2 - FontHeightLetters), ColorGrey, Letters, f2);
            }
        }
        public AlphanumericKeyboard(DataEntryField field) : base(field)
        {
            Keys.Add(new DMITextButton("1", "1", false, () => field.HandleKeyPress("1"), 102, 50, field.DMI, 16));
            Keys.Add(new AlphanumericButton("2", "abc", field));
            Keys.Add(new AlphanumericButton("3", "def", field));
            Keys.Add(new AlphanumericButton("4", "ghi", field));
            Keys.Add(new AlphanumericButton("5", "jkl", field));
            Keys.Add(new AlphanumericButton("6", "mno", field));
            Keys.Add(new AlphanumericButton("7", "pqrs", field));
            Keys.Add(new AlphanumericButton("8", "tuv", field));
            Keys.Add(new AlphanumericButton("9", "wxyz", field));
            Keys.Add(new DMIIconButton("NA_21.bmp", "NA_21.bmp", "Delete", false, () => field.HandleKeyPress("DEL"), 102, 50, field.DMI));
            Keys.Add(new DMITextButton("0", "0", false, () => field.HandleKeyPress("0"), 102, 50, field.DMI, 16));
            foreach (var key in Keys)
            {
                key.Enabled = true;
            }
            var dotkey = new DMITextButton(".", ".", false, () => field.HandleKeyPress("."), 102, 50, field.DMI);
            Keys.Add(dotkey);
        }
    }
    public class NumericKeyboard : Keyboard
    {
        public NumericKeyboard(DataEntryField field, bool enhanced = false) : base(field)
        {
            foreach (int i in Enumerable.Range(1, 10 - 1))
            {
                string digitname = i.ToString();
                Keys.Add(new DMITextButton(digitname, digitname, false, () => field.HandleKeyPress(digitname), 102, 50, field.DMI, 16));
            }
            Keys.Add(new DMIIconButton("NA_21.bmp", "NA_21.bmp", "Delete", false, () => field.HandleKeyPress("DEL"), 102, 50, field.DMI));
            Keys.Add(new DMITextButton("0", "0", false, () => field.HandleKeyPress("0"), 102, 50, field.DMI, 16));
            foreach (var key in Keys)
            {
                key.Enabled = true;
            }
            var dotkey = new DMITextButton(".", ".", false, () => field.HandleKeyPress("."), 102, 50, field.DMI, 16);
            dotkey.Enabled = enhanced;
            Keys.Add(dotkey);
        }
    }
    public class DedicateKeyboard : Keyboard
    {
        public DedicateKeyboard(List<string> keyLabels, DataEntryField field) : base(field)
        {
            int i = 0;
            foreach (string label in keyLabels)
            {
                if (label == null)
                {
                    Keys.Add(null);
                    i++;
                    continue;
                }
                DMIButton but = new DMITextButton(label, label, false, () => field.HandleKeyPress(label), 102, 50, field.DMI);
                but.Enabled = true;
                Keys.Add(but);
                i++;
            }
        }
    }
    public class DataEntryField
    {
        public readonly DriverMachineInterface DMI;
        public readonly DataEntryWindow DataEntryWindow;
        public Keyboard Keyboard;
        public string AcceptedValue { get; private set; }
        string KeyboardValue;
        string CurrentValue;
        string PreviousValue;
        public bool Selected { get; private set; }
        public bool Accepted { get; private set; }
        public bool TechnicalRangeInvalid;
        public bool TechnicalResolutionInvalid;
        public bool OperationalRangeInvalid;
        public bool TechnicalCrossCheckInvalid;
        public bool OperationalCrossCheckInvalid;

        public readonly FieldLabelArea LabelArea;
        public readonly FieldDataArea DataArea;
        public DMIArea.TextPrimitive LabelEchoText;
        public DMIArea.TextPrimitive DataEchoText;
        int Index;
        public readonly string Name;
        DMIDataEntryValue Field;
        int CursorIndex;
        float LastKeypress;
        public class FieldLabelArea : DMIArea
        {
            TextPrimitive Label;
            readonly int FontHeightDataField = 12;
            string Name;
            public FieldLabelArea(string name, DriverMachineInterface dmi) : base(dmi, 204, 50)
            {
                Name = name;
                BackgroundColor = ColorDarkGrey;
                SetText();
            }
            public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
            {
                base.Draw(spriteBatch, drawPosition);

                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, ColorMediumGrey);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, ColorMediumGrey);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, ColorMediumGrey);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, ColorMediumGrey);

                int x = drawPosition.X + (int)Math.Round(Label.Position.X * Scale);
                int y = drawPosition.Y + (int)Math.Round(Label.Position.Y * Scale);
                Label.Draw(spriteBatch, new Point(x, y));
            }
            public override void ScaleChanged()
            {
                base.ScaleChanged();
                SetText();
            }
            void SetText()
            {
                var font = GetFont(FontHeightDataField);
                var size = (int)(font.MeasureString(Name) / Scale);
                Label = new TextPrimitive(new Point(Width - 10 - size, (Height - FontHeightDataField) / 2), ColorGrey, Name, font);
            }
        }
        public class FieldDataArea : DMIButton
        {
            TextPrimitive Data;
            readonly int FontHeightDataField = 12;
            public bool CursorVisible;
            Rectangle Cursor;
            int CursorIndex;
            public FieldDataArea(string name, bool hasLabel, DriverMachineInterface dmi) : base(name, true, null, hasLabel ? 102 : 306, 50, dmi)
            {
                Data = new TextPrimitive(new Point(10, (Height - FontHeightDataField) / 2), Color.Transparent, "", null);
                Enabled = true;
                SetFont();
            }
            public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
            {
                base.Draw(spriteBatch, drawPosition);

                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, ColorMediumGrey);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, ColorMediumGrey);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, ColorMediumGrey);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, ColorMediumGrey);

                if (CursorVisible && DMI.Blinker2Hz)
                {
                    DrawIntRectangle(spriteBatch, drawPosition, Cursor.X, Cursor.Y, Cursor.Width, Cursor.Height, Color.Black);
                }

                int x = drawPosition.X + (int)Math.Round(Data.Position.X * Scale);
                int y = drawPosition.Y + (int)Math.Round(Data.Position.Y * Scale);
                Data.Draw(spriteBatch, new Point(x, y));
            }
            public override void ScaleChanged()
            {
                base.ScaleChanged();
                SetFont();
                UpdateCursor(CursorIndex);
            }
            public void SetText(string text, Color color)
            {
                Data.Text = text ?? "";
                Data.Color = color;
            }
            public void UpdateCursor(int cursorIndex)
            {
                CursorIndex = cursorIndex;
                float length;
                float charlength;
                if (cursorIndex >= Data.Text.Length)
                {
                    length = Data.Font.MeasureString(Data.Text) / Scale;
                    charlength = Data.Font.MeasureString("8") / Scale;
                }
                else
                {
                    length = Data.Font.MeasureString(Data.Text.Substring(0, cursorIndex)) / Scale;
                    charlength = Data.Font.MeasureString(Data.Text[cursorIndex].ToString()) / Scale;
                }
                Cursor = new Rectangle(10 + (int)length, (Height + FontHeightDataField) / 2 + 3, (int)charlength, 1);
            }
            void SetFont()
            {
                Data.Font = GetFont(FontHeightDataField);
            }
        }
        public DataEntryField(DMIDataEntryValue field, int index, DataEntryWindow window, bool singleField)
        {
            Name = field.Name;
            Field = field;
            AcceptedValue = PreviousValue = field.Value ?? "";
            Index = index;
            DataEntryWindow = window;
            DMI = DataEntryWindow.DMI;

            DataArea = new FieldDataArea(Name, !singleField, DMI);
            if (!singleField)
            {
                LabelArea = new FieldLabelArea(Name, DMI);
                LabelEchoText = new DMIArea.TextPrimitive(new Point((int)(DataEntryWindow.LabelFont.MeasureString(Name) / DMI.Scale), 0), Color.White, Name, DataEntryWindow.LabelFont);
            }

            if (field.Keyboard.Type == DMIKeyboard.KeyboardType.Dedicate) Keyboard = new DedicateKeyboard(field.Keyboard.KeyLabels, this);
            else if (field.Keyboard.Type == DMIKeyboard.KeyboardType.Numeric) Keyboard = new NumericKeyboard(this);
            else if (field.Keyboard.Type == DMIKeyboard.KeyboardType.Alphanumeric) Keyboard = new AlphanumericKeyboard(this);

            DataArea.PressedAction = () => DataEntryWindow.FieldSelected(Index);
            DataArea.ShowButtonBorder = false;

            UpdateText();
        }
        public void SetSelected(bool val)
        {
            DataArea.CursorVisible = val;
            if (Selected != val)
            {
                Selected = val;
                CurrentValue = PreviousValue;
                if (Selected) KeyboardValue = "";
            }
            UpdateText();
        }
        public void SetAccepted(bool val)
        {
            if (Accepted != val)
            {
                Accepted = val;
                if (Accepted) AcceptedValue = CurrentValue;
                else AcceptedValue = "";
                PreviousValue = AcceptedValue;
                if (!Selected) CurrentValue = PreviousValue;
                if (Accepted)
                {
                    //TechnicalRangeInvalid = !DataEntryWindow.Definition.TechnicalRangeCheck[Name].IsValid(AcceptedValue);
                    //TechnicalResolutionInvalid = !DataEntryWindow.Definition.TechnicalResolutionCheck[Name].IsValid(AcceptedValue);
                    //if (!TechnicalRangeInvalid && !TechnicalResolutionInvalid) OperationalRangeInvalid = !DataEntryWindow.Definition.OperationalRangeCheck[Name].IsValid(AcceptedValue);
                }
                else
                {
                    TechnicalResolutionInvalid = TechnicalRangeInvalid = OperationalRangeInvalid = false;
                    if (DataEntryWindow.Fields.Count > 1) DataEntryWindow.YesButton.Enabled = false;
                }
                DataEntryWindow.UnlockNavigationButtons();
            }
            UpdateText();
        }
        public void UpdateCursor()
        {
            if (LastKeypress + 2 < DMI.CurrentTime)
            {
                int cur = KeyboardValue.Length;
                if (CursorIndex != cur)
                {
                    CursorIndex = cur;
                    DataArea.UpdateCursor(CursorIndex);
                }
            }
        }
        void UpdateText()
        {
            DataArea.BackgroundColor = Selected ? ColorMediumGrey : ColorDarkGrey;
            DataArea.SetText(CurrentValue, Selected ? Color.Black : (Accepted ? Color.White : ColorGrey));
            DataArea.UpdateCursor(CursorIndex);
            if (LabelEchoText != null)
            {
                string text;
                Color color;
                if (TechnicalRangeInvalid || TechnicalResolutionInvalid)
                {
                    text = "++++";
                    color = ColorRed;
                }
                else if (TechnicalCrossCheckInvalid)
                {
                    text = "????";
                    color = ColorRed;
                }
                else if (OperationalRangeInvalid)
                {
                    text = "++++";
                    color = ColorYellow;
                }
                else if (OperationalCrossCheckInvalid)
                {
                    text = "????";
                    color = ColorYellow;
                }
                else
                {
                    text = AcceptedValue;
                    color = Accepted ? Color.White : ColorGrey;
                }
                DataEchoText = new DMIArea.TextPrimitive(new Point(0, 0), color, text, DataEntryWindow.LabelFont);
            }
        }
        public void ScaleChanged()
        {
            foreach (DMIButton key in Keyboard.Keys)
                key.ScaleChanged();
            Keyboard.MoreKey.ScaleChanged();
            DataArea.ScaleChanged();
            if (LabelArea != null)
            {
                LabelArea.ScaleChanged();
                DataEchoText.Font = DataEntryWindow.LabelFont;
                LabelEchoText = new DMIArea.TextPrimitive(new Point((int)(DataEntryWindow.LabelFont.MeasureString(Name) / DMI.Scale), 0), Color.White, Name, DataEntryWindow.LabelFont);
            }
        }
        public void HandleKeyPress(string keyname)
        {
            LastKeypress = DMI.CurrentTime;
            if (Keyboard is DedicateKeyboard)
            {
                KeyboardValue = keyname;
                CursorIndex = KeyboardValue.Length;
            }
            else if (Keyboard is NumericKeyboard)
            {
                if (keyname == "DEL") KeyboardValue = KeyboardValue.Length > 0 ? KeyboardValue.Substring(0, KeyboardValue.Length - 1) : "";
                else KeyboardValue += keyname;
                CursorIndex = KeyboardValue.Length;
            }
            else if (Keyboard is AlphanumericKeyboard)
            {
                if (keyname == "DEL")
                {
                    KeyboardValue = KeyboardValue.Length > 0 ? KeyboardValue.Substring(0, KeyboardValue.Length - 1) : "";
                    CursorIndex = KeyboardValue.Length;
                }
                else
                {
                    if (CursorIndex >= KeyboardValue.Length || !keyname.Contains(KeyboardValue[KeyboardValue.Length - 1]))
                    {
                        KeyboardValue += keyname[0];
                        CursorIndex = keyname.Length == 1 ? KeyboardValue.Length : KeyboardValue.Length - 1;
                    }
                    else
                    {
                        char c = KeyboardValue[KeyboardValue.Length - 1];
                        KeyboardValue = KeyboardValue.Substring(0, KeyboardValue.Length - 1);
                        KeyboardValue += keyname[(keyname.IndexOf(c) + 1) % keyname.Length];
                    }
                }
            }
            CurrentValue = KeyboardValue;
            PreviousValue = "";
            SetAccepted(false);
        }
    }
    public class DataEntryWindow : DMISubwindow
    {
        string Title;
        int NumPages => (Fields.Count - 1) / 4 + 1;
        int CurrentPage => ActiveField / 4;
        public int ActiveField;
        public readonly IList<DataEntryField> Fields = new List<DataEntryField>();
        DMIButton[] KeyboardButtons = new DMIButton[12];
        DMIButton NextButton;
        DMIButton PrevButton;
        public readonly DMIYesButton YesButton;
        DMITextLabel DataEntryCompleteLabel;
        public readonly DMIDataEntryDefinition Definition;
        public WindowTextFont LabelFont { get; private set; }
        readonly int FontHeightLabel = 12;
        public class DMIYesButton : DMIButton
        {
            TextPrimitive Yes;
            readonly int FontHeightYes = 12;
            public DMIYesButton(DriverMachineInterface dmi) : base(Viewer.Catalog.GetString("Yes"), true, null, 330, 40, dmi, false)
            {
                SetText();
            }
            public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
            {
                DrawRectangle(spriteBatch, drawPosition, 0, 0, Width, Height, Enabled ? ColorGrey : ColorDarkGrey);

                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, 1, Height, ColorMediumGrey);
                DrawIntRectangle(spriteBatch, drawPosition, Width - 1, 0, 1, Height, ColorMediumGrey);
                DrawIntRectangle(spriteBatch, drawPosition, 0, 0, Width, 1, ColorMediumGrey);
                DrawIntRectangle(spriteBatch, drawPosition, 0, Height - 1, Width, 1, ColorMediumGrey);

                int x = drawPosition.X + (int)Math.Round(Yes.Position.X * Scale);
                int y = drawPosition.Y + (int)Math.Round(Yes.Position.Y * Scale);
                Yes.Draw(spriteBatch, new Point(x, y));
            }
            public override void ScaleChanged()
            {
                base.ScaleChanged();
                SetText();
            }
            void SetText()
            {
                var font = GetFont(FontHeightYes);
                string text = Viewer.Catalog.GetString("Yes");
                Yes = new TextPrimitive(new Point((Width - (int)(font.MeasureString(text) / Scale)) / 2, (Height - FontHeightYes) / 2), Color.Black, text, font);
            }
        }
        public DataEntryWindow(DMIDataEntryDefinition definition, DriverMachineInterface dmi) : base(definition.WindowTitle, definition.FullScreen || definition.Fields.Count > 1, dmi)
        {
            Definition = definition;
            Title = definition.WindowTitle;
            int i = 0;
            LabelFont = GetFont(FontHeightLabel);
            foreach (var field in Definition.Fields)
            {
                Fields.Add(new DataEntryField(field, i, this, !FullScreen));
                i++;
            }
            if (FullScreen)
            {
                NextButton = new DMIIconButton("NA_17.bmp", "NA_18.2.bmp", Viewer.Catalog.GetString("Next"), true, () =>
                {
                    if ((ActiveField / 4) < (Fields.Count / 4))
                    {
                        NextButton.Enabled = false;
                        PrevButton.Enabled = true;
                        ActiveField = 4 * (ActiveField / 4 + 1);
                        PrepareLayout();
                    }
                }, 82, 50, dmi);
                PrevButton = new DMIIconButton("NA_18.bmp", "NA_19.bmp", Viewer.Catalog.GetString("Next"), true, () =>
                {
                    if (ActiveField > 3)
                    {
                        NextButton.Enabled = true;
                        PrevButton.Enabled = false;
                        ActiveField = 4 * (ActiveField / 4 - 1);
                        PrepareLayout();
                    }
                }, 82, 50, dmi);
                DataEntryCompleteLabel = new DMITextLabel(Title + " data entry complete?", 334, 50, dmi);
                YesButton = new DMIYesButton(dmi);
                YesButton.PressedAction = () =>
                {
                    bool overrideOperational = YesButton.DelayType;
                    YesButton.DelayType = false;
                    Dictionary<string, string> values = new Dictionary<string, string>();
                    foreach (var field in Fields)
                    {
                        values[field.Name] = field.AcceptedValue;
                    }
                    bool checkPassed = true;
                    foreach (var check in Definition.TechnicalCrossChecks)
                    {
                        var conflict = check.GetConflictingVariables(values);
                        foreach (var name in conflict)
                        {
                            foreach (var field in Fields)
                            {
                                if (field.Name == name)
                                {
                                    checkPassed = false;
                                    field.TechnicalCrossCheckInvalid = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (!checkPassed)
                    {
                        return;
                    }
                    if (overrideOperational) foreach (var check in Definition.OperationalCrossChecks)
                        {
                            var conflict = check.GetConflictingVariables(values);
                            foreach (var name in conflict)
                            {
                                foreach (var field in Fields)
                                {
                                    if (field.Name == name)
                                    {
                                        checkPassed = false;
                                        field.OperationalCrossCheckInvalid = true;
                                        break;
                                    }
                                }
                            }
                        }
                    if (!checkPassed)
                    {
                        YesButton.DelayType = true;
                        return;
                    }
                    string result = WindowTitle + "\n";
                    foreach (var field in Fields)
                    {
                        result += field.Name + "=" + field.AcceptedValue + "\n";
                    }
                    //if (DMI.ETCSStatus != null) DMI.ETCSStatus.DriverActionResult = result;
                    DMI.ExitWindow(this);
                };
                YesButton.ExtendedSensitiveArea = new Rectangle(0, 50, 0, 0);
                PrevButton.Enabled = false;
                NextButton.Enabled = false;
            }
            if (Fields.Count > 4)
            {
                NextButton.Enabled = true;
            }
            PrepareLayout();
            Visible = true;
        }
        public override void PrepareFrame(ETCSStatus status)
        {
            base.PrepareFrame(status);
            Fields[ActiveField].UpdateCursor();
        }
        public override void Draw(SpriteBatch spriteBatch, Point drawPosition)
        {
            base.Draw(spriteBatch, drawPosition);
            if (!FullScreen) return;
            foreach (int i in Enumerable.Range(0, Fields.Count))
            {
                {
                    var text = Fields[i].LabelEchoText;
                    int x = drawPosition.X + (int)Math.Round((204 - text.Position.X - 5) * Scale);
                    int y = drawPosition.Y + (int)Math.Round((100 + 2 * i * FontHeightLabel) * Scale);
                    text.Draw(spriteBatch, new Point(x, y));
                }
                {
                    var text = Fields[i].DataEchoText;
                    int x = drawPosition.X + (int)Math.Round((204 + 5) * Scale);
                    int y = drawPosition.Y + (int)Math.Round((100 + 2 * i * FontHeightLabel) * Scale);
                    text.Draw(spriteBatch, new Point(x, y));
                }
            }
        }
        public void PrepareLayout()
        {
            List<DMIArea> areas = new List<DMIArea>
            {
                CloseButton
            };
            foreach (int i in Enumerable.Range(0, Fields.Count))
            {
                if (i != ActiveField) Fields[i].SetSelected(false);
            }
            Fields[ActiveField].SetSelected(true);
            if (!FullScreen)
            {
                Fields[0].DataArea.Position = new Point(0, 65);
                areas.Add(Fields[0].DataArea);
            }
            else
            {
                if (Fields.Count > 4)
                {
                    SetTitle(Title + " (" + (CurrentPage + 1) + "/" + NumPages + ")");
                    PrevButton.Position = new Point(416, 400);
                    NextButton.Position = new Point(498, 400);
                    areas.Add(NextButton);
                    areas.Add(PrevButton);
                }
                DataEntryCompleteLabel.Position = new Point(0, 330);
                YesButton.Position = new Point(0, 330 + DataEntryCompleteLabel.Height);
                areas.Add(DataEntryCompleteLabel);
                areas.Add(YesButton);
                for (int i = 4 * (ActiveField / 4); i < Fields.Count && i < (4 * (ActiveField / 4 + 1)); i++)
                {
                    Fields[i].LabelArea.Position = new Point(334, (i % 4) * 50);
                    Fields[i].DataArea.Position = new Point(334 + Fields[i].LabelArea.Width, (i % 4) * 50);
                    areas.Add(Fields[i].LabelArea);
                    areas.Add(Fields[i].DataArea);
                }
            }
            for (int i = 0; i < 12; i++)
            {
                KeyboardButtons[i] = null;
            }
            DataEntryField field = Fields[ActiveField];
            var keyboard = field.Keyboard;
            for (int i = 0; i < 12; i++)
            {
                if (i == 11 && keyboard.Keys.Count > 12)
                {
                    KeyboardButtons[11] = keyboard.MoreKey;
                    break;
                }
                else if (i + keyboard.CurrentKeyPage * 11 < keyboard.Keys.Count) KeyboardButtons[i] = field.Keyboard.Keys[i + keyboard.CurrentKeyPage * 11];
            }
            for (int i = 0; i < KeyboardButtons.Length; i++)
            {
                if (KeyboardButtons[i] == null) continue;
                KeyboardButtons[i].Position = new Point((FullScreen ? 334 : 0) + i % 3 * 102, 200 + 50 * (i / 3));
                areas.Add(KeyboardButtons[i]);
            }
            SubAreas = areas;
        }
        public void FieldSelected(int index)
        {
            if (!FullScreen)
            {
                var field = Fields[index];
                bool overrideOperational = field.DataArea.DelayType;
                field.SetAccepted(true);
                if (field.TechnicalRangeInvalid || field.TechnicalResolutionInvalid || (field.OperationalRangeInvalid && !overrideOperational))
                {
                    if (field.OperationalRangeInvalid) field.DataArea.DelayType = true;
                    return;
                }
                string result = WindowTitle + "\n";
                result += field.Name + "=" + field.AcceptedValue + "\n";
                //if (DMI.ETCSStatus != null) DMI.ETCSStatus.DriverActionResult = result;
                DMI.ExitWindow(this);
            }
            else if (ActiveField == index)
            {
                var field = Fields[index];
                bool overrideOperational = field.DataArea.DelayType;
                field.SetAccepted(true);
                if (field.TechnicalRangeInvalid || field.TechnicalResolutionInvalid || (field.OperationalRangeInvalid && !overrideOperational))
                {
                    if (field.OperationalRangeInvalid) Fields[index].DataArea.DelayType = true;
                    for (int i = 0; i < Fields.Count; i++)
                    {
                        if (i != index) Fields[i].DataArea.Enabled = false;
                    }
                    PrevButton.Enabled = false;
                    NextButton.Enabled = false;
                    return;
                }
                if (index + 1 < Fields.Count) ActiveField++;
                else ActiveField = 0;
                NextButton.Enabled = CurrentPage + 1 < NumPages;
                PrevButton.Enabled = CurrentPage > 0;
                bool allaccepted = true;
                foreach (var f in Fields)
                {
                    if (!f.Accepted)
                    {
                        allaccepted = false;
                        break;
                    }
                }
                if (allaccepted)
                    YesButton.Enabled = true;
                else
                    YesButton.Enabled = false;
                PrepareLayout();
            }
            else
            {
                ActiveField = index;
                PrepareLayout();
            }
        }
        public override void ScaleChanged()
        {
            base.ScaleChanged();
            LabelFont = GetFont(FontHeightLabel);
            foreach (DataEntryField field in Fields)
                field.ScaleChanged();
        }
        public void UnlockNavigationButtons()
        {
            foreach (DataEntryField field in Fields)
            {
                field.DataArea.Enabled = true;
                field.DataArea.DelayType = false;
            }
            if (FullScreen)
            {
                PrevButton.Enabled = CurrentPage > 0;
                NextButton.Enabled = CurrentPage + 1 < NumPages;
                YesButton.DelayType = false;
            }
        }
    }
}
