using System.Windows;
using System.Windows.Controls;

namespace Colorlog.Views.Pages;

public partial class DashboardView : UserControl
{
    private const double CompactLayoutBreakpointWidth = 920;

    public DashboardView()
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
        if (DashboardTopCardsGrid is null
            || DashboardColorSummaryCard is null
            || DashboardRecommendCard is null
            || DashboardSkinMetricsCard is null)
        {
            return;
        }

        if (width <= 0)
        {
            return;
        }

        var compact = width < CompactLayoutBreakpointWidth;
        var grid = DashboardTopCardsGrid;
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();

        if (compact)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(DashboardColorSummaryCard, 0);
            Grid.SetRow(DashboardColorSummaryCard, 0);
            Grid.SetColumnSpan(DashboardColorSummaryCard, 1);

            Grid.SetColumn(DashboardRecommendCard, 0);
            Grid.SetRow(DashboardRecommendCard, 1);
            Grid.SetColumnSpan(DashboardRecommendCard, 1);

            Grid.SetColumn(DashboardSkinMetricsCard, 0);
            Grid.SetRow(DashboardSkinMetricsCard, 2);
            Grid.SetColumnSpan(DashboardSkinMetricsCard, 1);

            DashboardColorSummaryCard.Margin = new Thickness(10, 10, 10, 8);
            DashboardRecommendCard.Margin = new Thickness(10, 8, 10, 8);
            DashboardSkinMetricsCard.Margin = new Thickness(10, 8, 10, 10);
        }
        else
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetColumn(DashboardColorSummaryCard, 0);
            Grid.SetRow(DashboardColorSummaryCard, 0);
            Grid.SetColumnSpan(DashboardColorSummaryCard, 1);

            Grid.SetColumn(DashboardRecommendCard, 1);
            Grid.SetRow(DashboardRecommendCard, 0);
            Grid.SetColumnSpan(DashboardRecommendCard, 1);

            Grid.SetColumn(DashboardSkinMetricsCard, 0);
            Grid.SetRow(DashboardSkinMetricsCard, 1);
            Grid.SetColumnSpan(DashboardSkinMetricsCard, 2);

            var m = new Thickness(10);
            DashboardColorSummaryCard.Margin = m;
            DashboardRecommendCard.Margin = m;
            DashboardSkinMetricsCard.Margin = m;
        }
    }
}
