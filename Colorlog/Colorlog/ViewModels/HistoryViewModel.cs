using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Colorlog.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;

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

    private readonly PythonEngineService? _pythonService;
    private readonly Dictionary<DateTime, HistoryDayRecord> _sampleRecordsByDate = new();

    private double _chartWidth;
    private double _chartHeight = 88;

    public void OnChartSizeChanged(double width, double height)
    {
        if (width <= 0) return;
        _chartWidth = width;
        if (height > 0) _chartHeight = height;
        RebuildWeeklyGeometry();
    }

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

    public HistoryViewModel() : this(null) { }

    public HistoryViewModel(PythonEngineService? pythonService)
    {
        _pythonService = pythonService;
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
        _ = LoadMonthDataAsync(DisplayMonth);
    }

    [RelayCommand]
    private void NextMonth()
    {
        DisplayMonth = DisplayMonth.AddMonths(1);
        _ = LoadMonthDataAsync(DisplayMonth);
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

    /// <summary>DB에서 현재 월 + 최근 7일 데이터를 불러와 캘린더·트렌드를 다시 그립니다.</summary>
    public async Task LoadHistoryAsync()
    {
        if (_pythonService == null) return;

        var today = DateTime.Today;
        await LoadFromQueryAsync(today.Year, today.Month);
    }

    private async Task LoadMonthDataAsync(DateTime month)
    {
        if (_pythonService == null) return;
        await LoadFromQueryAsync(month.Year, month.Month);
    }

    private async Task LoadFromQueryAsync(int year, int month)
    {
        var result = await _pythonService.QueryHistoryAsync(year, month);

        var monthly = result["monthly"] as JArray ?? new JArray();
        var weekly = result["weekly"] as JArray ?? new JArray();

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _sampleRecordsByDate.Clear();

            foreach (var item in monthly)
                MergeRecord(item, withNote: true);

            foreach (var item in weekly)
                MergeRecord(item, withNote: false);

            BuildWeeklyTrend();
            RebuildWeeklyGeometry();
            RebuildWeeklyInsight();
            RebuildCalendarGrid();
        });
    }

    private void MergeRecord(JToken item, bool withNote)
    {
        var dateStr = item["date"]?.ToString();
        if (!DateTime.TryParse(dateStr, out var date))
            return;

        if (_sampleRecordsByDate.ContainsKey(date.Date))
            return;

        var rec = new HistoryDayRecord(
            item["personal_color"]?.ToString() ?? "",
            item["brightness"]?.Value<int>() ?? 0,
            item["redness"]?.Value<int>() ?? 0,
            withNote ? (item["note"]?.ToString() ?? "") : ""
        );
        _sampleRecordsByDate[date.Date] = rec;
    }

    private void BuildWeeklyTrend()
    {
        WeeklyTrend.Clear();
        var today = DateTime.Today;
        for (var i = WeeklyPointCount - 1; i >= 0; i--)
        {
            var d = today.AddDays(-i);
            _sampleRecordsByDate.TryGetValue(d.Date, out var rec);
            int? brightness = rec != null ? rec.Brightness : null;
            int? redness   = rec != null ? rec.Redness   : null;
            WeeklyTrend.Add(new DailyTrendVm(d, brightness, redness));
        }

        var first = WeeklyTrend[0].Date;
        var last = WeeklyTrend[^1].Date;
        WeeklyCaption = string.Create(CultureInfo.CurrentCulture, $"{first:yyyy.M.d} – {last:yyyy.M.d}");
    }

    private void RebuildWeeklyGeometry()
    {
        if (_chartWidth <= 0)
        {
            WeeklyLineGeometry = null;
            return;
        }

        var indexed = WeeklyTrend
            .Select((p, i) => (i, val: ShowBrightness ? p.Brightness : p.Redness))
            .Where(x => x.val.HasValue)
            .Select(x => (x.i, val: x.val!.Value))
            .ToArray();

        if (indexed.Length == 0)
        {
            WeeklyLineGeometry = null;
            WeeklyYMaxLabel = "100";
            WeeklyYMinLabel = "0";
            return;
        }

        var allValues = indexed.Select(x => x.val).ToArray();
        var min = allValues.Min();
        var max = allValues.Max();
        if (max - min < 4)
        {
            min = Math.Max(0, min - 2);
            max = Math.Min(100, max + 2);
        }

        WeeklyYMinLabel = min.ToString(CultureInfo.CurrentCulture);
        WeeklyYMaxLabel = max.ToString(CultureInfo.CurrentCulture);

        var w = _chartWidth;
        var h = _chartHeight;

        var sg = new StreamGeometry();
        using (var ctx = sg.Open())
        {
            var started = false;
            foreach (var (idx, val) in indexed)
            {
                var x = (idx + 0.5) / WeeklyPointCount * w;
                var norm = max == min ? 0.5 : (double)(val - min) / (max - min);
                var y = h - norm * h;
                var pt = new System.Windows.Point(x, y);
                if (!started)
                {
                    ctx.BeginFigure(pt, isFilled: false, isClosed: false);
                    started = true;
                    if (indexed.Length == 1)
                        ctx.LineTo(new System.Windows.Point(x + 0.5, y), isStroked: true, isSmoothJoin: false);
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
        var values = WeeklyTrend
            .Select(p => ShowBrightness ? p.Brightness : p.Redness)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToArray();

        if (values.Length == 0)
        {
            WeeklyInsight = "최근 7일간 진단 기록이 없어요. 분석을 시작하면 여기에 트렌드가 표시됩니다.";
            return;
        }

        if (values.Length == 1)
        {
            WeeklyInsight = "진단 기록이 1건 있어요. 더 많은 기록이 쌓이면 트렌드를 확인할 수 있어요.";
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
    public int? Brightness { get; }
    public int? Redness { get; }
    public string DayLabel { get; }
    public bool HasData { get; }

    public DailyTrendVm(DateTime date, int? brightness, int? redness)
    {
        Date = date;
        Brightness = brightness;
        Redness = redness;
        HasData = brightness.HasValue || redness.HasValue;
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
