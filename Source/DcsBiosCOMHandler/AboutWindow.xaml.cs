using System;
using System.Windows;
using System.Windows.Documents;
using System.Diagnostics;
using System.Windows.Navigation;

namespace DcsBiosCOMHandler
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            var bold = new Bold();
            var runDcsBios = new Run("DCS-BIOS");
            var hyperLinkDcsBios = new Hyperlink(runDcsBios) { NavigateUri = new Uri("https://github.com/dcs-bios") };
            hyperLinkDcsBios.TextDecorations = null;
            hyperLinkDcsBios.RequestNavigate += HyperlinkRequestNavigate;
            hyperLinkDcsBios.FontSize = 16;
            hyperLinkDcsBios.FontWeight = FontWeights.Bold;

            var runDcsBiosSupportThread = new Run("Support thread");
            var hyperLinkDcsBiosSupportThread = new Hyperlink(runDcsBiosSupportThread) { NavigateUri = new Uri("http://forums.eagle.ru/showthread.php?t=136570") };
            hyperLinkDcsBiosSupportThread.TextDecorations = null;
            hyperLinkDcsBiosSupportThread.RequestNavigate += HyperlinkRequestNavigate;
            hyperLinkDcsBiosSupportThread.FontSize = 16;
            hyperLinkDcsBiosSupportThread.FontWeight = FontWeights.Bold;

            var runFSFIan = new Run("[FSF]Ian");
            var hyperLinkFSFIan = new Hyperlink(runFSFIan) { NavigateUri = new Uri("http://forums.eagle.ru/member.php?u=95995") };
            hyperLinkFSFIan.TextDecorations = null;
            hyperLinkFSFIan.RequestNavigate += HyperlinkRequestNavigate;
            hyperLinkFSFIan.FontSize = 16;
            hyperLinkFSFIan.FontWeight = FontWeights.Bold;

            var runArturDCS = new Run("ArturDCS");
            var hyperLinkArturDCS = new Hyperlink(runArturDCS) { NavigateUri = new Uri("http://forums.eagle.ru/member.php?u=104153") };
            hyperLinkArturDCS.TextDecorations = null;
            hyperLinkArturDCS.RequestNavigate += HyperlinkRequestNavigate;
            hyperLinkArturDCS.FontSize = 12;
            hyperLinkArturDCS.FontWeight = FontWeights.Bold;
            
            TextBlockInformation.Inlines.Add(hyperLinkDcsBios);
            var run = new Run(", bringing easy communication to and from Digital Combat Simulator.");
            run.FontSize = 16;
            run.FontWeight = FontWeights.Bold;
            TextBlockInformation.Inlines.Add(run);
            TextBlockInformation.Inlines.Add(new LineBreak());
            TextBlockInformation.Inlines.Add(new LineBreak());
            run = new Run("Created by ");
            run.FontSize = 16;
            run.FontWeight = FontWeights.Bold;
            TextBlockInformation.Inlines.Add(run);
            TextBlockInformation.Inlines.Add(hyperLinkFSFIan);
            run = new Run(".");
            run.FontSize = 16;
            run.FontWeight = FontWeights.Bold;
            TextBlockInformation.Inlines.Add(run);
            TextBlockInformation.Inlines.Add(new LineBreak());
            TextBlockInformation.Inlines.Add(new LineBreak());
            TextBlockInformation.Inlines.Add(hyperLinkDcsBiosSupportThread);
            //--------------------------------------
            TextBlockInformation.Inlines.Add(new LineBreak());
            TextBlockInformation.Inlines.Add(new LineBreak());
            TextBlockInformation.Inlines.Add(new LineBreak());
            TextBlockInformation.Inlines.Add(new LineBreak());
            run = new Run("DCS-BIOS COM Handler programmed by ");
            run.FontWeight = FontWeights.Normal;
            TextBlockInformation.Inlines.Add(run);
            TextBlockInformation.Inlines.Add(hyperLinkArturDCS);
        }

        private void HyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void ButtonLogo_OnClick(object sender, RoutedEventArgs e)
        {
            DcsBiosLogo.IsEnabled = !DcsBiosLogo.IsEnabled;
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
