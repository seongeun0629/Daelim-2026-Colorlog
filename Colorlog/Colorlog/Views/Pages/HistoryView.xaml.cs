using System.Windows;
using System.Windows.Controls;

namespace Colorlog.Views.Pages;

public partial class HistoryView : UserControl
{
    private const double CompactLayoutBreakpointWidth = 920;

    public HistoryView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyCardLayout(ActualWidth);
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyCardLayout(e.NewSize.Width);
    }

    private void ApplyCardLayout(double width)
    {
        if (HistoryCardsLayoutGrid is null
            || WeekHistoryCard is null
            || MonthHistoryCard is null
            || HistoryHintCard is null)
        {
            return;
        }

        if (width <= 0)
        {
            return;
        }

        var compact = width < CompactLayoutBreakpointWidth;
        var grid = HistoryCardsLayoutGrid;
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();

        var cardMarginNormal = new Thickness(10);
        var cardMarginStacked = new Thickness(10, 10, 10, 16);
        var cardMarginLast = new Thickness(10);

        if (compact)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (var i = 0; i < 3; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            Grid.SetColumn(WeekHistoryCard, 0);
            Grid.SetRow(WeekHistoryCard, 0);
            Grid.SetColumnSpan(WeekHistoryCard, 1);
            Grid.SetRowSpan(WeekHistoryCard, 1);

            Grid.SetColumn(HistoryHintCard, 0);
            Grid.SetRow(HistoryHintCard, 1);
            Grid.SetColumnSpan(HistoryHintCard, 1);

            Grid.SetColumn(MonthHistoryCard, 0);
            Grid.SetRow(MonthHistoryCard, 2);
            Grid.SetColumnSpan(MonthHistoryCard, 1);
            Grid.SetRowSpan(MonthHistoryCard, 1);

            WeekHistoryCard.Margin = cardMarginStacked;
            HistoryHintCard.Margin = cardMarginStacked;
            MonthHistoryCard.Margin = cardMarginLast;
        }
        else
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(16) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(WeekHistoryCard, 0);
            Grid.SetRow(WeekHistoryCard, 0);
            Grid.SetColumnSpan(WeekHistoryCard, 1);
            Grid.SetRowSpan(WeekHistoryCard, 1);

            Grid.SetColumn(HistoryHintCard, 0);
            Grid.SetRow(HistoryHintCard, 2);
            Grid.SetColumnSpan(HistoryHintCard, 1);

            Grid.SetColumn(MonthHistoryCard, 2);
            Grid.SetRow(MonthHistoryCard, 0);
            Grid.SetColumnSpan(MonthHistoryCard, 1);
            Grid.SetRowSpan(MonthHistoryCard, 3);

            WeekHistoryCard.Margin = cardMarginNormal;
            HistoryHintCard.Margin = cardMarginNormal;
            MonthHistoryCard.Margin = cardMarginNormal;
        }
    }
}
