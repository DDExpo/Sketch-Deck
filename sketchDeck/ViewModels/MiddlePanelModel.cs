using System;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;

namespace sketchDeck.MiddlePanel;

public class ViewToTemplateConverter : IValueConverter
{
    public IDataTemplate? Details { get; set; }
    public IDataTemplate? Small { get; set; }
    public IDataTemplate? Medium { get; set; }
    public IDataTemplate? Big { get; set; }
    public IDataTemplate? Gigantic { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value switch
        {
            "Details" => Details,
            "Small" => Small,
            "Medium" => Medium,
            "Big" => Big,
            "Gigantic" => Gigantic,
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => throw new NotSupportedException();
}

// public class SizeComparer : DataGridComparerSortDescription
// {
//     public int Compare(object x, object y)
//     {
//         if (x is not ImageItem a || y is not ImageItem b)
//             return 0;

//         long sizeA = ParseSize(a.Size);
//         long sizeB = ParseSize(b.Size);

//         return sizeA.CompareTo(sizeB);
//     }

//     private static long ParseSize(string size)
//     {
//         if (string.IsNullOrWhiteSpace(size)) return 0;
//         size = size.Trim().ToUpper();
//         double number = 0;
//         if (size.EndsWith("KB"))
//             number = double.Parse(size.Replace("KB", "").Trim()) * 1024;
//         else if (size.EndsWith("MB"))
//             number = double.Parse(size.Replace("MB", "").Trim()) * 1024 * 1024;
//         else if (size.EndsWith("GB"))
//             number = double.Parse(size.Replace("GB", "").Trim()) * 1024 * 1024 * 1024;
//         else
//             number = double.Parse(size);

//         return (long)number;
//     }
// }
