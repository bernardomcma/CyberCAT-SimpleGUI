using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace CP2077SaveEditor.Utils
{
    /// <summary>
    /// Light/dark theme. Standard controls follow WinForms' color mode; this fixes the colors the designers
    /// hard-code (white backgrounds, black text) and the custom controls.
    /// </summary>
    public static class Theme
    {
        public static bool IsDark { get; private set; }

        public static readonly Color DarkSurface = Color.FromArgb(32, 32, 32);
        public static readonly Color DarkRaised = Color.FromArgb(45, 45, 48);
        public static readonly Color DarkHover = Color.FromArgb(70, 70, 74);
        public static readonly Color DarkText = Color.FromArgb(230, 230, 230);

        // Rows that stand out a little, e.g. equipped items
        public static Color HighlightRow => IsDark ? DarkRaised : Color.FromArgb(248, 248, 248);

        private static readonly ConditionalWeakTable<Form, object> s_themedForms = new();

        /// <summary>
        /// Call before the first window is created. mode: 0 follows Windows, 1 light, 2 dark.
        /// </summary>
        public static void Initialize(int mode)
        {
#pragma warning disable WFO5001 // Dark mode is still marked experimental
            Application.SetColorMode(mode switch
            {
                1 => SystemColorMode.Classic,
                2 => SystemColorMode.Dark,
                _ => SystemColorMode.System
            });
            IsDark = Application.IsDarkModeEnabled;
#pragma warning restore WFO5001

            if (IsDark)
            {
                // Dialogs are created all over the code, theme every new window once
                Application.Idle += (_, _) =>
                {
                    foreach (Form form in Application.OpenForms)
                    {
                        if (!s_themedForms.TryGetValue(form, out _))
                        {
                            s_themedForms.Add(form, null);
                            Apply(form);
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Recolors a control and its children. Does nothing in light mode.
        /// </summary>
        public static void Apply(Control control)
        {
            if (!IsDark)
            {
                return;
            }

            if (control is ModernButton button)
            {
                button.DefaultColor = DarkRaised;
                button.HoverColor = DarkHover;
                button.TextColor = DarkText;
            }
            else
            {
                // A tab page with the visual style background is painted light whatever its BackColor
                if (control is TabPage { UseVisualStyleBackColor: true } tabPage)
                {
                    tabPage.UseVisualStyleBackColor = false;
                    tabPage.BackColor = DarkRaised;
                }

                if (IsLight(control.BackColor))
                {
                    control.BackColor = control is ListView or TextBox or ListBox or TreeView ? DarkSurface : DarkRaised;
                }

                if (control.ForeColor.GetBrightness() < 0.4f)
                {
                    control.ForeColor = DarkText;
                }
            }

            // The dark color mode doesn't cover column headers, which stay light with light text
            if (control is ListView { View: View.Details, OwnerDraw: false } detailsView)
            {
                detailsView.OwnerDraw = true;
                detailsView.GridLines = false; // Bright white in dark mode
                if (detailsView.BorderStyle == BorderStyle.Fixed3D)
                {
                    detailsView.BorderStyle = BorderStyle.FixedSingle;
                }
                detailsView.DrawColumnHeader += DrawDarkColumnHeader;
                detailsView.DrawSubItem += DrawDarkSubItem;

                // The header area right of the last column can't be drawn, so let the last column fill it
                detailsView.ClientSizeChanged += (_, _) => FillLastColumn(detailsView);
                detailsView.ColumnWidthChanged += (_, e) =>
                {
                    if (e.ColumnIndex < detailsView.Columns.Count - 1)
                    {
                        FillLastColumn(detailsView);
                    }
                };
                FillLastColumn(detailsView);
            }

            // Virtual lists create their rows on demand and can't be enumerated
            if (control is ListView { VirtualMode: false } listView)
            {
                foreach (ListViewItem item in listView.Items)
                {
                    if (IsLight(item.BackColor))
                    {
                        item.BackColor = DarkSurface;
                    }
                }
            }

            foreach (Control child in control.Controls)
            {
                Apply(child);
            }
        }

        private static void DrawDarkColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using var background = new SolidBrush(DarkRaised);
            using var border = new Pen(DarkHover);

            e.Graphics.FillRectangle(background, e.Bounds);
            e.Graphics.DrawLine(border, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
            e.Graphics.DrawLine(border, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            var textBounds = Rectangle.Inflate(e.Bounds, -4, 0);
            TextRenderer.DrawText(e.Graphics, e.Header?.Text, e.Font, textBounds, DarkText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static void DrawDarkSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            var listView = (ListView)sender;

            // The default selection of an unfocused list is light gray, unreadable with light text
            if (!e.Item.Selected)
            {
                e.DrawDefault = true;
                return;
            }

            using var background = new SolidBrush(listView.Focused ? SystemColors.Highlight : DarkHover);
            e.Graphics.FillRectangle(background, e.Bounds);

            var textBounds = Rectangle.Inflate(e.Bounds, -3, 0);
            if (listView.CheckBoxes && e.ColumnIndex == 0)
            {
                var state = e.Item.Checked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal;
                var glyph = CheckBoxRenderer.GetGlyphSize(e.Graphics, state);
                var glyphLocation = new Point(e.Bounds.X + 4, e.Bounds.Y + (e.Bounds.Height - glyph.Height) / 2);
                CheckBoxRenderer.DrawCheckBox(e.Graphics, glyphLocation, state);

                textBounds = Rectangle.FromLTRB(glyphLocation.X + glyph.Width + 3, e.Bounds.Top, e.Bounds.Right, e.Bounds.Bottom);
            }
            TextRenderer.DrawText(e.Graphics, e.SubItem?.Text, e.Item.Font, textBounds, DarkText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private static void FillLastColumn(ListView listView)
        {
            if (listView.Columns.Count == 0)
            {
                return;
            }

            var last = listView.Columns[^1];
            var othersWidth = 0;
            for (var i = 0; i < listView.Columns.Count - 1; i++)
            {
                othersWidth += listView.Columns[i].Width;
            }

            // ClientSize already excludes the vertical scrollbar; one pixel less avoids a horizontal one
            var width = listView.ClientSize.Width - othersWidth - 1;
            if (width > 40 && last.Width != width)
            {
                last.Width = width;
            }
        }

        private static bool IsLight(Color color) => color.A > 0 && color.GetBrightness() > 0.85f;
    }
}
