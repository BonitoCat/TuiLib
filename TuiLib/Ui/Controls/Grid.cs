using System;
using System.Collections.Generic;
using System.Linq;

namespace TuiLib.Ui.Controls;

public class Grid : Panel
{
    public static readonly TuiProperty<int> RowProperty = TuiProperty.RegisterAttached<Grid, int>("Row", 0);
    public static readonly TuiProperty<int> ColumnProperty = TuiProperty.RegisterAttached<Grid, int>("Column", 0);
    public static readonly TuiProperty<int> RowSpanProperty = TuiProperty.RegisterAttached<Grid, int>("RowSpan", 1);
    public static readonly TuiProperty<int> ColumnSpanProperty = TuiProperty.RegisterAttached<Grid, int>("ColumnSpan", 1);
    public static readonly TuiProperty<int> RowSpacingProperty = TuiProperty.Register<Grid, int>(nameof(RowSpacing), 0, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<int> ColumnSpacingProperty = TuiProperty.Register<Grid, int>(nameof(ColumnSpacing), 0, PropertyFlags.AffectsMeasure);

    private RowDefinitions _rowDefinitions = new();
    private ColumnDefinitions _columnDefinitions = new();

    public RowDefinitions RowDefinitions
    {
        get => _rowDefinitions;
        set
        {
            _rowDefinitions = value;
            InvalidateMeasure();
        }
    }

    public ColumnDefinitions ColumnDefinitions
    {
        get => _columnDefinitions;
        set
        {
            _columnDefinitions = value;
            InvalidateMeasure();
        }
    }

    public int RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    public int ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public static int GetRow(Element element) => element.GetValue(RowProperty);

    public static void SetRow(Element element, int value) => element.SetValue(RowProperty, value);

    public static int GetColumn(Element element) => element.GetValue(ColumnProperty);

    public static void SetColumn(Element element, int value) => element.SetValue(ColumnProperty, value);

    public static int GetRowSpan(Element element) => element.GetValue(RowSpanProperty);

    public static void SetRowSpan(Element element, int value) => element.SetValue(RowSpanProperty, value);

    public static int GetColumnSpan(Element element) => element.GetValue(ColumnSpanProperty);

    public static void SetColumnSpan(Element element, int value) => element.SetValue(ColumnSpanProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        List<DefinitionBase> columns = GetColumns();
        List<DefinitionBase> rows = GetRows();

        foreach (Element child in Children)
        {
            (int start, int span) = GetSpan(child, columns.Count, true);
            int width = AllAbsolute(columns, start, span) ? SumAbsolute(columns, start, span, ColumnSpacing) : availableSize.Width;
            child.Measure(new(width, Layout.Infinite));
        }

        Resolve(columns, availableSize.Width, ColumnSpacing, child => GetSpan(child, columns.Count, true), child => child.DesiredSize.Width);

        foreach (Element child in Children)
        {
            (int columnStart, int columnSpan) = GetSpan(child, columns.Count, true);
            (int rowStart, int rowSpan) = GetSpan(child, rows.Count, false);
            int height = AllAbsolute(rows, rowStart, rowSpan) ? SumAbsolute(rows, rowStart, rowSpan, RowSpacing) : availableSize.Height;
            child.Measure(new(SumActual(columns, columnStart, columnSpan, ColumnSpacing), height));
        }

        Resolve(rows, availableSize.Height, RowSpacing, child => GetSpan(child, rows.Count, false), child => child.DesiredSize.Height);

        foreach (Element child in Children)
        {
            (int columnStart, int columnSpan) = GetSpan(child, columns.Count, true);
            (int rowStart, int rowSpan) = GetSpan(child, rows.Count, false);
            child.Measure(new(SumActual(columns, columnStart, columnSpan, ColumnSpacing), SumActual(rows, rowStart, rowSpan, RowSpacing)));
        }

        return new(DesiredLength(columns, ColumnSpacing, child => GetSpan(child, columns.Count, true), child => child.DesiredSize.Width),
            DesiredLength(rows, RowSpacing, child => GetSpan(child, rows.Count, false), child => child.DesiredSize.Height));
    }

    protected override void ArrangeOverride(Rect bounds)
    {
        List<DefinitionBase> columns = GetColumns();
        List<DefinitionBase> rows = GetRows();
        Resolve(columns, bounds.Width, ColumnSpacing, child => GetSpan(child, columns.Count, true), child => child.DesiredSize.Width);
        Resolve(rows, bounds.Height, RowSpacing, child => GetSpan(child, rows.Count, false), child => child.DesiredSize.Height);
        AssignOffsets(columns, bounds.X, ColumnSpacing);
        AssignOffsets(rows, bounds.Y, RowSpacing);

        foreach (Element child in Children)
        {
            (int columnStart, int columnSpan) = GetSpan(child, columns.Count, true);
            (int rowStart, int rowSpan) = GetSpan(child, rows.Count, false);
            child.Arrange(new(
                columns[columnStart].Offset,
                rows[rowStart].Offset,
                SumActual(columns, columnStart, columnSpan, ColumnSpacing),
                SumActual(rows, rowStart, rowSpan, RowSpacing)));
        }
    }

    private List<DefinitionBase> GetColumns()
    {
        return _columnDefinitions.Count == 0 ? new() { new ColumnDefinition() } : _columnDefinitions.Cast<DefinitionBase>().ToList();
    }

    private List<DefinitionBase> GetRows()
    {
        return _rowDefinitions.Count == 0 ? new() { new RowDefinition() } : _rowDefinitions.Cast<DefinitionBase>().ToList();
    }

    private static (int Start, int Span) GetSpan(Element child, int count, bool isColumn)
    {
        int start = Layout.Clamp(isColumn ? GetColumn(child) : GetRow(child), 0, count - 1);
        int span = Layout.Clamp(isColumn ? GetColumnSpan(child) : GetRowSpan(child), 1, count - start);
        return (start, span);
    }

    private void Resolve(List<DefinitionBase> definitions, int available, int spacing, Func<Element, (int Start, int Span)> getSpan, Func<Element, int> getDesired)
    {
        foreach (DefinitionBase definition in definitions)
        {
            definition.ActualLength = definition.Length.IsAbsolute ? Clamp(definition, (int) definition.Length.Value) : Clamp(definition, 0);
        }

        List<Element> visibleChildren = Children.Where(child => child.IsVisible).ToList();
        foreach (Element child in visibleChildren.OrderBy(child => getSpan(child).Span))
        {
            (int start, int span) = getSpan(child);
            List<DefinitionBase> spanned = definitions.GetRange(start, span);
            if (spanned.Any(definition => definition.Length.IsStar) || !spanned.Any(definition => definition.Length.IsAuto))
            {
                continue;
            }

            int current = SumActual(definitions, start, span, spacing);
            int extra = getDesired(child) - current;
            if (extra <= 0)
            {
                continue;
            }

            DefinitionBase target = spanned.Last(definition => definition.Length.IsAuto);
            target.ActualLength = Clamp(target, target.ActualLength + extra);
        }

        List<DefinitionBase> stars = definitions.Where(definition => definition.Length.IsStar).ToList();
        if (stars.Count == 0)
        {
            return;
        }

        double totalWeight = stars.Sum(definition => Math.Max(0, definition.Length.Value));
        if (Layout.IsInfinite(available) || totalWeight <= 0)
        {
            foreach (Element child in visibleChildren)
            {
                (int start, int span) = getSpan(child);
                if (span != 1 || !definitions[start].Length.IsStar)
                {
                    continue;
                }

                definitions[start].ActualLength = Clamp(definitions[start], Math.Max(definitions[start].ActualLength, getDesired(child)));
            }

            return;
        }

        int fixedLength = definitions.Where(definition => !definition.Length.IsStar).Sum(definition => definition.ActualLength);
        int remaining = Math.Max(0, available - fixedLength - spacing * (definitions.Count - 1));
        int assigned = 0;
        double accumulated = 0;
        foreach (DefinitionBase star in stars)
        {
            accumulated += Math.Max(0, star.Length.Value);
            int end = (int) Math.Round(remaining * accumulated / totalWeight);
            star.ActualLength = Clamp(star, end - assigned);
            assigned = end;
        }
    }

    private int DesiredLength(List<DefinitionBase> definitions, int spacing, Func<Element, (int Start, int Span)> getSpan, Func<Element, int> getDesired)
    {
        int[] lengths = definitions.Select(definition => definition.Length.IsStar ? 0 : definition.ActualLength).ToArray();
        foreach (Element child in Children.Where(child => child.IsVisible))
        {
            (int start, int span) = getSpan(child);
            if (span != 1 || !definitions[start].Length.IsStar)
            {
                continue;
            }

            lengths[start] = Math.Max(lengths[start], getDesired(child));
        }

        return Layout.Add(lengths.Sum(), spacing * (definitions.Count - 1));
    }

    private static void AssignOffsets(List<DefinitionBase> definitions, int origin, int spacing)
    {
        int offset = origin;
        foreach (DefinitionBase definition in definitions)
        {
            definition.Offset = offset;
            offset += definition.ActualLength + spacing;
        }
    }

    private static bool AllAbsolute(List<DefinitionBase> definitions, int start, int span)
    {
        return definitions.GetRange(start, span).All(definition => definition.Length.IsAbsolute);
    }

    private static int SumAbsolute(List<DefinitionBase> definitions, int start, int span, int spacing)
    {
        return definitions.GetRange(start, span).Sum(definition => Clamp(definition, (int) definition.Length.Value)) + spacing * (span - 1);
    }

    private static int SumActual(List<DefinitionBase> definitions, int start, int span, int spacing)
    {
        int sum = 0;
        for (int i = start; i < start + span; i++)
        {
            sum = Layout.Add(sum, definitions[i].ActualLength);
        }

        return Layout.Add(sum, spacing * (span - 1));
    }

    private static int Clamp(DefinitionBase definition, int value)
    {
        return Layout.Clamp(value, definition.MinLength, definition.MaxLength);
    }
}
