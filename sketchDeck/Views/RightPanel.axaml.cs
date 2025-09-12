using System;
using System.ComponentModel;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;

using sketchDeck.Models;
using sketchDeck.ViewModels;

namespace sketchDeck.Views;

public partial class RightPanel : UserControl
{
    // private Point _startPoint;
    // private bool _isDragging;
    private ItemsRepeater? _currentRepeater;
    public RightPanel()
    {
        InitializeComponent();

        ContentHost.PropertyChanged += (_, e) =>
        {
            if (e.Property == ContentProperty)
            {
                _currentRepeater = ContentHost.FindControl<ItemsRepeater>("Repeater");
            }
        };
    }
    private void OnSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (e.Column is not DataGridBoundColumn column) return;
        if (DataContext is not RightPanelViewModel vm) return;

        var sortBy = column.SortMemberPath ?? (column.Binding as Binding)?.Path;
        if (string.IsNullOrEmpty(sortBy)) return;

        var newDirecation = vm.Parent.SortDirection == ListSortDirection.Ascending && vm.Parent.SortBy == sortBy
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        vm.Parent.UpdateSort(sortBy, newDirecation);

        e.Handled = true;
    }
    private void OnImageDoubleTapped(object sender, TappedEventArgs args)
    {
        if (sender is Control control && control.DataContext is ImageItem item)
        {
            new PreviewWindow(item).Show();
        }
    }
    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ImageItem item)
        {
            if (DataContext is not RightPanelViewModel vm) return;

            bool ctrl = (e.KeyModifiers & KeyModifiers.Control) != 0;
            bool shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;

            if (!ctrl && !shift)
            {
                foreach (var img in vm.Parent.Images)
                    img.IsSelected = false;

                item.IsSelected = true;
            }
            else if (ctrl)
            {
                item.IsSelected = !item.IsSelected;
            }
            else if (shift)
            {
                var list = vm.Parent.Images.ToList();
                int lastIndex = list.FindIndex(x => x.IsSelected);
                int thisIndex = list.IndexOf(item);

                if (lastIndex >= 0)
                {
                    int start = Math.Min(lastIndex, thisIndex);
                    int end = Math.Max(lastIndex, thisIndex);
                    for (int i = start; i <= end; i++)
                        list[i].IsSelected = true;
                }
                else
                {
                    item.IsSelected = true;
                }
            }
        }
    }
    // private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    // {
    //     if (e.GetCurrentPoint(OverlayCanvas).Properties.IsLeftButtonPressed)
    //     {
    //         _isDragging = true;
    //         _startPoint = e.GetPosition(OverlayCanvas);

    //         Canvas.SetLeft(SelectionRect, _startPoint.X);
    //         Canvas.SetTop(SelectionRect, _startPoint.Y);
    //         SelectionRect.Width = 0;
    //         SelectionRect.Height = 0;
    //         SelectionRect.IsVisible = true;
    //     }
    // }
    // private void OnOverlayPointerMoved(object? sender, PointerEventArgs e)
    // {
    //     if (_isDragging)
    //     {
    //         var pos = e.GetPosition(OverlayCanvas);

    //         double x = Math.Min(pos.X, _startPoint.X);
    //         double y = Math.Min(pos.Y, _startPoint.Y);
    //         double w = Math.Abs(pos.X - _startPoint.X);
    //         double h = Math.Abs(pos.Y - _startPoint.Y);

    //         Canvas.SetLeft(SelectionRect, x);
    //         Canvas.SetTop(SelectionRect, y);
    //         SelectionRect.Width = w;
    //         SelectionRect.Height = h;

    //         UpdateSelection(new Rect(x, y, w, h));
    //     }
    // }
    // private void OnOverlayPointerReleased(object? sender, PointerReleasedEventArgs e)
    // {
    //     _isDragging = false;
    //     SelectionRect.IsVisible = false;
    // }
    // private void UpdateSelection(Rect selectionRect)
    // {
    //     if (_currentRepeater is null) return;

    //     foreach (var child in _currentRepeater.GetVisualChildren().OfType<Control>().ToList())
    //     {
    //         if (child is Border border)
    //         {
    //             var bounds = border.Bounds;
    //             var transform = border.TransformToVisual(OverlayCanvas);
    //             if (transform != null)
    //             {
    //                 var itemBounds = bounds.TransformToAABB(transform.Value);
    //                 bool isSelected = selectionRect.Intersects(itemBounds);

    //                 if (border.DataContext is ImageItem vm)
    //                 {
    //                     vm.IsSelected = isSelected;
    //                 }
    //             }
    //         }
    //     }
    // }
}