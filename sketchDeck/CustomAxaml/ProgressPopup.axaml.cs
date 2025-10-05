using System;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace sketchDeck.CustomAxaml
{
    public partial class ProgressPopup : Window
    {
        private readonly TextBlock? _textBlock;
        private int _dotCount = 0;
        private readonly DispatcherTimer _timer;

        public ProgressPopup()
        {
            AvaloniaXamlLoader.Load(this);

            this.Icon = new WindowIcon(AppResources.AppIconPath);

            _textBlock = this.FindControl<TextBlock>("ProcessingText");

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            this.Closing += (s, e) =>
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            };
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_textBlock is null) return;
            _dotCount = (_dotCount + 1) % 4;
            _textBlock.Text = "Processing files" + new string('.', _dotCount);
        }
    }
}