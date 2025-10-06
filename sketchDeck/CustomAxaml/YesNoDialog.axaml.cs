using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

using sketchDeck.ViewModels;

namespace sketchDeck.CustomAxaml;


public partial class YesNoCancelDialog : Window
{
    public bool ShowNoButton { get; set; }
    public new string? Name { get; set; }
    public YesNoCancelDialog()
    {
        InitializeComponent();
        this.Icon = new WindowIcon(AppResources.AppIconPath);
        DataContext = this;
    }
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (ShowNoButton)
        {
            Prefix.Text = "Do you wanna delete collection: ";
            ItemName.Text = Name;
            Suffix.Text = " and all images?";
            NoButton.IsVisible = true;
        }
        else
        {
            Prefix.Text = "Do you wanna delete collection: ";
            ItemName.Text = Name + "?";
            Suffix.Text = "";
            NoButton.IsVisible = false;
        }
    }
    private void Yes_Click(object? sender, RoutedEventArgs e) => Close(DialogResult.Yes);
    private void No_Click(object? sender, RoutedEventArgs e) => Close(DialogResult.No);
    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(DialogResult.Cancel);
}