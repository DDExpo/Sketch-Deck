using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

using DynamicData;

using sketchDeck.CustomAxaml;
using sketchDeck.Models;
using sketchDeck.ViewModels;
using Splat;

namespace sketchDeck.Views;

public partial class RightPanel : UserControl
{
    private readonly Dictionary<string, float> _scrollOffsets = [];
    private DispatcherTimer? _dragTimer;
    private bool _deleteBtncliked = false;
    private CancellationTokenSource? _tapCts;
    private int? _lastClickedIndex = null;
    public RightPanel()
    {
        InitializeComponent();
        ContentHost.PointerPressed += (_, e) =>
        {
            if (ContentHost.GetVisualDescendants().OfType<DataGrid>().Any()) return;
            DoDrag(e, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
        };
        ContentHost.PointerReleased += (_, _) => { _dragTimer?.Stop(); };
        ContentHost.PointerExited += (_, _) => { _dragTimer?.Stop(); };
    }
    private void DoDrag(PointerEventArgs e, DragDropEffects effects)
    {
        _dragTimer?.Stop();

        _dragTimer = new DispatcherTimer{ Interval = TimeSpan.FromSeconds(0.5)};
        _dragTimer.Tick += (_, _) =>
        {
            _dragTimer?.Stop();
            if (DataContext is not RightPanelViewModel vm) return;

            var selected = vm.Parent.Images?.Where(i => i.IsSelected).ToList();
            if (selected == null || selected.Count == 0) return;

            var dragData = new DataObject();
            dragData.Set("ImageItems", selected);
            _ = Dispatcher.UIThread.InvokeAsync(async () => { _ = await DragDrop.DoDragDrop(e, dragData, effects); });
        };
        _dragTimer.Start();
    }
    private void ScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer sv && DataContext is RightPanelViewModel vm)
        {
            var viewName = vm.Parent.SelectedView;
            _scrollOffsets[viewName] = (float)sv.Offset.Y;
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
    private async void OnBorderTapped(object sender, TappedEventArgs args)
    {
        if (_deleteBtncliked) { _deleteBtncliked = false; return; }
        _tapCts?.Cancel();
        _tapCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(100, _tapCts.Token);
            if (sender is Border b && b.DataContext is ImageItem item)
            {
                var images = (DataContext as RightPanelViewModel)!.Parent.Images;
                int clickedIndex = images!.IndexOf(item);

                var modifiers = args.KeyModifiers;
                if (modifiers.HasFlag(KeyModifiers.Shift) && _lastClickedIndex is int lastIndex)
                {
                    int start = Math.Min(lastIndex, clickedIndex);
                    int end   = Math.Max(lastIndex, clickedIndex);

                    for (int i = start; i <= end; i++)
                        images[i].IsSelected = !item.IsSelected;
                }
                else
                {
                    item.IsSelected = !item.IsSelected;
                    _lastClickedIndex = clickedIndex;
                }
            }
        }
        catch (TaskCanceledException) {}
    }
    private async void OnImageDoubleTapped(object sender, TappedEventArgs args)
    {
        _tapCts?.Cancel();
        if (sender is not Control { DataContext: ImageItem item }) return;

        if (!File.Exists(item.PathImage))
        {
            var dialog = new FileMissingDialog { FilePath = item.PathImage };
            var result = await dialog.ShowDialog<FileMissingResult>((Window)this.GetVisualRoot()!);

            if (result == FileMissingResult.SetNewPath && dialog.NewPath is not null)
            {
                if (item.PathImage == dialog.NewPath) { item.PathImage = dialog.NewPath; }
                else
                {
                    ThumbnailRefs.ReleaseReference(item.ThumbnailPath);
                    var newItem = await ImageItem.FromPathAsync(dialog.NewPath, null, null, null);
                    item.PathImage = dialog.NewPath;
                    item.Type = newItem.Type;
                    item.Size = newItem.Size;
                    item.DateModified = newItem.DateModified;
                    item.ThumbnailPath = newItem.ThumbnailPath;
                }
            }
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
        _deleteBtncliked = true;
        var btn = (Button)sender!;
        var item = (ImageItem)btn.DataContext!;


        if (DataContext is RightPanelViewModel vm)
        {
            ThumbnailRefs.ReleaseReference(item.ThumbnailPath);
            vm.Parent.Collections[vm.Parent.SelectedCollection!.Value].CollectionImages.Remove(item);
        }
    }
}