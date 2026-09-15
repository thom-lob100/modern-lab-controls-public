using System;
using System.Windows.Forms;

namespace Modern.Lab.MasterData
{
    public static class Program
    {
        private const string ScreenPrefix = "--screen=";

        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Modern.Lab.WinForms.Controls.Hosting.WpfHostOptions.SoftwareRendering = true;

            foreach (string arg in args)
            {
                if (string.Equals(arg, "--dark", StringComparison.OrdinalIgnoreCase))
                {
                    Modern.Lab.Theming.ModernTheme.Mode = Modern.Lab.Theming.ModernTheme.ThemeMode.Dark;
                }
                else if (arg.StartsWith("--theme=", StringComparison.OrdinalIgnoreCase))
                {
                    Modern.Lab.Theming.ModernTheme.ThemeMode mode;

                    if (Enum.TryParse(arg.Substring("--theme=".Length), true, out mode))
                    {
                        Modern.Lab.Theming.ModernTheme.Mode = mode;
                    }
                }
            }

            Modern.Lab.WinForms.Controls.Hosting.ModernWpfWarmup.Run();

            foreach (string arg in args)
            {
                if (string.Equals(arg, "--uitest-product", StringComparison.OrdinalIgnoreCase))
                {
                    Modern.Lab.MasterData.Diagnostics.ProductUiSelfTest.Run();
                    return;
                }

                if (string.Equals(arg, "--uitest-areabay", StringComparison.OrdinalIgnoreCase))
                {
                    Modern.Lab.MasterData.Diagnostics.AreaBayUiSelfTest.Run();
                    return;
                }

                if (string.Equals(arg, "--uitest-flowoper", StringComparison.OrdinalIgnoreCase))
                {
                    Modern.Lab.MasterData.Diagnostics.FlowOperUiSelfTest.Run();
                    return;
                }

                if (string.Equals(arg, "--uitest-pfo", StringComparison.OrdinalIgnoreCase))
                {
                    Modern.Lab.MasterData.Diagnostics.PfoNodeUiSelfTest.Run();
                    return;
                }
            }

            Application.Run(StartForm(args));
        }

        private static Form StartForm(string[] args)
        {
            foreach (string arg in args)
            {
                if (!arg.StartsWith(ScreenPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Form screen = MasterDataMenuForm.Create(arg.Substring(ScreenPrefix.Length));

                if (screen != null)
                {
                    return screen;
                }
            }

            return new MasterDataMenuForm();
        }
    }
}
