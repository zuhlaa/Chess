using System;
using System.Windows.Controls;

namespace ChessUI
{
    public partial class ModeMenu : UserControl
    {
        public event Action<bool> ModeSelected; // bool = isAiMode

        public ModeMenu()
        {
            InitializeComponent();
        }

        private void TwoPlayers_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ModeSelected?.Invoke(false);
        }

        private void VsComputer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ModeSelected?.Invoke(true);
        }
    }
}
