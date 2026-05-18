using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Colorlog.ViewModels;

internal static class HistoryUiBrushes
{
    public static Brush SwatchForPersonal(string name)
    {
        if (name.Contains("웜", StringComparison.Ordinal))
        {
            var b = new SolidColorBrush(Color.FromArgb(0xFF, 0xF4, 0xC9, 0xA8));
            b.Freeze();
            return b;
        }

        if (name.Contains("쿨", StringComparison.Ordinal))
        {
            var b = new SolidColorBrush(Color.FromArgb(0xFF, 0xC7, 0xD2, 0xFE));
            b.Freeze();
            return b;
        }

        var d = new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0xE7, 0xF3));
        d.Freeze();
        return d;
    }
}

public partial class HistoryViewModel : ObservableObject
{
    private const int WeeklyPointCount = 7;

    private readonly Dictionary<DateTime, HistoryDayRecord> _sampleRecordsByDate = new();

    public ObservableCollection<DailyTrendVm> WeeklyTrend { get; } = new();

    public ObservableCollection<CalendarCellVm> CalendarCells { get; } = new();

    [ObservableProperty]
    private bool _showBrightness = true;

    [ObservableProperty]
    private Geometry? _weeklyLineGeometry;

    [ObservableProperty]
    private string _weeklyCaption = string.Empty;

    [ObservableProperty]
    private string _weeklyYMaxLabel = "100";

    [ObservableProperty]
    private string _weeklyYMinLabel = "0";

    [ObservableProperty]
    private string _weeklyInsight = string.Empty;

    [ObservableProperty]
    private DateTime _displayMonth;

    [ObservableProperty]
    private string _displayMonthLabel = string.Empty;

    [ObservableProperty]
    private CalendarCellVm? _selectedCell;

    [ObservableProperty]
    private string _selectedDetailDateText = string.Empty;

    [ObservableProperty]
    private string _selectedDetailPersonalColor = string.Empty;

    [ObservableProperty]
    private string _selectedDetailMetricsLine = string.Empty;

    [ObservableProperty]
    private string _selectedDetailNote = string.Empty;

    [ObservableProperty]
    private Brush? _selectedDetailSwatchBrush;

    [ObservableProperty]
    private bool _selectedDetailHasRecord;

    public HistoryViewModel()
    {
        SeedSampleRecords();
        BuildWeeklyTrend();
        DisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        RebuildWeeklyGeometry();
        RebuildWeeklyInsight();
        RebuildCalendarGrid();
        UpdateDisplayMonthLabel();
    }

    partial void OnShowBrightnessChanged(bool value)
    {
        RebuildWeeklyGeometry();
        RebuildWeeklyInsight();
    }

    partial void OnDisplayMonthChanged(DateTime value)
    {
        UpdateDisplayMonthLabel();
        RebuildCalendarGrid();
    }

    partial void OnSelectedCellChanged(CalendarCellVm? value)
    {
        UpdateSelectedDetail(value);
    }

    [RelayCommand]
    private void SelectBrightness() => ShowBrightness = true;

    [RelayCommand]
    private void SelectRedness() => ShowBrightness = false;

    [RelayCommand]
    private void PreviousMonth()
    {
        DisplayMonth = DisplayMonth.AddMonths(-1);
    }

    [RelayCommand]
    private void NextMonth()
    {
        DisplayMonth = DisplayMonth.AddMonths(1);
    }

    [RelayCommand]
    private void SelectCalendarDay(CalendarCellVm? cell)
    {
        if (cell is null || cell.IsPlaceholder || cell.CellDate is null)
        {
            return;
        }

        foreach (var c in CalendarCells)
        {
            c.IsSelected = ReferenceEquals(c, cell);
        }

        SelectedCell = cell;
    }

    [RelayCommand]
    private void ClearCalendarSelection()
    {
        foreach (var c in CalendarCells)
        {
            c.IsSelected = false;
        }

        SelectedCell = null;
    }

    private void SeedSampleRecords()
    {
        var today = DateTime.Today;
        _sampleRecordsByDate[today.AddDays(-1)] = new HistoryDayRecord("봄 웜 라이트", 72, 38, "전일 대비 톤 안정적이에요.");
        _sampleRecordsByDate[today.AddDays(-2)] = new HistoryDayRecord("봄 웜 라이트", 68, 44, "붉은기가 소폭 있었어요. 진정 케어 추천.");
        _sampleRecordsByDate[today.AddDays(-3)] = new HistoryDayRecord("봄 웜 라이트", 70, 41, string.Empty);
        _sampleRecordsByDate[today.AddDays(-5)] = new HistoryDayRecord("봄 웜 라이트", 65, 48, "수면 부족일 때 측정.");
        _sampleRecordsByDate[today.AddDays(-8)] = new HistoryDayRecord("봄 웜 라이트", 63, 52, string.Empty);
        _sampleRecordsByDate[new DateTime(today.Year, today.Month, 3)] = new HistoryDayRecord("봄 웜 라이트", 69, 40, "월초 베이스라인.");
        _sampleRecordsByDate[new DateTime(today.Year, today.Month, 7)] = new HistoryDayRecord("봄 웜 라이트", 74, 36, "컨디션 좋은 날.");
        _sampleRecordsByDate[new DateTime(today.Year, today.Month, 11)] = new HistoryDayRecord("봄 웜 라이트", 71, 43, string.Empty);
        _sampleRecordsByDate[new DateTime(today.Year, today.Month, 14)] = new HistoryDayRecord("봄 웜 라이트", 73, 39, "오늘 촬영 기준.");
    }

    private void BuildWeeklyTrend()
    {
        WeeklyTrend.Clear();
        var today = DateTime.Today;
        for (var i = WeeklyPointCount - 1; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            _sampleRecordsByDate.TryGetValue(d.Date, out var rec);
            var brightness = rec?.Brightness ?? InterpolateDemoBrightness(d);
            var redness = rec?.Redness ?? InterpolateDemoRedness(d);
            WeeklyTrend.Add(new DailyTrendVm(d, brightness, redness));
        }

        var first = WeeklyTrend[0].Date;
        var last = WeeklyTrend[^1].Date;
        WeeklyCaption = string.Create(CultureInfo.CurrentCulture, $"{first:yyyy.M.d} – {last:yyyy.M.d}");
    }

    private static int InterpolateDemoBrightness(DateTime d)
    {
        var seed = (int)(d.Ticks % 997);
        return 64 + seed % 12;
    }

    private static int InterpolateDemoRedness(DateTime d)
    {
        var seed = (int)(d.Ticks % 991);
        return 36 + seed % 14;
    }

    private void RebuildWeeklyGeometry()
    {
        var values = WeeklyTrend.Select(p => ShowBrightness ? p.Brightness : p.Redness).ToArray();
        if (values.Length == 0)
        {
            WeeklyLineGeometry = null;
            return;
        }

        var min = values.Min();
        var max = values.Max();
        if (max - min < 4)
        {
            min = Math.Max(0, min - 2);
            max = Math.Min(100, max + 2);
        }

        WeeklyYMinLabel = min.ToString(CultureInfo.CurrentCulture);
        WeeklyYMaxLabel = max.ToString(CultureInfo.CurrentCulture);

        const double w = 100d;
        const double h = 100d;
        const double padX = 2d;
        var usableW = w - padX * 2;

        var sg = new StreamGeometry();
        using (var ctx = sg.Open())
        {
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];
                var t = values.Length == 1 ? 0.5 : (double)i / (values.Length - 1);
                var x = padX + t * usableW;
                var norm = (v - min) / (max - min);
                var y = h - norm * h;
                var pt = new System.Windows.Point(x, y);
                if (i == 0)
                {
                    ctx.BeginFigure(pt, isFilled: false, isClosed: false);
                }
                else
                {
                    ctx.LineTo(pt, isStroked: true, isSmoothJoin: true);
                }
            }
        }

        sg.Freeze();
        WeeklyLineGeometry = sg;
    }

    private void RebuildWeeklyInsight()
    {
        var values = WeeklyTrend.Select(p => ShowBrightness ? p.Brightness : p.Redness).ToArray();
        if (values.Length < 2)
        {
            WeeklyInsight = string.Empty;
            return;
        }

        var first = values[0];
        var last = values[^1];
        var delta = last - first;
        if (Math.Abs(delta) < 2)
        {
            WeeklyInsight = ShowBrightness
                ? "밝기 지수는 최근 일주일 동안 비슷한 수준을 유지했어요."
                : "붉은기 지수는 최근 일주일 동안 비슷한 수준을 유지했어요.";
        }
        else if (delta > 0)
        {
            WeeklyInsight = ShowBrightness
                ? "밝기 지수가 주 초반보다 다소 올랐어요. 톤업·광채 케어 효과를 의심해볼 수 있어요."
                : "붉은기 지수가 소폭 상승했어요. 자극 요인·수면을 함께 살보면 좋아요.";
        }
        else
        {
            WeeklyInsight = ShowBrightness
                ? "밝기 지수가 약간 내려갔어요. 촬영 조명·각도 차이도 반영될 수 있어요."
                : "붉은기가 완화된 흐름이에요. 진정 루틴이 잘 맞았을 수 있어요.";
        }
    }

    private void UpdateDisplayMonthLabel()
    {
        DisplayMonthLabel = DisplayMonth.ToString("yyyy년 M월", CultureInfo.CurrentCulture);
    }

    private void RebuildCalendarGrid()
    {
        CalendarCells.Clear();
        var monthStart = DisplayMonth;
        var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
        var leading = (int)monthStart.DayOfWeek;
        const int totalCells = 42;
        var prevMonth = monthStart.AddDays(-1);
        var daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        var selectedDate = SelectedCell?.CellDate?.Date;

        for (var i = 0; i < totalCells; i++)
        {
            if (i < leading)
            {
                var dayNum = daysInPrevMonth - leading + i + 1;
                CalendarCells.Add(CalendarCellVm.PlaceholderMuted(dayNum));
                continue;
            }

            if (i < leading + daysInMonth)
            {
                var day = i - leading + 1;
                var cellDate = new DateTime(monthStart.Year, monthStart.Month, day);
                _sampleRecordsByDate.TryGetValue(cellDate, out var rec);
                var isSel = selectedDate == cellDate.Date;
                CalendarCells.Add(CalendarCellVm.ForDay(cellDate, rec, isSel));
                continue;
            }

            var nextDay = i - (leading + daysInMonth) + 1;
            CalendarCells.Add(CalendarCellVm.PlaceholderMuted(nextDay));
        }

        if (selectedDate is { } sd && sd.Year == monthStart.Year && sd.Month == monthStart.Month)
        {
            var match = CalendarCells.FirstOrDefault(c => c.CellDate?.Date == sd);
            if (match is not null)
            {
                foreach (var c in CalendarCells)
                {
                    c.IsSelected = ReferenceEquals(c, match);
                }

                if (!ReferenceEquals(SelectedCell, match))
                {
                    SelectedCell = match;
                }

                return;
            }
        }

        foreach (var c in CalendarCells)
        {
            c.IsSelected = false;
        }

        SelectedCell = null;
    }

    private void UpdateSelectedDetail(CalendarCellVm? cell)
    {
        if (cell?.CellDate is null)
        {
            SelectedDetailDateText = string.Empty;
            SelectedDetailPersonalColor = string.Empty;
            SelectedDetailMetricsLine = string.Empty;
            SelectedDetailNote = string.Empty;
            SelectedDetailSwatchBrush = null;
            SelectedDetailHasRecord = false;
            return;
        }

        SelectedDetailDateText = cell.CellDate.Value.ToString("yyyy년 M월 d일 (ddd)", CultureInfo.CurrentCulture);
        if (!cell.HasRecord || cell.Record is null)
        {
            SelectedDetailPersonalColor = "기록 없음";
            SelectedDetailMetricsLine = "이 날 저장된 분석 데이터가 없어요.";
            SelectedDetailNote = "측정이 있었던 날만 달력에 색이 표시됩니다.";
            SelectedDetailSwatchBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xE5, 0xE7, 0xEB));
            SelectedDetailHasRecord = false;
            return;
        }

        var r = cell.Record;
        SelectedDetailPersonalColor = r.PersonalColorName;
        SelectedDetailMetricsLine = $"밝기 {r.Brightness} · 붉은기 {r.Redness}";
        SelectedDetailNote = string.IsNullOrWhiteSpace(r.Note) ? "추가 메모가 없습니다." : r.Note;
        SelectedDetailSwatchBrush = HistoryUiBrushes.SwatchForPersonal(r.PersonalColorName);
        SelectedDetailHasRecord = true;
    }
}

public sealed class HistoryDayRecord
{
    public string PersonalColorName { get; }
    public int Brightness { get; }
    public int Redness { get; }
    public string Note { get; }

    public HistoryDayRecord(string personalColorName, int brightness, int redness, string note)
    {
        PersonalColorName = personalColorName;
        Brightness = brightness;
        Redness = redness;
        Note = note;
    }
}

public sealed class DailyTrendVm
{
    public DateTime Date { get; }
    public int Brightness { get; }
    public int Redness { get; }
    public string DayLabel { get; }

    public DailyTrendVm(DateTime date, int brightness, int redness)
    {
        Date = date;
        Brightness = brightness;
        Redness = redness;
        DayLabel = date.ToString("M/d (ddd)", CultureInfo.CurrentCulture);
    }
}

public partial class CalendarCellVm : ObservableObject
{
    public bool IsPlaceholder { get; }
    public int DisplayDay { get; }
    public DateTime? CellDate { get; }
    public bool HasRecord { get; }
    public HistoryDayRecord? Record { get; }
    public string PersonalShort { get; }
    public Brush IndicatorBrush { get; }
    public bool IsMuted { get; }
    public bool IsToday { get; }

    [ObservableProperty]
    private bool _isSelected;

    private CalendarCellVm(bool isPlaceholder, int displayDay, DateTime? cellDate, bool hasRecord, HistoryDayRecord? record, string personalShort, Brush indicatorBrush, bool isMuted, bool isToday)
    {
        IsPlaceholder = isPlaceholder;
        DisplayDay = displayDay;
        CellDate = cellDate;
        HasRecord = hasRecord;
        Record = record;
        PersonalShort = personalShort;
        IndicatorBrush = indicatorBrush;
        IsMuted = isMuted;
        IsToday = isToday;
    }

    public static CalendarCellVm PlaceholderMuted(int displayDay)
    {
        var brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xF3, 0xF4, 0xF6));
        brush.Freeze();
        return new CalendarCellVm(true, displayDay, null, false, null, string.Empty, brush, true, false);
    }

    public static CalendarCellVm ForDay(DateTime cellDate, HistoryDayRecord? record, bool isSelected)
    {
        var has = record is not null;
        var shortLabel = has ? ShortenPersonal(record!.PersonalColorName) : string.Empty;
        Brush brush = has ? HistoryUiBrushes.SwatchForPersonal(record!.PersonalColorName) : MutedBarBrush();
        var today = DateTime.Today;
        var vm = new CalendarCellVm(false, cellDate.Day, cellDate, has, record, shortLabel, brush, false, cellDate.Date == today.Date);
        vm.IsSelected = isSelected;
        return vm;
    }

    private static Brush MutedBarBrush()
    {
        var brush = new SolidColorBrush(Color.FromArgb(0xFF, 0xE5, 0xE7, 0xEB));
        brush.Freeze();
        return brush;
    }

    private static string ShortenPersonal(string name)
    {
        if (name.Length <= 5)
        {
            return name;
        }

        return string.Create(CultureInfo.InvariantCulture, $"{name.AsSpan(0, 4)}…");
    }
}
