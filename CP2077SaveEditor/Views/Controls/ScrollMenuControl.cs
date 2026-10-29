using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CP2077SaveEditor.Views.Controls
{
    public partial class ScrollMenuControl : UserControl
    {
        private const int StepHeight = 44;
        private readonly List<ModernButton> _buttons = new();

        public ScrollMenuControl()
        {
            InitializeComponent();
        }

        public ModernButton AddButton(string text, string icon, EventHandler eventHandler)
        {
            var button = new ModernButton()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ClickEffectEnabled = true,
                DefaultColor = Color.White,
                HoverColor = Color.DarkGray,
                Location = new Point(-1, _buttons.Count * StepHeight - 1),
                Size = new Size(pnl_Menu.Width + 1, StepHeight + 1),
                Text = text,
                IconGlyph = icon,
                TextColor = Color.Black,
                TextFont = new Font("Segoe UI", 10.25F, FontStyle.Regular, GraphicsUnit.Point),
            };
            button.Click += eventHandler;
            button.MouseWheel += pnl_Menu_MouseWheel;

            _buttons.Add(button);
            pnl_Menu.Controls.Add(button);
            return button;
        }

        public void SelectButton(ModernButton selectedButton)
        {
            foreach (var button in _buttons)
            {
                button.IsSelected = button == selectedButton;
            }

            if (selectedButton != null)
            {
                while (selectedButton.Top < -1 && pnl_Menu.Controls[0].Top < -1)
                {
                    ScrollBy(StepHeight);
                }

                while (selectedButton.Bottom > pnl_Menu.Height + 1 && pnl_Menu.Controls[^1].Bottom > pnl_Menu.Height)
                {
                    ScrollBy(-StepHeight);
                }
            }
        }

        private void ScrollBy(int offset)
        {
            if (pnl_Menu.Controls.Count == 0)
            {
                return;
            }

            if (offset > 0 && pnl_Menu.Controls[0].Location.Y + offset > -1)
            {
                return;
            }

            if (offset < 0 && pnl_Menu.Controls[^1].Location.Y + StepHeight <= pnl_Menu.Height)
            {
                return;
            }

            foreach (Control control in pnl_Menu.Controls)
            {
                control.Location = new Point(control.Location.X, control.Location.Y + offset);
            }
        }

        private void btn_ScrollUp_Click(object sender, EventArgs e)
        {
            ScrollBy(StepHeight);
        }

        private void pnl_Menu_MouseWheel(object sender, MouseEventArgs eventArgs)
        {
            if (eventArgs.Delta > 0)
            {
                ScrollBy(StepHeight);
            }

            if (eventArgs.Delta < 0)
            {
                ScrollBy(-StepHeight);
            }
        }

        private void btn_ScrollDown_Click(object sender, EventArgs e)
        {
            ScrollBy(-StepHeight);
        }
    }
}
