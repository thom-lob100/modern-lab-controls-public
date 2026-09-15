using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Modern.Lab.Hosting;

namespace Modern.Lab.MasterData
{
    public partial class MasterDataMenuForm : ModernFormBase
    {
        private readonly Dictionary<string, Form> openScreens = new Dictionary<string, Form>(StringComparer.Ordinal);

        public MasterDataMenuForm()
        {
            this.InitializeComponent();
            this.InitializeModernForm();
        }

        public static Form Create(string screen)
        {
            if (string.Equals(screen, "areabay", StringComparison.OrdinalIgnoreCase))
            {
                return new AreaBayForm();
            }

            if (string.Equals(screen, "flow", StringComparison.OrdinalIgnoreCase))
            {
                return new FlowForm();
            }

            if (string.Equals(screen, "oper", StringComparison.OrdinalIgnoreCase))
            {
                return new OperForm();
            }

            if (string.Equals(screen, "product", StringComparison.OrdinalIgnoreCase))
            {
                return new ProductForm();
            }

            if (string.Equals(screen, "pfo", StringComparison.OrdinalIgnoreCase))
            {
                return new PfoNodeForm();
            }

            return null;
        }

        private void OnProductClick(object sender, EventArgs e)
        {
            this.OpenScreen("product");
        }

        private void OnAreaBayClick(object sender, EventArgs e)
        {
            this.OpenScreen("areabay");
        }

        private void OnFlowClick(object sender, EventArgs e)
        {
            this.OpenScreen("flow");
        }

        private void OnOperClick(object sender, EventArgs e)
        {
            this.OpenScreen("oper");
        }

        private void OnPfoClick(object sender, EventArgs e)
        {
            this.OpenScreen("pfo");
        }

        private void OpenScreen(string screen)
        {
            Form existing;

            if (this.openScreens.TryGetValue(screen, out existing) && !existing.IsDisposed)
            {
                if (existing.WindowState == FormWindowState.Minimized)
                {
                    existing.WindowState = FormWindowState.Normal;
                }

                existing.Activate();
                return;
            }

            Form created = Create(screen);

            if (created == null)
            {
                return;
            }

            this.openScreens[screen] = created;
            created.FormClosed += (s, e) => this.openScreens.Remove(screen);
            created.Show(this);
        }
    }
}
