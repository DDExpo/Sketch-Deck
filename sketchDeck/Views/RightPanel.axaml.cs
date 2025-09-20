using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

using DynamicData;

using sketchDeck.CustomAxaml;
using sketchDeck.Models;
using sketchDeck.ViewModels;

namespace sketchDeck.Views;

public partial class RightPanel : UserControl
{
    private readonly Dictionary<string, double> _scrollOffsets = [];
    public RightPanel()
    {
        InitializeComponent();
    }
    private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer sv && DataContext is RightPanelViewModel vm)
        {
            var viewName = vm.Parent.SelectedView;
            _scrollOffsets[viewName] = sv.Offset.Y;
        }
    }
    private void OnScrollViewerAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is ScrollViewer sv && DataContext is RightPanelViewModel vm && _scrollOffsets.TryGetValue(vm.Parent.SelectedView, out var offset))
        {
            sv.Offset = new Vector(sv.Offset.X, offset);
        }
    }
    private void OnSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (DataContext is not RightPanelViewModel vm) return;
        if (vm.Parent.SelectedCollection is null) return;

        var collection = vm.Parent.Collections[vm.Parent.SelectedCollection.Value];
        var sortBy = e.Column.SortMemberPath;
        if (string.IsNullOrEmpty(sortBy) && e.Column is DataGridBoundColumn bound) sortBy = (bound.Binding as Binding)?.Path;

        if (string.IsNullOrEmpty(sortBy)) return;

        var newDirection =
            collection.SortBy == sortBy && collection.SortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

        collection.SortBy = sortBy;
        collection.SortDirection = newDirection;

        e.Handled = true;
    }
    private async void OnImageDoubleTapped(object sender, TappedEventArgs args)
    {
        if (sender is not Control { DataContext: ImageItem item })  return;

        if (!File.Exists(item.PathImage))
        {
            var dialog = new FileMissingDialog(item.PathImage);
            var result = await dialog.ShowDialog<FileMissingResult>((Window)this.GetVisualRoot()!);

            if (result == FileMissingResult.SetNewPath && dialog.NewPath is not null) { item.PathImage = dialog.NewPath; }
            else if (result == FileMissingResult.None) { return; }
        }

        new PreviewWindow(item).Show();
    }
    private void OnTextBlockDoubleTapped(object sender, TappedEventArgs args)
    {
        if (sender is TextBlock tb && tb.DataContext is ImageItem item) {item.IsEditing = true;};
    }
    private void EditTextBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ImageItem item) { item.IsEditing = false; }
    }
    private void EditTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is ImageItem item)
        {
            if (e.Key == Key.Enter) { item.IsEditing = false; }
            else if (e.Key == Key.Escape) { item.IsEditing = false; }
        }
    }
    private void DeleteItem_Click(object? sender, RoutedEventArgs e)
    {
        var btn = (Button)sender!;
        var item = (ImageItem)btn.DataContext!;

        ThumbnailRefs.ReleaseReference(item.Thumbnail);

        if (DataContext is RightPanelViewModel vm)
        {
            vm.Parent.Collections[vm.Parent.SelectedCollection!.Value].CollectionImages.Remove(item);
        }
    }
}